using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IEmailConfigurationService
{
    Task<EmailConfigurationDto> GetAsync(CancellationToken cancellationToken);

    Task<EmailConfigurationDto> SaveAsync(
        UpsertEmailConfigurationRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken);

    Task<EmailRuntimeConfiguration> GetRuntimeConfigurationAsync(CancellationToken cancellationToken);

    Task<EmailConnectionTestResponse> TestConnectionAsync(
        EmailSmtpTestRequest request,
        CancellationToken cancellationToken);

    Task EnsureDefaultConfigurationAsync(CancellationToken cancellationToken);
}
