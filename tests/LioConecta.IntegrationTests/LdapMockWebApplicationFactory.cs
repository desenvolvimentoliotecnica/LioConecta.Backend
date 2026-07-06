using LioConecta.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LioConecta.IntegrationTests;

public sealed class LdapMockWebApplicationFactory : LioConectaWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILdapAuthService>();
            services.AddSingleton<ILdapAuthService, FakeLdapAuthService>();
        });
    }
}
