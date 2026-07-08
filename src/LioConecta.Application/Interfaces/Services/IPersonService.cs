using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IPersonService
{
    Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default);

    Task<PersonProfileDto?> GetProfileAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonSummaryDto>> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default);

    Task<OrgChartDto> GetOrgChartAsync(CancellationToken cancellationToken = default);

    Task<PersonHierarchyDto?> GetHierarchyAsync(string slug, CancellationToken cancellationToken = default);

    Task<PersonDirectoryDto> GetDirectoryAsync(
        string? query,
        string? departmentId,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnAboutAsync(string? aboutMe, CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnSkillsAsync(
        IReadOnlyList<PersonSkillDto> skills,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnLanguagesAsync(
        IReadOnlyList<PersonLanguageDto> languages,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnLinksAsync(
        IReadOnlyDictionary<string, string> links,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnPronounsAsync(string? pronouns, CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnAvailabilityAsync(
        PersonAvailabilityDto availability,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnMentorshipAsync(
        PersonContactRefDto? mentor,
        PersonContactRefDto? buddy,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnProjectsAsync(
        IReadOnlyList<PersonProjectDto> projects,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnEducationAsync(
        IReadOnlyList<PersonEducationDto> education,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnCertificationsAsync(
        IReadOnlyList<PersonCertificationDto> certifications,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnCareerHistoryAsync(
        IReadOnlyList<PersonCareerHistoryItemDto> careerHistory,
        CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdateOwnAvatarAsync(string? photoUrl, CancellationToken cancellationToken = default);

    Task<PersonProfileDto> UpdatePersonAvatarAsync(
        string personKey,
        string? photoUrl,
        CancellationToken cancellationToken = default);
}
