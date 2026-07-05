using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;

namespace LioConecta.Application.Services;

public sealed class PersonService(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService) : IPersonService
{
    public async Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var person = await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Current user profile was not found.");

        var roles = await currentUserService.GetRolesAsync(cancellationToken);
        return PersonMapper.ToMe(person, roles);
    }

    public async Task<PersonProfileDto?> GetProfileAsync(string slug, CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetBySlugAsync(slug, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var viewerContext = await currentUserService.GetViewerContextAsync(person.Id, cancellationToken);
        return PersonMapper.ToProfile(person, viewerContext);
    }

    public async Task<IReadOnlyList<PersonSummaryDto>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var people = await personRepository.SearchAsync(query, limit, cancellationToken);
        return people.Select(PersonMapper.ToSummary).ToList();
    }

    public async Task<OrgChartDto> GetOrgChartAsync(CancellationToken cancellationToken = default)
    {
        var people = await personRepository.GetOrgChartPeopleAsync(cancellationToken);
        var nodes = people.Select(PersonMapper.ToOrgChartNode).ToList();
        var rootId = people.FirstOrDefault(p => p.ManagerId is null)?.Id;
        return new OrgChartDto(nodes, rootId);
    }
}
