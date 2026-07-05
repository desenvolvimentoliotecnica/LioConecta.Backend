using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IComunicadoService
{
    Task<PagedResult<ComunicadoListItemDto>> ListAsync(
        ComunicadoKind? kind,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ComunicadoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
}
