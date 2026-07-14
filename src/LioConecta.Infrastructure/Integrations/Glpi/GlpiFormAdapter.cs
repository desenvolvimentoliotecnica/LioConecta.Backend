using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Web;
using LioConecta.Application.Common.Integrations;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations.Models;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Glpi;

public sealed partial class GlpiAdapter
{
    private const string GlpiFormType = "Glpi\\Form\\Form";
    private const string GlpiFormCategoryType = "Glpi\\Form\\Category";
    private const string GlpiFormSectionType = "Glpi\\Form\\Section";
    private const string GlpiFormQuestionType = "Glpi\\Form\\Question";

    public async Task<IReadOnlyList<GlpiFormCategory>> GetFormCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            return [];
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        var rows = await GetItemtypeListAsync(credentials, sessionToken, GlpiFormCategoryType, cancellationToken);
        return rows
            .Select(MapFormCategory)
            .Where(c => c.Id > 0)
            .OrderBy(c => c.CompleteName ?? c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<GlpiFormSummary>> GetFormsAsync(
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            return [];
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        var rows = await GetItemtypeListAsync(credentials, sessionToken, GlpiFormType, cancellationToken);
        return rows
            .Select(MapFormSummary)
            .Where(f => f.Id > 0)
            .Where(f => categoryId is null or <= 0 || f.CategoryId == categoryId)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<GlpiFormSchema?> GetFormSchemaAsync(
        int formId,
        CancellationToken cancellationToken = default)
    {
        if (formId <= 0)
        {
            return null;
        }

        var credentials = credentialsResolver.Resolve();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl))
        {
            return null;
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        var formElement = await GetItemAsync(credentials, sessionToken, GlpiFormType, formId, cancellationToken);
        if (formElement is null)
        {
            return null;
        }

        var form = MapFormSummary(formElement.Value);
        var sections = await GetItemtypeSubListAsync(
            credentials,
            sessionToken,
            GlpiFormType,
            formId,
            GlpiFormSectionType,
            cancellationToken);

        var mappedSections = new List<GlpiFormSection>();
        foreach (var sectionElement in sections.OrderBy(s => ReadJsonInt(s, "rank") ?? 0))
        {
            var sectionId = ReadJsonInt(sectionElement, "id") ?? 0;
            if (sectionId <= 0)
            {
                continue;
            }

            var questions = await GetItemtypeSubListAsync(
                credentials,
                sessionToken,
                GlpiFormSectionType,
                sectionId,
                GlpiFormQuestionType,
                cancellationToken);

            mappedSections.Add(new GlpiFormSection
            {
                Id = sectionId,
                Name = ReadJsonString(sectionElement, "name").Trim(),
                Rank = ReadJsonInt(sectionElement, "rank") ?? 0,
                Questions = questions
                    .Select(MapFormQuestion)
                    .Where(q => q.Id > 0)
                    .OrderBy(q => q.VerticalRank)
                    .ThenBy(q => q.HorizontalRank ?? 0)
                    .ToList(),
            });
        }

        return new GlpiFormSchema
        {
            Id = form.Id,
            Name = form.Name,
            Description = form.Description,
            CategoryId = form.CategoryId,
            Sections = mappedSections,
        };
    }

    public async Task<GlpiTicketResult> CreateTicketFromFormAnswersAsync(
        int formId,
        int entityId,
        IReadOnlyList<GlpiFormAnswerInput> answers,
        string? subjectOverride,
        string requesterEmail,
        CancellationToken cancellationToken = default)
    {
        var schema = await GetFormSchemaAsync(formId, cancellationToken)
            ?? throw new GlpiIntegrationException("Formulário GLPI não encontrado.");

        ValidateMandatoryAnswers(schema, answers);

        var credentials = credentialsResolver.Resolve();
        var requesterId = await ResolveUserIdAsync(credentials, requesterEmail, cancellationToken)
            ?? throw new GlpiRequesterNotFoundException(requesterEmail);

        var answerMap = answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().Value?.Trim() ?? string.Empty);

        var questionLookup = schema.Sections
            .SelectMany(s => s.Questions)
            .ToDictionary(q => q.Id);

        var content = BuildFormContentHtml(schema, answerMap, questionLookup);
        var subject = string.IsNullOrWhiteSpace(subjectOverride)
            ? BuildSubjectFromForm(schema, answerMap, questionLookup)
            : subjectOverride.Trim();

        var urgency = ResolveUrgency(answerMap, questionLookup);
        var categoryId = ResolveItilCategoryId(answerMap, questionLookup);
        var ticketType = InferTicketType(schema);

        if (entityId <= 0)
        {
            entityId = 1;
        }

        var sessionToken = await sessionManager.GetSessionTokenAsync(httpClient, credentials, cancellationToken);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(credentials.BaseUrl, "Ticket"));
            ApplySessionHeaders(request, credentials, sessionToken);

            var input = new Dictionary<string, object?>
            {
                ["name"] = subject,
                ["content"] = content,
                ["priority"] = urgency,
                ["urgency"] = urgency,
                ["type"] = ticketType,
                ["entities_id"] = entityId,
                ["_users_id_requester"] = requesterId,
            };

            if (categoryId > 0)
            {
                input["itilcategories_id"] = categoryId;
            }

            request.Content = JsonContent.Create(new { input });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await sessionManager.InvalidateSessionAsync(httpClient, credentials, cancellationToken);
                return await CreateTicketFromFormAnswersAsync(
                    formId,
                    entityId,
                    answers,
                    subjectOverride,
                    requesterEmail,
                    cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "GLPI form ticket creation failed for {Email} form {FormId}: {Status} {Body}",
                    requesterEmail,
                    formId,
                    (int)response.StatusCode,
                    body);
                throw new GlpiIntegrationException(
                    "O GLPI rejeitou a criação do chamado a partir do formulário. Verifique permissões e respostas.");
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
        catch (GlpiIntegrationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected GLPI form ticket error for {Email}", requesterEmail);
            throw new GlpiIntegrationException("Falha inesperada ao registrar o chamado no GLPI.");
        }
    }

    private async Task<IReadOnlyList<JsonElement>> GetItemtypeListAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string itemtype,
        CancellationToken cancellationToken)
    {
        var results = new List<JsonElement>();
        var start = 0;
        const int pageSize = 200;

        while (true)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{BuildUrl(credentials.BaseUrl, EncodeItemtype(itemtype))}?range={start}-{start + pageSize - 1}");
            ApplySessionHeaders(request, credentials, sessionToken);
            request.Headers.TryAddWithoutValidation("Range", $"{start}-{start + pageSize - 1}");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "GLPI list {Itemtype} failed: {Status}",
                    itemtype,
                    (int)response.StatusCode);
                break;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            var page = document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
            results.AddRange(page);
            if (page.Count < pageSize)
            {
                break;
            }

            start += pageSize;
        }

        return results;
    }

    private async Task<IReadOnlyList<JsonElement>> GetItemtypeSubListAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string parentType,
        int parentId,
        string childType,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUrl(
                credentials.BaseUrl,
                $"{EncodeItemtype(parentType)}/{parentId}/{EncodeItemtype(childType)}"));
        ApplySessionHeaders(request, credentials, sessionToken);
        request.Headers.TryAddWithoutValidation("Range", "0-999");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "GLPI {Parent}/{Id}/{Child} failed: {Status}",
                parentType,
                parentId,
                childType,
                (int)response.StatusCode);
            return [];
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private async Task<JsonElement?> GetItemAsync(
        GlpiRuntimeCredentials credentials,
        string sessionToken,
        string itemtype,
        int id,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUrl(credentials.BaseUrl, $"{EncodeItemtype(itemtype)}/{id}"));
        ApplySessionHeaders(request, credentials, sessionToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.Clone();
    }

    private static string EncodeItemtype(string itemtype) =>
        Uri.EscapeDataString(itemtype).Replace("%5C", "%5C", StringComparison.Ordinal);

    private static GlpiFormCategory MapFormCategory(JsonElement element) =>
        new()
        {
            Id = ReadJsonInt(element, "id") ?? 0,
            Name = ReadJsonString(element, "name").Trim(),
            CompleteName = NullIfEmpty(ReadJsonString(element, "completename")),
            ParentId = ReadJsonInt(element, "forms_categories_id") is int parent and > 0 ? parent : null,
            Level = ReadJsonInt(element, "level") ?? 0,
        };

    private static GlpiFormSummary MapFormSummary(JsonElement element)
    {
        var isActive = ReadJsonInt(element, "is_active") is null or 1;
        var isDeleted = ReadJsonInt(element, "is_deleted") == 1;
        var isDraft = ReadJsonInt(element, "is_draft") == 1;
        if (!isActive || isDeleted || isDraft)
        {
            return new GlpiFormSummary();
        }

        return new GlpiFormSummary
        {
            Id = ReadJsonInt(element, "id") ?? 0,
            Name = ReadJsonString(element, "name").Trim(),
            Description = NullIfEmpty(StripTags(ReadJsonString(element, "description"))),
            Illustration = NullIfEmpty(ReadJsonString(element, "illustration")),
            CategoryId = ReadJsonInt(element, "forms_categories_id") ?? 0,
            RenderLayout = NullIfEmpty(ReadJsonString(element, "render_layout")) ?? "single_page",
        };
    }

    private static GlpiFormQuestion MapFormQuestion(JsonElement element) =>
        new()
        {
            Id = ReadJsonInt(element, "id") ?? 0,
            Uuid = ReadJsonString(element, "uuid"),
            SectionId = ReadJsonInt(element, "forms_sections_id") ?? 0,
            Name = ReadJsonString(element, "name").Trim(),
            Type = ReadJsonString(element, "type"),
            IsMandatory = ReadJsonInt(element, "is_mandatory") == 1,
            VerticalRank = ReadJsonInt(element, "vertical_rank") ?? 0,
            HorizontalRank = ReadJsonInt(element, "horizontal_rank"),
            Description = NullIfEmpty(StripTags(ReadJsonString(element, "description"))),
            DefaultValue = NullIfEmpty(ReadJsonFlexible(element, "default_value")),
            ExtraDataJson = NullIfEmpty(ReadJsonFlexible(element, "extra_data")),
            VisibilityStrategy = ReadJsonString(element, "visibility_strategy"),
            ConditionsJson = NullIfEmpty(ReadJsonFlexible(element, "conditions")) ?? "[]",
        };

    /// <summary>
    /// GLPI Forms often returns default_value/extra_data as JSON objects (e.g. {"items_id":0}),
    /// not as strings. ReadJsonString alone would drop them and break field-kind detection.
    /// </summary>
    private static string ReadJsonFlexible(JsonElement item, string property)
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
            JsonValueKind.Object or JsonValueKind.Array => value.GetRawText(),
            _ => string.Empty,
        };
    }

    private static void ValidateMandatoryAnswers(
        GlpiFormSchema schema,
        IReadOnlyList<GlpiFormAnswerInput> answers)
    {
        var map = answers.ToDictionary(a => a.QuestionId, a => a.Value?.Trim() ?? string.Empty);
        foreach (var question in schema.Sections.SelectMany(s => s.Questions))
        {
            if (!question.IsMandatory)
            {
                continue;
            }

            if (question.Type.Contains("QuestionTypeFile", StringComparison.Ordinal))
            {
                // Anexos sobem em endpoint separado após criar o ticket.
                continue;
            }

            if (!map.TryGetValue(question.Id, out var value) || IsEmptyAnswer(value))
            {
                throw new ArgumentException($"Resposta obrigatória: {question.Name}");
            }
        }
    }

    private static bool IsEmptyAnswer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed is "0" or "-1")
        {
            return true;
        }

        if (!trimmed.StartsWith('{'))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.TryGetProperty("items_id", out var itemsId))
            {
                var id = itemsId.ValueKind == JsonValueKind.Number
                    ? itemsId.GetInt32()
                    : int.TryParse(itemsId.GetString(), out var parsed) ? parsed : 0;
                return id <= 0;
            }
        }
        catch (JsonException)
        {
            return true;
        }

        return false;
    }

    private static string BuildFormContentHtml(
        GlpiFormSchema schema,
        IReadOnlyDictionary<int, string> answers,
        IReadOnlyDictionary<int, GlpiFormQuestion> questions)
    {
        var sb = new StringBuilder();
        foreach (var section in schema.Sections)
        {
            var title = string.IsNullOrWhiteSpace(section.Name) ? " " : section.Name.Trim();
            sb.Append("<h2>").Append(HttpUtility.HtmlEncode(title)).Append("</h2>");
            sb.Append("<p>");
            var index = 1;
            foreach (var question in section.Questions)
            {
                if (question.Type.Contains("QuestionTypeFile", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!answers.TryGetValue(question.Id, out var raw) || string.IsNullOrWhiteSpace(raw))
                {
                    if (!question.IsMandatory)
                    {
                        continue;
                    }
                }

                var display = FormatAnswerForDisplay(question, raw ?? string.Empty);
                sb.Append("<b>")
                    .Append(index)
                    .Append(") ")
                    .Append(HttpUtility.HtmlEncode(question.Name.TrimEnd(':', ' ')))
                    .Append("</b>: ");

                if (question.Type.Contains("QuestionTypeLongText", StringComparison.Ordinal))
                {
                    sb.Append("<p>")
                        .Append(HttpUtility.HtmlEncode(display).Replace("\n", "<br>", StringComparison.Ordinal))
                        .Append("</p>");
                }
                else
                {
                    sb.Append(HttpUtility.HtmlEncode(display));
                }

                sb.Append("<br>");
                index++;
            }

            sb.Append("</p>");
        }

        return sb.ToString();
    }

    private static string BuildSubjectFromForm(
        GlpiFormSchema schema,
        IReadOnlyDictionary<int, string> answers,
        IReadOnlyDictionary<int, GlpiFormQuestion> questions)
    {
        foreach (var question in schema.Sections.SelectMany(s => s.Questions))
        {
            if (!question.Type.Contains("QuestionTypeShortText", StringComparison.Ordinal) &&
                !question.Type.Contains("QuestionTypeDropdown", StringComparison.Ordinal) &&
                !question.Type.Contains("QuestionTypeRadio", StringComparison.Ordinal))
            {
                continue;
            }

            if (!answers.TryGetValue(question.Id, out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var display = FormatAnswerForDisplay(question, value);
            if (!string.IsNullOrWhiteSpace(display))
            {
                var subject = $"{schema.Name} — {display}";
                return subject.Length <= 120 ? subject : subject[..120];
            }
        }

        return schema.Name.Length <= 120 ? schema.Name : schema.Name[..120];
    }

    private static string FormatAnswerForDisplay(GlpiFormQuestion question, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var options = ParseOptions(question.ExtraDataJson);
        if (options.Count == 0)
        {
            return raw.Trim();
        }

        if (options.TryGetValue(raw.Trim(), out var label))
        {
            return label;
        }

        return raw.Trim();
    }

    private static Dictionary<string, string> ParseOptions(string? extraDataJson)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(extraDataJson))
        {
            return result;
        }

        try
        {
            using var document = JsonDocument.Parse(extraDataJson);
            if (!document.RootElement.TryGetProperty("options", out var options) ||
                options.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in options.EnumerateObject())
            {
                result[property.Name] = property.Value.GetString()?.Trim() ?? property.Name;
            }
        }
        catch (JsonException)
        {
            // ignore malformed extra_data
        }

        return result;
    }

    private static int ResolveUrgency(
        IReadOnlyDictionary<int, string> answers,
        IReadOnlyDictionary<int, GlpiFormQuestion> questions)
    {
        foreach (var (questionId, question) in questions)
        {
            if (!question.Type.Contains("QuestionTypeUrgency", StringComparison.Ordinal) &&
                !question.Name.Contains("impacto", StringComparison.OrdinalIgnoreCase) &&
                !question.Name.Contains("urgência", StringComparison.OrdinalIgnoreCase) &&
                !question.Name.Contains("urgencia", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!answers.TryGetValue(questionId, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (int.TryParse(raw, out var numeric) && numeric is >= 1 and <= 5)
            {
                return numeric;
            }

            var label = FormatAnswerForDisplay(question, raw).ToLowerInvariant();
            if (label.Contains("muito alta") || label.Contains("crítica") || label.Contains("critica")) return 5;
            if (label.Contains("alta") || label.Contains("alto")) return 4;
            if (label.Contains("média") || label.Contains("media") || label.Contains("médio") || label.Contains("medio")) return 3;
            if (label.Contains("baixa") || label.Contains("baixo")) return 2;
        }

        return 3;
    }

    private static int ResolveItilCategoryId(
        IReadOnlyDictionary<int, string> answers,
        IReadOnlyDictionary<int, GlpiFormQuestion> questions)
    {
        foreach (var (questionId, question) in questions)
        {
            if (!question.Type.Contains("QuestionTypeItemDropdown", StringComparison.Ordinal) &&
                !question.Type.Contains("ITILCategory", StringComparison.Ordinal))
            {
                // Dropdown "módulo" is not always ITIL category id.
                continue;
            }

            if (question.ExtraDataJson is not null &&
                question.ExtraDataJson.Contains("ITILCategory", StringComparison.Ordinal) &&
                answers.TryGetValue(questionId, out var raw) &&
                int.TryParse(raw, out var categoryId) &&
                categoryId > 0)
            {
                return categoryId;
            }
        }

        return 0;
    }

    private static int InferTicketType(GlpiFormSchema schema)
    {
        var haystack = $"{schema.Name} {schema.Description}".ToLowerInvariant();
        return haystack.Contains("incident") || haystack.Contains("incidente") ? 1 : 2;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string StripTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex
            .Replace(value, "<[^>]+>", " ")
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}
