using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.Common.Integrations;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed partial class GlpiAdapter(
    HttpClient httpClient,
    GlpiCredentialsResolver credentialsResolver,
    GlpiSessionManager sessionManager,
    GlpiUserNameResolver userNameResolver,
    ILogger<GlpiAdapter> logger) : IGlpiAdapter
{
    private const int PageSize = 50;
    private const int MaxTickets = 500;
    private static readonly int[] OpenTicketStatuses = [1, 2, 3, 4, 10];
    private static readonly int[] ClosedTicketStatuses = [5, 6];
    private static readonly TimeSpan CategoryCacheDuration = TimeSpan.FromMinutes(5);

    private IReadOnlyList<GlpiEntity>? _cachedEntities;
    private DateTimeOffset _entitiesCachedAt = DateTimeOffset.MinValue;
    private readonly Dictionary<int, (IReadOnlyList<GlpiItilCategory> Categories, DateTimeOffset CachedAt)> _categoriesByEntity = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<GlpiTicketResult> CreateTicketAsync(
        string title,
        string description,
        string priority,
        int entityId,
        int categoryId,
        string requesterEmail,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            throw new GlpiIntegrationException("GLPI não configurado. Informe glpi.base_url no portal admin.");
        }

        if (entityId <= 0)
        {
            throw new ArgumentException("Entidade inválida.");
        }

        if (categoryId <= 0)
        {
            throw new ArgumentException("Categoria inválida.");
        }

        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (requesterId is null)
        {
            throw new GlpiRequesterNotFoundException(requesterEmail);
        }

        var priorityLevel = MapFormPriority(priority);
        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(credentials.BaseUrl, "Ticket"));
            ApplySessionHeaders(request, credentials, sessionToken);
            request.Content = JsonContent.Create(new
            {
                input = new Dictionary<string, object?>
                {
                    ["name"] = title,
                    ["content"] = description,
                    ["priority"] = priorityLevel,
                    ["urgency"] = priorityLevel,
                    ["type"] = 2,
                    ["entities_id"] = entityId,
                    ["itilcategories_id"] = categoryId,
                    ["_users_id_requester"] = requesterId,
                },
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                return await CreateTicketAsync(title, description, priority, entityId, categoryId, requesterEmail, cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "GLPI ticket creation failed for {Email}: {Status} {Body}",
                    requesterEmail,
                    (int)response.StatusCode,
                    body);
                throw new GlpiIntegrationException("O GLPI rejeitou a criação do chamado. Verifique categoria e permissões.");
            }

            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
            var ticketId = ReadElement(payload?.GetValueOrDefault("id")).Trim('"');
            if (string.IsNullOrWhiteSpace(ticketId))
            {
                throw new GlpiIntegrationException("O GLPI não retornou o identificador do chamado.");
            }

            return new GlpiTicketResult
            {
                TicketId = ticketId,
                Status = "New",
                Url = BuildTicketUrl(credentials, ticketId),
            };
        }
        catch (GlpiRequesterNotFoundException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (GlpiIntegrationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to create GLPI ticket for {Email}", requesterEmail);
            throw new GlpiIntegrationException("Falha ao comunicar com o GLPI.", exception);
        }
    }

    public async Task<IReadOnlyList<GlpiEntity>> GetEntitiesAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedEntities is not null &&
            DateTimeOffset.UtcNow - _entitiesCachedAt < CategoryCacheDuration)
        {
            return _cachedEntities;
        }

        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return [];
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        var rawEntities = await LoadEntitiesFromApiAsync(credentials, sessionToken, cancellationToken);

        _cachedEntities = HelpDeskGlpiEntityTreeBuilder.Build(rawEntities);
        _entitiesCachedAt = DateTimeOffset.UtcNow;
        return _cachedEntities;
    }

    public async Task<IReadOnlyList<GlpiItilCategory>> GetAllItilCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return [];
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        await sessionManager.EnsureActiveEntityAsync(httpClient, credentials, sessionToken, 1, cancellationToken);
        var rawCategories = await LoadItilCategoriesFromApiAsync(credentials, sessionToken, cancellationToken);
        return HelpDeskItilCategoryTreeBuilder.Build(rawCategories, dedupeByLabel: false);
    }

    public async Task<IReadOnlyList<GlpiItilCategory>> GetItilCategoriesAsync(
        int entityId,
        CancellationToken cancellationToken = default)
    {
        if (entityId <= 0)
        {
            throw new ArgumentException("Entidade inválida.");
        }

        if (_categoriesByEntity.TryGetValue(entityId, out var cached) &&
            DateTimeOffset.UtcNow - cached.CachedAt < CategoryCacheDuration)
        {
            return cached.Categories;
        }

        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return [];
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        await sessionManager.EnsureActiveEntityAsync(httpClient, credentials, sessionToken, 1, cancellationToken);
        var rawCategories = await LoadItilCategoriesFromApiAsync(credentials, sessionToken, cancellationToken);
        var filtered = rawCategories
            .Where(c => c.EntityId == entityId)
            .ToList();

        var built = HelpDeskItilCategoryTreeBuilder.Build(filtered, dedupeByLabel: false);
        _categoriesByEntity[entityId] = (built, DateTimeOffset.UtcNow);
        return built;
    }

    private async Task<List<GlpiEntity>> LoadEntitiesFromApiAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        const int pageSize = 500;
        var results = new List<GlpiEntity>();
        var start = 0;

        while (true)
        {
            var url = $"{BuildUrl(credentials.BaseUrl, "Entity")}?range={start}-{start + pageSize - 1}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("GLPI Entity list failed: {Status} {Body}", (int)response.StatusCode, body);
                break;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                break;
            }

            var pageCount = 0;
            foreach (var item in root.EnumerateArray())
            {
                pageCount++;
                var entity = MapEntityItem(item);
                if (entity is not null)
                {
                    results.Add(entity);
                }
            }

            if (pageCount < pageSize)
            {
                break;
            }

            start += pageSize;
        }

        return results;
    }

    private static GlpiEntity? MapEntityItem(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var idElement) ||
            !idElement.TryGetInt32(out var id) ||
            id <= 0)
        {
            return null;
        }

        var name = ReadJsonString(item, "name");
        var fullName = ReadJsonString(item, "completename");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = name;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        int? parentId = null;
        if (item.TryGetProperty("entities_id", out var parentElement) &&
            parentElement.TryGetInt32(out var parentRaw))
        {
            parentId = HelpDeskGlpiEntityTreeBuilder.NormalizeParentId(parentRaw);
        }

        return new GlpiEntity
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? fullName : name,
            FullName = fullName,
            ParentId = parentId,
        };
    }

    private async Task<List<GlpiItilCategory>> LoadItilCategoriesFromApiAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        const int pageSize = 500;
        var results = new List<GlpiItilCategory>();
        var start = 0;

        while (true)
        {
            var url = $"{BuildUrl(credentials.BaseUrl, "ITILCategory")}?range={start}-{start + pageSize - 1}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("GLPI ITILCategory list failed: {Status} {Body}", (int)response.StatusCode, body);
                break;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                break;
            }

            var pageCount = 0;
            foreach (var item in root.EnumerateArray())
            {
                pageCount++;
                var category = MapItilCategoryItem(item);
                if (category is not null)
                {
                    results.Add(category);
                }
            }

            if (pageCount < pageSize)
            {
                break;
            }

            start += pageSize;
        }

        return results;
    }

    private static GlpiItilCategory? MapItilCategoryItem(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var idElement) ||
            !idElement.TryGetInt32(out var id) ||
            id <= 0)
        {
            return null;
        }

        var name = ReadJsonString(item, "name");
        var fullName = ReadJsonString(item, "completename");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = name;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        int? parentId = null;
        if (item.TryGetProperty("itilcategories_id", out var parentElement) &&
            parentElement.TryGetInt32(out var parentRaw))
        {
            parentId = HelpDeskItilCategoryTreeBuilder.NormalizeParentId(parentRaw);
        }

        var entityId = 0;
        if (item.TryGetProperty("entities_id", out var entityElement) &&
            entityElement.TryGetInt32(out var entityRaw))
        {
            entityId = entityRaw;
        }

        return new GlpiItilCategory
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? fullName : name,
            FullName = fullName,
            ParentId = parentId,
            EntityId = entityId,
        };
    }

    private static string ReadJsonString(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty,
        };
    }

    public async Task<GlpiTicketResult> GetTicketStatusAsync(
        string ticketId,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(credentials.BaseUrl, $"Ticket/{ticketId}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        var status = ReadElement(payload?.GetValueOrDefault("status")).Trim('"');

        return new GlpiTicketResult
        {
            TicketId = ticketId,
            Status = status,
            Url = BuildTicketUrl(credentials, ticketId),
        };
    }

    public async Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsByRequesterAsync(
        string requesterEmail,
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return [];
        }

        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (requesterId is null)
        {
            logger.LogWarning("GLPI user not found for email {Email}", requesterEmail);
            return [];
        }

        return await SearchTicketsAsync(credentials, scope, requesterId, includeRequester: false, cancellationToken);
    }

    public Task<IReadOnlyList<GlpiTicketSummary>> SearchAllTicketsAsync(
        GlpiTicketScope scope,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return Task.FromResult<IReadOnlyList<GlpiTicketSummary>>([]);
        }

        return SearchTicketsAsync(credentials, scope, requesterId: null, includeRequester: true, cancellationToken);
    }

    public async Task<GlpiOpenQueueCounts> CountOpenQueueAsync(
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            logger.LogWarning("GLPI BaseUrl is not configured.");
            return new GlpiOpenQueueCounts();
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        // Pendentes = Novo (1) + Pendente (4) + Aprovação (10)
        // Em atendimento = Atribuído (2) + Planejado (3)
        var pending = await CountTicketsByStatusesAsync(
            credentials,
            sessionToken,
            [1, 4, 10],
            cancellationToken);
        var inProgress = await CountTicketsByStatusesAsync(
            credentials,
            sessionToken,
            [2, 3],
            cancellationToken);

        return new GlpiOpenQueueCounts
        {
            Pending = pending,
            InProgress = inProgress,
        };
    }

    public async Task<GlpiTicketDetail?> GetTicketDetailAsync(
        string ticketId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (!skipOwnershipCheck)
        {
            var ownsTicket = await VerifyTicketOwnershipAsync(credentials, ticketId, requesterId, cancellationToken);
            if (!ownsTicket)
            {
                logger.LogWarning("User {Email} attempted to access GLPI ticket {TicketId}", requesterEmail, ticketId);
                return null;
            }
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(credentials.BaseUrl, $"Ticket/{ticketId}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        var status = GlpiStatusMapper.NormalizeStatusCode(
            ReadElement(payload.GetValueOrDefault("status")).Trim('"'));
        var priority = ReadElement(payload.GetValueOrDefault("priority")).Trim('"');
        var createdAt = ParseGlpiDate(ReadElement(payload.GetValueOrDefault("date")).Trim('"'));
        var updatedAt = ParseGlpiDate(ReadElement(payload.GetValueOrDefault("date_mod")).Trim('"'));
        var solvedAt = ParseGlpiDateNullable(ReadElement(payload.GetValueOrDefault("solvedate")).Trim('"'));

        var summary = new GlpiTicketSummary
        {
            TicketId = ticketId,
            Subject = ReadElement(payload.GetValueOrDefault("name")).Trim('"'),
            Status = status,
            StatusLabel = GlpiStatusMapper.StatusLabel(status),
            PriorityLabel = GlpiStatusMapper.PriorityLabel(priority),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Url = BuildTicketUrl(credentials, ticketId),
        };

        var requesterTask = LoadTicketRequesterAsync(credentials, sessionToken, ticketId, cancellationToken);
        var assigneeTask = LoadTicketAssigneeAsync(credentials, sessionToken, ticketId, cancellationToken);
        var followupsTask = LoadTicketFollowupsAsync(credentials, sessionToken, ticketId, cancellationToken);
        var solutionTask = LoadTicketSolutionAsync(credentials, sessionToken, ticketId, solvedAt, cancellationToken);
        var attachmentsTask = LoadTicketAttachmentsAsync(credentials, sessionToken, ticketId, cancellationToken);

        await Task.WhenAll(requesterTask, assigneeTask, followupsTask, solutionTask, attachmentsTask);

        summary.RequesterLabel = await requesterTask;

        return new GlpiTicketDetail
        {
            Summary = summary,
            Description = ReadElement(payload.GetValueOrDefault("content")).Trim('"'),
            Assignee = await assigneeTask,
            Solution = await solutionTask,
            Followups = await followupsTask,
            Attachments = await attachmentsTask,
        };
    }

    public async Task<GlpiTicketAttachmentContent?> GetTicketAttachmentAsync(
        string ticketId,
        string documentId,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(documentId))
        {
            return null;
        }

        var credentials = credentialsResolver.Resolve();
        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (!skipOwnershipCheck)
        {
            var ownsTicket = await VerifyTicketOwnershipAsync(credentials, ticketId, requesterId, cancellationToken);
            if (!ownsTicket)
            {
                logger.LogWarning(
                    "User {Email} attempted to download GLPI document {DocumentId} on ticket {TicketId}",
                    requesterEmail,
                    documentId,
                    ticketId);
                return null;
            }
        }

        var attachments = await LoadTicketAttachmentsAsync(
            credentials,
            await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken),
            ticketId,
            cancellationToken);

        var meta = attachments.FirstOrDefault(item =>
            item.DocumentId.Equals(documentId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (meta is null)
        {
            return null;
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUrl(credentials.BaseUrl, $"Document/{meta.DocumentId}?alt=media"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "GLPI document download failed for {DocumentId}: {Status}",
                meta.DocumentId,
                (int)response.StatusCode);
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType
            ?? meta.ContentType
            ?? "application/octet-stream";

        return new GlpiTicketAttachmentContent
        {
            FileName = meta.FileName,
            ContentType = contentType,
            Content = bytes,
        };
    }

    public async Task UploadTicketDocumentAsync(
        string ticketId,
        string fileName,
        string contentType,
        Stream content,
        string requesterEmail,
        bool skipOwnershipCheck = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Ticket e nome do arquivo são obrigatórios.");
        }

        var credentials = credentialsResolver.Resolve();
        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken);
        if (!skipOwnershipCheck)
        {
            var ownsTicket = await VerifyTicketOwnershipAsync(credentials, ticketId, requesterId, cancellationToken);
            if (!ownsTicket)
            {
                throw new UnauthorizedAccessException("Você não tem permissão para anexar arquivos neste chamado.");
            }
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        var restBase = BuildLegacyRestBaseUrl(credentials.BaseUrl);
        var safeName = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "anexo.bin";
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var documentId = await UploadDocumentBinaryAsync(
            credentials,
            sessionToken,
            restBase,
            safeName,
            contentType,
            buffer,
            cancellationToken);

        using var linkRequest = new HttpRequestMessage(HttpMethod.Post, BuildUrl(credentials.BaseUrl, "Document_Item"));
        ApplySessionHeaders(linkRequest, credentials, sessionToken);
        linkRequest.Content = JsonContent.Create(new
        {
            input = new
            {
                items_id = ticketId,
                itemtype = "Ticket",
                documents_id = documentId,
            },
        });

        using var linkResponse = await httpClient.SendAsync(linkRequest, cancellationToken);
        if (!linkResponse.IsSuccessStatusCode)
        {
            var body = await linkResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "GLPI Document_Item link failed for ticket {TicketId} document {DocumentId}: {Status} {Body}",
                ticketId,
                documentId,
                (int)linkResponse.StatusCode,
                body);
            throw new GlpiIntegrationException("Anexo enviado, mas não foi possível vincular ao chamado no GLPI.");
        }
    }

    private async Task<string> UploadDocumentBinaryAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string restBase,
        string safeName,
        string contentType,
        MemoryStream buffer,
        CancellationToken cancellationToken,
        bool retried = false)
    {
        var manifest = JsonSerializer.Serialize(new
        {
            input = new
            {
                name = safeName,
                _filename = new[] { safeName },
            },
        });

        buffer.Position = 0;
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(manifest, Encoding.UTF8, "application/json"), "uploadManifest");
        var streamContent = new StreamContent(buffer);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(streamContent, "filename[0]", safeName);

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"{restBase}/Document");
        ApplySessionHeaders(uploadRequest, credentials, sessionToken);
        uploadRequest.Content = form;

        using var uploadResponse = await httpClient.SendAsync(uploadRequest, cancellationToken);
        if (uploadResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized && !retried)
        {
            await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
            var renewed = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
            return await UploadDocumentBinaryAsync(
                credentials,
                renewed,
                restBase,
                safeName,
                contentType,
                buffer,
                cancellationToken,
                retried: true);
        }

        if (!uploadResponse.IsSuccessStatusCode)
        {
            var body = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "GLPI Document upload failed: {Status} {Body}",
                (int)uploadResponse.StatusCode,
                body);
            throw new GlpiIntegrationException("Não foi possível enviar o anexo ao GLPI.");
        }

        var payload = await uploadResponse.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(
            JsonOptions,
            cancellationToken);
        var documentId = ReadElement(payload?.GetValueOrDefault("id")).Trim('"');
        if (string.IsNullOrWhiteSpace(documentId) &&
            payload is not null &&
            payload.TryGetValue("0", out var nested) &&
            nested.ValueKind == JsonValueKind.Object &&
            nested.TryGetProperty("id", out var nestedId))
        {
            documentId = nestedId.ToString().Trim('"');
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new GlpiIntegrationException("O GLPI não retornou o identificador do documento anexado.");
        }

        return documentId;
    }

    private static string BuildLegacyRestBaseUrl(string apiBaseUrl)
    {
        var trimmed = apiBaseUrl.TrimEnd('/');
        const string v1Suffix = "/api.php/v1";
        if (trimmed.EndsWith(v1Suffix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[..^v1Suffix.Length] + "/apirest.php";
        }

        if (trimmed.Contains("/apirest.php", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var uri = new Uri(trimmed);
        return $"{uri.Scheme}://{uri.Authority}/apirest.php";
    }

    private async Task<string?> LoadTicketRequesterAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string ticketId,
        CancellationToken cancellationToken)
    {
        try
        {
            var labels = new List<string>();
            var userActors = await GetTicketSubItemsAsync(
                credentials,
                sessionToken,
                ticketId,
                "Ticket_User",
                cancellationToken);

            foreach (var item in userActors)
            {
                if (!IsRequesterActor(item))
                {
                    continue;
                }

                var userId = ReadJsonInt(item, "users_id");
                if (userId is null or <= 0)
                {
                    continue;
                }

                var name = await userNameResolver.ResolveDisplayNameAsync(
                    httpClient,
                    credentials,
                    sessionToken,
                    userId.Value.ToString(),
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    labels.Add(name);
                }
            }

            if (labels.Count == 0)
            {
                return null;
            }

            return string.Join(", ", labels.Distinct(StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load GLPI requester for ticket {TicketId}", ticketId);
            return null;
        }
    }

    private async Task<string?> LoadTicketAssigneeAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string ticketId,
        CancellationToken cancellationToken)
    {
        try
        {
            var labels = new List<string>();

            var userActors = await GetTicketSubItemsAsync(
                credentials,
                sessionToken,
                ticketId,
                "Ticket_User",
                cancellationToken);
            foreach (var item in userActors)
            {
                if (!IsAssignActor(item))
                {
                    continue;
                }

                var userId = ReadJsonInt(item, "users_id");
                if (userId is null or <= 0)
                {
                    continue;
                }

                var name = await userNameResolver.ResolveDisplayNameAsync(
                    httpClient,
                    credentials,
                    sessionToken,
                    userId.Value.ToString(),
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    labels.Add(name);
                }
            }

            var groupActors = await GetTicketSubItemsAsync(
                credentials,
                sessionToken,
                ticketId,
                "Ticket_Group",
                cancellationToken);
            foreach (var item in groupActors)
            {
                if (!IsAssignActor(item))
                {
                    continue;
                }

                var groupId = ReadJsonInt(item, "groups_id");
                if (groupId is null or <= 0)
                {
                    continue;
                }

                var groupName = await ResolveGroupNameAsync(
                    credentials,
                    sessionToken,
                    groupId.Value.ToString(),
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    labels.Add(groupName);
                }
            }

            if (labels.Count == 0)
            {
                return null;
            }

            return string.Join(", ", labels.Distinct(StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load GLPI assignees for ticket {TicketId}", ticketId);
            return null;
        }
    }

    private async Task<IReadOnlyList<GlpiTicketFollowup>> LoadTicketFollowupsAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string ticketId,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await GetTicketSubItemsAsync(
                credentials,
                sessionToken,
                ticketId,
                "ITILFollowup",
                cancellationToken);
            var results = new List<GlpiTicketFollowup>();

            foreach (var item in items)
            {
                if (IsPrivateItem(item))
                {
                    continue;
                }

                var content = ReadJsonString(item, "content");
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var createdRaw = FirstNonEmpty(
                    ReadJsonString(item, "date_creation"),
                    ReadJsonString(item, "date"));
                var createdAt = ParseGlpiDate(createdRaw);
                var authorId = ReadJsonInt(item, "users_id");
                string? author = null;
                if (authorId is > 0)
                {
                    author = await userNameResolver.ResolveDisplayNameAsync(
                        httpClient,
                        credentials,
                        sessionToken,
                        authorId.Value.ToString(),
                        cancellationToken);
                }

                results.Add(new GlpiTicketFollowup
                {
                    Kind = "followup",
                    Content = content,
                    CreatedAt = createdAt,
                    Author = author,
                });
            }

            return results
                .OrderBy(f => f.CreatedAt)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load GLPI followups for ticket {TicketId}", ticketId);
            return [];
        }
    }

    private async Task<GlpiTicketSolution?> LoadTicketSolutionAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string ticketId,
        DateTimeOffset? ticketSolvedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await GetTicketSubItemsAsync(
                credentials,
                sessionToken,
                ticketId,
                "ITILSolution",
                cancellationToken);
            if (items.Count == 0)
            {
                return null;
            }

            var candidates = items
                .Select(item =>
                {
                    var content = ReadJsonString(item, "content");
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return null;
                    }

                    var at = ParseGlpiDateNullable(
                        FirstNonEmpty(
                            ReadJsonString(item, "date_creation"),
                            ReadJsonString(item, "date")));
                    var status = ReadJsonInt(item, "status") ?? 0;
                    return new { Item = item, Content = content, At = at, Status = status };
                })
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderByDescending(x => x.Status == 2) // accepted first when present
                .ThenByDescending(x => x.At ?? DateTimeOffset.MinValue)
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            var preferred = candidates[0];
            var authorId = ReadJsonInt(preferred.Item, "users_id");
            string? author = null;
            if (authorId is > 0)
            {
                author = await userNameResolver.ResolveDisplayNameAsync(
                    httpClient,
                    credentials,
                    sessionToken,
                    authorId.Value.ToString(),
                    cancellationToken);
            }

            return new GlpiTicketSolution
            {
                Content = preferred.Content,
                ResolvedAt = preferred.At ?? ticketSolvedAt,
                Author = author,
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load GLPI solution for ticket {TicketId}", ticketId);
            return null;
        }
    }

    private async Task<string?> ResolveGroupNameAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string groupId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(credentials.BaseUrl, $"Group/{groupId}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var name = FirstNonEmpty(
            ReadJsonString(document.RootElement, "completename"),
            ReadJsonString(document.RootElement, "name"));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private async Task<IReadOnlyList<JsonElement>> GetTicketSubItemsAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string ticketId,
        string resource,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUrl(credentials.BaseUrl, $"Ticket/{ticketId}/{resource}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "GLPI Ticket/{TicketId}/{Resource} failed: {Status}",
                ticketId,
                resource,
                (int)response.StatusCode);
            return [];
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        // Clone elements: JsonDocument disposed after return.
        return root.EnumerateArray()
            .Select(item => item.Clone())
            .ToList();
    }

    private static bool IsRequesterActor(JsonElement item)
    {
        var type = ReadJsonInt(item, "type");
        // CommonITILActor::REQUESTER = 1
        return type is 1;
    }

    private static bool IsAssignActor(JsonElement item)
    {
        var type = ReadJsonInt(item, "type");
        // CommonITILActor::ASSIGN = 2
        return type is 2;
    }

    private static bool IsPrivateItem(JsonElement item)
    {
        if (!item.TryGetProperty("is_private", out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => value.TryGetInt32(out var number) && number == 1,
            JsonValueKind.String => value.GetString() is "1" or "true",
            _ => false,
        };
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static DateTimeOffset? ParseGlpiDateNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "null" or "0000-00-00 00:00:00")
        {
            return null;
        }

        return ParseGlpiDate(value);
    }

    private async Task<IReadOnlyList<GlpiTicketAttachment>> LoadTicketAttachmentsAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string ticketId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                BuildUrl(credentials.BaseUrl, $"Ticket/{ticketId}/Document_Item"));
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "GLPI Ticket/{TicketId}/Document_Item failed: {Status}",
                    ticketId,
                    (int)response.StatusCode);
                return [];
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<GlpiTicketAttachment>();
            foreach (var item in root.EnumerateArray())
            {
                var documentsId = ReadJsonInt(item, "documents_id");
                if (documentsId is null or <= 0)
                {
                    continue;
                }

                var meta = await LoadDocumentMetaAsync(
                    credentials,
                    sessionToken,
                    documentsId.Value.ToString(),
                    cancellationToken);
                if (meta is not null)
                {
                    results.Add(meta);
                }
            }

            return results;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to load GLPI attachments for ticket {TicketId}", ticketId);
            return [];
        }
    }

    private async Task<GlpiTicketAttachment?> LoadDocumentMetaAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string documentId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(credentials.BaseUrl, $"Document/{documentId}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        var fileName = ReadElement(payload.GetValueOrDefault("filename")).Trim('"');
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = ReadElement(payload.GetValueOrDefault("name")).Trim('"');
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"documento-{documentId}";
        }

        var mime = ReadElement(payload.GetValueOrDefault("mime")).Trim('"');
        long? sizeBytes = null;
        if (payload.TryGetValue("filesize", out var sizeElement) &&
            long.TryParse(ReadElement(sizeElement).Trim('"'), out var parsedSize))
        {
            sizeBytes = parsedSize;
        }

        return new GlpiTicketAttachment
        {
            DocumentId = documentId,
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(mime) ? null : mime,
            SizeBytes = sizeBytes,
        };
    }

    private static int? ReadJsonInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private async Task<int> CountTicketsByStatusesAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        IReadOnlyList<int> statuses,
        CancellationToken cancellationToken)
    {
        if (statuses.Count == 0)
        {
            return 0;
        }

        var query = new StringBuilder(BuildUrl(credentials.BaseUrl, "search/Ticket"));
        query.Append('?');
        for (var i = 0; i < statuses.Count; i++)
        {
            if (i > 0)
            {
                query.Append($"&criteria[{i}][link]=OR");
            }

            query.Append(
                $"&criteria[{i}][field]={GlpiSearchFields.TicketStatus}" +
                $"&criteria[{i}][searchtype]=equals" +
                $"&criteria[{i}][value]={statuses[i]}");
        }

        query.Append(
            $"&forcedisplay[0]={GlpiSearchFields.TicketId}" +
            "&range=0-0");

        using var request = new HttpRequestMessage(HttpMethod.Get, query.ToString());
        ApplySessionHeaders(request, credentials, sessionToken);
        request.Headers.TryAddWithoutValidation("Range", "0-0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
            var renewed = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
            return await CountTicketsByStatusesAsync(credentials, renewed, statuses, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "GLPI ticket count by status failed ({Statuses}): {Status} {Body}",
                string.Join(',', statuses),
                (int)response.StatusCode,
                body);
            return 0;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        if (root.TryGetProperty("totalcount", out var total) && total.TryGetInt32(out var totalCount))
        {
            return totalCount;
        }

        if (root.TryGetProperty("count", out var count) && count.TryGetInt32(out var pageCount))
        {
            return pageCount;
        }

        return 0;
    }

    private async Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsAsync(
        GlpiRuntimeCredentials credentials,
        GlpiTicketScope scope,
        string? requesterId,
        bool includeRequester,
        CancellationToken cancellationToken)
    {
        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        List<GlpiTicketSummary> results;

        // Fila completa: todos os abertos (sem recorte de data) + fechados para completar a lista.
        // Assim o header (CountOpenQueueAsync) bate com os chips Pendente/Em atendimento da grid.
        if (scope is GlpiTicketScope.All or GlpiTicketScope.Last90Days)
        {
            var open = await SearchTicketsByStatusesAsync(
                credentials,
                sessionToken,
                requesterId,
                includeRequester,
                OpenTicketStatuses,
                maxResults: MaxTickets,
                cancellationToken);
            var remaining = Math.Max(0, MaxTickets - open.Count);
            var closed = remaining > 0
                ? await SearchTicketsByStatusesAsync(
                    credentials,
                    sessionToken,
                    requesterId,
                    includeRequester,
                    ClosedTicketStatuses,
                    maxResults: remaining,
                    cancellationToken)
                : [];
            results = open.Concat(closed).ToList();
        }
        else
        {
            results = (await SearchTicketsByStatusesAsync(
                credentials,
                sessionToken,
                requesterId,
                includeRequester,
                OpenTicketStatuses,
                maxResults: MaxTickets,
                cancellationToken)).ToList();
        }

        if (includeRequester && results.Count > 0)
        {
            await userNameResolver.EnrichRequesterLabelsAsync(
                httpClient,
                credentials,
                sessionToken,
                results,
                cancellationToken);
        }

        if (results.Count > 0)
        {
            await userNameResolver.EnrichAssigneeLabelsAsync(
                httpClient,
                credentials,
                sessionToken,
                results,
                cancellationToken);
        }

        return results
            .OrderByDescending(t => GlpiStatusMapper.IsOpenStatus(t.Status))
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
    }

    private async Task<IReadOnlyList<GlpiTicketSummary>> SearchTicketsByStatusesAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string? requesterId,
        bool includeRequester,
        IReadOnlyList<int> statuses,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (statuses.Count == 0 || maxResults <= 0)
        {
            return [];
        }

        var query = BuildTicketSearchQueryByStatuses(
            credentials.BaseUrl,
            requesterId,
            statuses,
            includeRequester);
        var results = new List<GlpiTicketSummary>();
        var start = 0;
        var allowed = statuses.Select(s => s.ToString()).ToHashSet(StringComparer.Ordinal);

        while (results.Count < maxResults)
        {
            var pageUrl = $"{query}&range={start}-{start + PageSize - 1}";
            using var request = new HttpRequestMessage(HttpMethod.Get, pageUrl);
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                var renewed = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
                return await SearchTicketsByStatusesAsync(
                    credentials,
                    renewed,
                    requesterId,
                    includeRequester,
                    statuses,
                    maxResults,
                    cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("GLPI ticket search by status failed: {Status} {Body}", (int)response.StatusCode, body);
                break;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (!root.TryGetProperty("data", out var dataElement) || dataElement.GetArrayLength() == 0)
            {
                break;
            }

            foreach (var row in dataElement.EnumerateArray())
            {
                var summary = MapSearchRow(row, credentials, includeRequester);
                if (!allowed.Contains(summary.Status))
                {
                    continue;
                }

                results.Add(summary);
                if (results.Count >= maxResults)
                {
                    break;
                }
            }

            var pageCount = root.TryGetProperty("count", out var countElement) ? countElement.GetInt32() : 0;
            if (pageCount < PageSize)
            {
                break;
            }

            start += PageSize;
        }

        return results;
    }

    private async Task<bool> VerifyTicketOwnershipAsync(
        GlpiRuntimeCredentials credentials,
        string ticketId,
        string? requesterId,
        CancellationToken cancellationToken)
    {
        if (requesterId is null)
        {
            return false;
        }

        var query =
            $"{BuildUrl(credentials.BaseUrl, "search/Ticket")}" +
            $"?criteria[0][field]={GlpiSearchFields.TicketId}" +
            $"&criteria[0][searchtype]=equals" +
            $"&criteria[0][value]={Uri.EscapeDataString(ticketId)}" +
            $"&criteria[1][link]=AND" +
            $"&criteria[1][field]={GlpiSearchFields.TicketRequester}" +
            $"&criteria[1][searchtype]=equals" +
            $"&criteria[1][value]={Uri.EscapeDataString(requesterId)}" +
            $"&range=0-0";

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("data", out var data) && data.GetArrayLength() > 0;
    }

    private async Task<string?> ResolveUserIdAsync(
        GlpiRuntimeCredentials credentials,
        string requesterEmail,
        CancellationToken cancellationToken)
    {
        var email = requesterEmail.Trim();
        var emailLower = email.ToLowerInvariant();
        var login = emailLower.Split('@')[0];

        foreach (var candidate in new[] { email, emailLower })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var byUserEmailRecord = await SearchUserIdByEmailRecordAsync(credentials, candidate, cancellationToken);
            if (byUserEmailRecord is not null)
            {
                return byUserEmailRecord;
            }

            var byEmailContains = await SearchUserIdAsync(
                credentials,
                GlpiSearchFields.UserEmail,
                candidate,
                "contains",
                cancellationToken);
            if (byEmailContains is not null)
            {
                return byEmailContains;
            }

            var byEmailEquals = await SearchUserIdAsync(
                credentials,
                GlpiSearchFields.UserEmail,
                candidate,
                "equals",
                cancellationToken);
            if (byEmailEquals is not null)
            {
                return byEmailEquals;
            }
        }

        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        foreach (var candidate in new[] { login, email, emailLower })
        {
            var byLogin = await SearchUserIdAsync(
                credentials,
                GlpiSearchFields.UserLogin,
                candidate,
                "equals",
                cancellationToken);
            if (byLogin is not null)
            {
                return byLogin;
            }
        }

        return null;
    }

    private async Task<string?> SearchUserIdByEmailRecordAsync(
        GlpiRuntimeCredentials credentials,
        string email,
        CancellationToken cancellationToken)
    {
        foreach (var searchType in new[] { "equals", "contains" })
        {
            var query =
                $"{BuildUrl(credentials.BaseUrl, "search/UserEmail")}" +
                $"?criteria[0][field]={GlpiSearchFields.UserEmailRecordEmail}" +
                $"&criteria[0][searchtype]={searchType}" +
                $"&criteria[0][value]={Uri.EscapeDataString(email)}" +
                $"&forcedisplay[0]={GlpiSearchFields.UserEmailRecordUserId}";

            var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, query);
            ApplySessionHeaders(request, credentials, sessionToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
            {
                continue;
            }

            var first = data[0];
            if (first.TryGetProperty(GlpiSearchFields.UserEmailRecordUserId.ToString(), out var idElement))
            {
                return idElement.ToString();
            }
        }

        return null;
    }

    private async Task<string?> SearchUserIdAsync(
        GlpiRuntimeCredentials credentials,
        int field,
        string value,
        string searchType,
        CancellationToken cancellationToken)
    {
        var query =
            $"{BuildUrl(credentials.BaseUrl, "search/User")}" +
            $"?criteria[0][field]={field}" +
            $"&criteria[0][searchtype]={searchType}" +
            $"&criteria[0][value]={Uri.EscapeDataString(value)}" +
            $"&forcedisplay[0]={GlpiSearchFields.UserId}";

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
        {
            return null;
        }

        var first = data[0];
        if (first.TryGetProperty(GlpiSearchFields.UserId.ToString(), out var idElement))
        {
            return idElement.ToString();
        }

        return null;
    }

    private static string BuildTicketSearchQueryByStatuses(
        string baseUrl,
        string? requesterId,
        IReadOnlyList<int> statuses,
        bool includeRequester)
    {
        var query = new StringBuilder($"{BuildUrl(baseUrl, "search/Ticket")}?");
        var hasRequester = !string.IsNullOrWhiteSpace(requesterId);

        if (hasRequester)
        {
            query.Append(
                $"criteria[0][field]={GlpiSearchFields.TicketRequester}" +
                $"&criteria[0][searchtype]=equals" +
                $"&criteria[0][value]={Uri.EscapeDataString(requesterId!)}" +
                "&criteria[1][link]=AND");

            for (var i = 0; i < statuses.Count; i++)
            {
                if (i > 0)
                {
                    query.Append($"&criteria[1][criteria][{i}][link]=OR");
                }

                query.Append(
                    $"&criteria[1][criteria][{i}][field]={GlpiSearchFields.TicketStatus}" +
                    $"&criteria[1][criteria][{i}][searchtype]=equals" +
                    $"&criteria[1][criteria][{i}][value]={statuses[i]}");
            }
        }
        else
        {
            for (var i = 0; i < statuses.Count; i++)
            {
                if (i > 0)
                {
                    query.Append($"&criteria[{i}][link]=OR");
                }

                query.Append(
                    $"&criteria[{i}][field]={GlpiSearchFields.TicketStatus}" +
                    $"&criteria[{i}][searchtype]=equals" +
                    $"&criteria[{i}][value]={statuses[i]}");
            }
        }

        query.Append(
            $"&forcedisplay[0]={GlpiSearchFields.TicketId}" +
            $"&forcedisplay[1]={GlpiSearchFields.TicketTitle}" +
            $"&forcedisplay[2]={GlpiSearchFields.TicketStatus}" +
            $"&forcedisplay[3]={GlpiSearchFields.TicketPriority}" +
            $"&forcedisplay[4]={GlpiSearchFields.TicketDateOpening}" +
            $"&forcedisplay[5]={GlpiSearchFields.TicketTechnician}");

        if (includeRequester)
        {
            query.Append($"&forcedisplay[6]={GlpiSearchFields.TicketRequester}");
        }

        query.Append("&sort=15&order=DESC");
        return query.ToString();
    }

    private static GlpiTicketSummary MapSearchRow(
        JsonElement row,
        GlpiRuntimeCredentials credentials,
        bool includeRequester)
    {
        var ticketId = ReadRowField(row, GlpiSearchFields.TicketId);
        var status = GlpiStatusMapper.NormalizeStatusCode(ReadRowField(row, GlpiSearchFields.TicketStatus));
        var priority = ReadRowField(row, GlpiSearchFields.TicketPriority);
        var createdRaw = ReadRowField(row, GlpiSearchFields.TicketDateOpening);
        var assigneeRaw = ReadRowField(row, GlpiSearchFields.TicketTechnician);

        return new GlpiTicketSummary
        {
            TicketId = ticketId,
            Subject = ReadRowField(row, GlpiSearchFields.TicketTitle),
            Status = status,
            StatusLabel = GlpiStatusMapper.StatusLabel(status),
            PriorityLabel = GlpiStatusMapper.PriorityLabel(priority),
            CreatedAt = ParseGlpiDate(createdRaw),
            Url = BuildTicketUrl(credentials, ticketId),
            RequesterLabel = includeRequester ? ReadRowField(row, GlpiSearchFields.TicketRequester) : null,
            AssigneeLabel = string.IsNullOrWhiteSpace(assigneeRaw) ? null : assigneeRaw.Trim(),
        };
    }

    private static string ReadRowField(JsonElement row, int fieldId)
    {
        if (row.TryGetProperty(fieldId.ToString(), out var value))
        {
            return value.ToString();
        }

        return string.Empty;
    }

    private static void ApplySessionHeaders(
        HttpRequestMessage request,
        GlpiRuntimeCredentials credentials,
        string sessionToken)
    {
        request.Headers.Add("App-Token", credentials.AppToken);
        request.Headers.Add("Session-Token", sessionToken);
    }

    private static string BuildUrl(string baseUrl, string path) =>
        $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

    private static string BuildTicketUrl(GlpiRuntimeCredentials credentials, string ticketId) =>
        $"{credentials.PortalUrl}/front/ticket.form.php?id={ticketId}";

    private static string ReadElement(JsonElement? element) =>
        element?.ToString() ?? string.Empty;

    private static DateTimeOffset ParseGlpiDate(string raw)
    {
        if (DateTimeOffset.TryParse(raw, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private static int MapFormPriority(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        return normalized switch
        {
            "urgente" or "critica" or "crítica" => 5,
            "alta" => 4,
            "media" or "média" => 3,
            "baixa" => 2,
            _ => 3,
        };
    }
}
