using LioConecta.Application.DTOs;

namespace LioConecta.Application.Interfaces.Services;

public interface IChatConfigurationService
{
    Task<ChatConnectionTestResponse> TestConnectionAsync(
        TestChatConnectionRequest request,
        CancellationToken cancellationToken = default);
}
