using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Services;

public sealed class ComunicadoService(
    IComunicadoRepository comunicadoRepository,
    ICurrentUserService currentUserService) : IComunicadoService
{
    public async Task<PagedResult<ComunicadoListItemDto>> ListAsync(
        ComunicadoKind? kind,
        bool archivedOnly,
        CursorPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var page = await comunicadoRepository.GetPageAsync(kind, archivedOnly, request, cancellationToken);
        var items = new List<ComunicadoListItemDto>();

        foreach (var comunicado in page.Items)
        {
            var isRead = await comunicadoRepository.IsReadAsync(comunicado.Id, viewerId, cancellationToken);
            items.Add(ComunicadoMapper.ToListItem(comunicado, isRead));
        }

        return PagedResult<ComunicadoListItemDto>.FromItems(items, page.NextCursor, page.HasMore);
    }

    public async Task<ComunicadoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var comunicado = await comunicadoRepository.GetByIdAsync(id, cancellationToken);
        if (comunicado is null)
        {
            return null;
        }

        var isRead = await comunicadoRepository.IsReadAsync(id, viewerId, cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, isRead);
    }

    public async Task<ComunicadoDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var comunicado = await comunicadoRepository.GetBySlugAsync(normalized, cancellationToken);
        if (comunicado is null)
        {
            return null;
        }

        var isRead = await comunicadoRepository.IsReadAsync(comunicado.Id, viewerId, cancellationToken);
        return ComunicadoMapper.ToDto(comunicado, isRead);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var viewerId = await currentUserService.GetPersonIdAsync(cancellationToken);
        _ = await comunicadoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Comunicado {id} was not found.");

        await comunicadoRepository.MarkAsReadAsync(id, viewerId, cancellationToken);
    }
}
