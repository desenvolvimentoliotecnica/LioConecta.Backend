using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IComunicadoService
{
    Task<PagedResult<ComunicadoListItemDto>> ListAsync(
        ComunicadoKind? kind,
        bool archivedOnly,
        bool manage,
        CursorPageRequest request,
        CancellationToken cancellationToken = default);

    Task<ComunicadoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ComunicadoDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ComunicadoHubDto> GetHubAsync(CancellationToken cancellationToken = default);
    Task<ComunicadoDto> CreateAsync(CreateComunicadoRequest request, CancellationToken cancellationToken = default);
    Task<ComunicadoDto> UpdateAsync(Guid id, UpdateComunicadoRequest request, CancellationToken cancellationToken = default);
    Task<ComunicadoDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComunicadoDto> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ComunicadoDto> ScheduleAsync(Guid id, DateTimeOffset scheduledAt, CancellationToken cancellationToken = default);
    Task<ComunicadoMetricsDto> GetMetricsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> PublishScheduledAsync(CancellationToken cancellationToken = default);
}
