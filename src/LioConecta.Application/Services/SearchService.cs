using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Services;

public sealed class SearchService(
    ISearchRepository searchRepository,
    ICurrentUserService currentUserService) : ISearchService
{
    public async Task<SearchResultDto> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResultDto([], [], []);
        }

        _ = await currentUserService.GetPersonIdAsync(cancellationToken);

        var peopleTask = searchRepository.SearchPeopleAsync(query, limit, cancellationToken);
        var documentsTask = searchRepository.SearchDocumentsAsync(query, limit, cancellationToken);
        var comunicadosTask = searchRepository.SearchComunicadosAsync(query, limit, cancellationToken);

        await Task.WhenAll(peopleTask, documentsTask, comunicadosTask);

        var people = peopleTask.Result.Select(PersonMapper.ToSummary).ToList();
        var documents = documentsTask.Result.Select(DocumentMapper.ToDto).ToList();
        var comunicados = comunicadosTask.Result
            .Select(c => ComunicadoMapper.ToListItem(c, isRead: false))
            .ToList();

        return new SearchResultDto(people, documents, comunicados);
    }
}
