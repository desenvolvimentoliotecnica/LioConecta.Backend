using System.Net.Http.Headers;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class GraphAuthDelegatingHandler(GraphTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Microsoft Graph is not configured. Set graph.tenant_id, graph.client_id and graph.client_secret.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
