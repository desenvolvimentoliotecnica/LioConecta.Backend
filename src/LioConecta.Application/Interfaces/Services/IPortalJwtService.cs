using LioConecta.Application.DTOs;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IPortalJwtService
{
    (string Token, int ExpiresInSeconds) CreateToken(Person person, IReadOnlyList<UserRole> roles);

    (string Token, int ExpiresInSeconds) CreateToken(
        Person person,
        IReadOnlyList<UserRole> roles,
        RbacSubjectType subjectType,
        Guid subjectId,
        string securityStamp,
        bool isTestUser,
        IReadOnlyList<EffectivePermissionDto> permissions);
}
