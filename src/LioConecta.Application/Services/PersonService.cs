using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class PersonService(
    IPersonRepository personRepository,
    ICurrentUserService currentUserService,
    ITotvsRmEmployeeRepository employeeRepository,
    IAppSettingsProvider settingsProvider) : IPersonService
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
        var chapa = TotvsRmChapaNormalizer.Normalize(person.EmployeeId);
        if (!string.IsNullOrWhiteSpace(chapa))
        {
            var rmProfile = await employeeRepository.GetProfileByChapaAsync(chapa, cancellationToken);
            if (rmProfile is not null)
            {
                return PersonRmProfileMapper.ApplyRmProfile(person, rmProfile, viewerContext);
            }
        }

        return PersonMapper.ToProfile(person, viewerContext);
    }

    public async Task<PersonProfileDto> UpdateOwnAboutAsync(
        string? aboutMe,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        var trimmed = string.IsNullOrWhiteSpace(aboutMe) ? null : aboutMe.Trim();
        if (trimmed is { Length: > 2000 })
        {
            throw new InvalidOperationException("O texto sobre você deve ter no máximo 2000 caracteres.");
        }

        if (trimmed is null)
        {
            personalData.Remove("aboutMe");
            personalData.Remove("bio");
        }
        else
        {
            personalData["aboutMe"] = trimmed;
            personalData["bio"] = trimmed;
        }

        PersonProfileEditor.SavePersonalData(person, personalData);
        await personRepository.UpdateAsync(person, cancellationToken);
        return await GetProfileAsync(person.Slug, cancellationToken)
            ?? throw new InvalidOperationException("Updated profile could not be loaded.");
    }

    public async Task<PersonProfileDto> UpdateOwnSkillsAsync(
        IReadOnlyList<PersonSkillDto> skills,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var normalized = PersonProfileEditor.NormalizeSkills(skills);
        person.SkillsJson = JsonMapper.SerializeSkills(normalized);
        await personRepository.UpdateAsync(person, cancellationToken);
        return await GetProfileAsync(person.Slug, cancellationToken)
            ?? throw new InvalidOperationException("Updated profile could not be loaded.");
    }

    public async Task<PersonProfileDto> UpdateOwnLanguagesAsync(
        IReadOnlyList<PersonLanguageDto> languages,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        var normalized = PersonProfileEditor.NormalizeLanguages(languages);
        personalData["languages"] = normalized
            .Select(language => new Dictionary<string, string>
            {
                ["name"] = language.Name,
                ["level"] = language.Level,
            })
            .ToList();
        PersonProfileEditor.SavePersonalData(person, personalData);
        await personRepository.UpdateAsync(person, cancellationToken);
        return await GetProfileAsync(person.Slug, cancellationToken)
            ?? throw new InvalidOperationException("Updated profile could not be loaded.");
    }

    public async Task<PersonProfileDto> UpdateOwnLinksAsync(
        IReadOnlyDictionary<string, string> links,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        var normalized = PersonProfileEditor.NormalizeLinks(links);
        personalData["links"] = normalized.ToDictionary(
            pair => pair.Key,
            pair => (object?)pair.Value,
            StringComparer.OrdinalIgnoreCase);
        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnPronounsAsync(
        string? pronouns,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        var normalized = PersonProfileEditor.NormalizePronouns(pronouns);
        if (normalized is null)
        {
            personalData.Remove("pronouns");
        }
        else
        {
            personalData["pronouns"] = normalized;
        }

        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnAvailabilityAsync(
        PersonAvailabilityDto availability,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        personalData["availability"] = PersonProfileEditor.ToStoredObject(
            PersonProfileEditor.NormalizeAvailability(availability));
        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnMentorshipAsync(
        PersonContactRefDto? mentor,
        PersonContactRefDto? buddy,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        var normalizedMentor = PersonProfileEditor.NormalizeContactRef(mentor);
        var normalizedBuddy = PersonProfileEditor.NormalizeContactRef(buddy);

        if (normalizedMentor is null)
        {
            personalData.Remove("mentor");
        }
        else
        {
            personalData["mentor"] = PersonProfileEditor.ToStoredObject(normalizedMentor);
        }

        if (normalizedBuddy is null)
        {
            personalData.Remove("buddy");
        }
        else
        {
            personalData["buddy"] = PersonProfileEditor.ToStoredObject(normalizedBuddy);
        }

        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnProjectsAsync(
        IReadOnlyList<PersonProjectDto> projects,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        personalData["projects"] = PersonProfileEditor.ToStoredList(
            PersonProfileEditor.NormalizeProjects(projects));
        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnEducationAsync(
        IReadOnlyList<PersonEducationDto> education,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        personalData["education"] = PersonProfileEditor.ToStoredList(
            PersonProfileEditor.NormalizeEducation(education));
        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnCertificationsAsync(
        IReadOnlyList<PersonCertificationDto> certifications,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        personalData["certifications"] = PersonProfileEditor.ToStoredList(
            PersonProfileEditor.NormalizeCertifications(certifications));
        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    public async Task<PersonProfileDto> UpdateOwnCareerHistoryAsync(
        IReadOnlyList<PersonCareerHistoryItemDto> careerHistory,
        CancellationToken cancellationToken = default)
    {
        var person = await GetEditablePersonAsync(cancellationToken);
        var personalData = PersonProfileEditor.LoadPersonalData(person);
        personalData["careerHistory"] = PersonProfileEditor.ToStoredList(
            PersonProfileEditor.NormalizeCareerHistory(careerHistory));
        PersonProfileEditor.SavePersonalData(person, personalData);
        return await ReloadProfileAsync(person, cancellationToken);
    }

    private async Task<PersonProfileDto> ReloadProfileAsync(
        Domain.Entities.Person person,
        CancellationToken cancellationToken)
    {
        await personRepository.UpdateAsync(person, cancellationToken);
        return await GetProfileAsync(person.Slug, cancellationToken)
            ?? throw new InvalidOperationException("Updated profile could not be loaded.");
    }

    private async Task<Domain.Entities.Person> GetEditablePersonAsync(CancellationToken cancellationToken)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        return await personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new InvalidOperationException("Current user profile was not found.");
    }

    public async Task<IReadOnlyList<PersonSummaryDto>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var people = await personRepository.SearchAsync(query, limit, cancellationToken);
        return people.Select(p => PersonMapper.ToSummary(p)).ToList();
    }

    public async Task<OrgChartDto> GetOrgChartAsync(CancellationToken cancellationToken = default)
    {
        var people = await personRepository.GetOrgChartPeopleAsync(cancellationToken);
        var chartPeople = people.Where(p => p.ManagerId is not null).ToList();
        var unassignedPeople = people.Where(p => p.ManagerId is null).ToList();
        var idSet = chartPeople.Select(p => p.Id).ToHashSet();
        var nodes = chartPeople
            .Select(p =>
            {
                var isOrphan = !idSet.Contains(p.ManagerId!.Value);
                return PersonMapper.ToOrgChartNode(p, isOrphan);
            })
            .ToList();

        var unassignedNodes = unassignedPeople
            .Select(p => PersonMapper.ToOrgChartNode(p))
            .ToList();

        var rootIds = chartPeople
            .Where(p => !idSet.Contains(p.ManagerId!.Value))
            .Select(p => p.Id)
            .ToList();

        DateTimeOffset? syncedAt = null;
        var syncedRaw = settingsProvider.GetString(AppSettingKeys.GraphDirectoryLastSyncUtc);
        if (DateTimeOffset.TryParse(syncedRaw, out var parsed))
        {
            syncedAt = parsed;
        }

        return new OrgChartDto(
            nodes,
            rootIds.FirstOrDefault(),
            people.Count,
            rootIds,
            nodes.Count(n => n.IsOrphan),
            syncedAt,
            unassignedNodes,
            unassignedNodes.Count);
    }

    public async Task<PersonHierarchyDto?> GetHierarchyAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetBySlugAsync(slug, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var chain = await BuildManagerChainAsync(person, cancellationToken);
        var peers = person.ManagerId is null
            ? []
            : await personRepository.GetPeersAsync(person.Id, person.ManagerId.Value, cancellationToken);
        var directReports = await personRepository.GetDirectReportsAsync(person.Id, cancellationToken);

        return new PersonHierarchyDto(
            person.Manager is null ? null : PersonMapper.ToHierarchyMember(person.Manager),
            chain.Select(PersonMapper.ToHierarchyMember).ToList(),
            peers.Select(PersonMapper.ToHierarchyMember).ToList(),
            directReports.Select(PersonMapper.ToHierarchyMember).ToList(),
            directReports.Count);
    }

    private async Task<IReadOnlyList<Person>> BuildManagerChainAsync(
        Person person,
        CancellationToken cancellationToken)
    {
        var chain = new List<Person>();
        var visited = new HashSet<Guid> { person.Id };
        var managerId = person.ManagerId;

        while (managerId is not null && !visited.Contains(managerId.Value))
        {
            var manager = await personRepository.GetByIdAsync(managerId.Value, cancellationToken);
            if (manager is null)
            {
                break;
            }

            visited.Add(manager.Id);
            chain.Insert(0, manager);
            managerId = manager.ManagerId;
        }

        return chain;
    }

    public async Task<PersonDirectoryDto> GetDirectoryAsync(
        string? query,
        string? departmentId,
        CancellationToken cancellationToken = default)
    {
        var people = await personRepository.GetDirectoryPeopleAsync(query, departmentId, cancellationToken);
        var departments = people
            .GroupBy(p => PersonSlugHelper.DepartmentIdFromName(PersonDepartmentHelper.GetName(p)))
            .OrderBy(group => PersonDepartmentHelper.GetName(group.First()) ?? "Sem departamento")
            .Select(group =>
            {
                var name = PersonDepartmentHelper.GetName(group.First()) ?? "Sem departamento";
                var entries = group
                    .OrderBy(p => p.Name)
                    .Select(PersonMapper.ToDirectoryEntry)
                    .ToList();
                return new PersonDirectoryDepartmentDto(group.Key, name, entries.Count, entries);
            })
            .ToList();

        DateTimeOffset? syncedAt = null;
        var syncedRaw = settingsProvider.GetString(AppSettingKeys.GraphDirectoryLastSyncUtc);
        if (DateTimeOffset.TryParse(syncedRaw, out var parsed))
        {
            syncedAt = parsed;
        }

        return new PersonDirectoryDto(syncedAt, people.Count, departments);
    }
}
