using LioConecta.Application;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using LioConecta.Infrastructure;
using LioConecta.Infrastructure.Configuration;
using LioConecta.Workers.Jobs;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    var devFallback = builder.Environment.IsDevelopment()
        ? "Host=localhost;Port=5433;Database=lioconecta;Username=lioconecta;Password=lioconecta_dev"
        : null;

    var bootstrapConnection = BootstrapConnection.Resolve(devFallback);
    var values = await AppSettingsSeeder.LoadValuesAsync(bootstrapConnection);
    var settingsProvider = new AppSettingsProvider();
    settingsProvider.Reload(values);

    builder.Services.AddSingleton<LioConecta.Application.Interfaces.Services.IAppSettingsProvider>(settingsProvider);

    builder.Services.AddSerilog((services, configuration) => configuration
        .MinimumLevel.Is(LogEventLevel.Information)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(settingsProvider);
    builder.Services.AddSingleton<INotificationBroadcaster, NoOpNotificationBroadcaster>();

    builder.Services.AddHostedService<TotvsSyncWorker>();
    builder.Services.AddHostedService<GraphSyncWorker>();
    builder.Services.AddHostedService<PollClosureWorker>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
