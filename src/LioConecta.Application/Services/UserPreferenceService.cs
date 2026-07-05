using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;

namespace LioConecta.Application.Services;

public sealed class UserPreferenceService(
    IUserPreferenceRepository userPreferenceRepository,
    ICurrentUserService currentUserService) : IUserPreferenceService
{
    public async Task<UserPreferencesDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var preference = await userPreferenceRepository.GetByPersonIdAsync(personId, cancellationToken);
        return ToDto(preference);
    }

    public async Task<UserPreferencesDto> UpdateAsync(
        UpdatePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var personId = await currentUserService.GetPersonIdAsync(cancellationToken);
        var preference = await userPreferenceRepository.GetByPersonIdAsync(personId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var isNew = preference is null;

        preference ??= new UserPreference
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CreatedAt = now
        };

        if (request.Bookmarks is not null)
        {
            preference.BookmarksJson = JsonMapper.SerializeStringList(request.Bookmarks);
        }

        if (request.Favorites is not null)
        {
            preference.FavoritesJson = JsonMapper.SerializeStringList(request.Favorites);
        }

        if (request.Shortcuts is not null)
        {
            preference.ShortcutsJson = JsonMapper.SerializeStringList(request.Shortcuts);
        }

        preference.UpdatedAt = now;

        if (isNew)
        {
            await userPreferenceRepository.AddAsync(preference, cancellationToken);
        }
        else
        {
            await userPreferenceRepository.UpdateAsync(preference, cancellationToken);
        }

        return ToDto(preference);
    }

    private static UserPreferencesDto ToDto(UserPreference? preference)
        => new(
            JsonMapper.DeserializeStringList(preference?.BookmarksJson),
            JsonMapper.DeserializeStringList(preference?.FavoritesJson),
            JsonMapper.DeserializeStringList(preference?.ShortcutsJson));
}
