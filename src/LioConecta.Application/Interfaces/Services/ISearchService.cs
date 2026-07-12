using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(
        string query,
        int limit = 20,
        string? types = null,
        CancellationToken cancellationToken = default);
}
