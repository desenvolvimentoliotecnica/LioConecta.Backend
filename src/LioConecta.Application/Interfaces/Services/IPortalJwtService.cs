using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Interfaces.Services;

public interface IPortalJwtService
{
    (string Token, int ExpiresInSeconds) CreateToken(Person person, IReadOnlyList<UserRole> roles);
}
