using LioConecta.Application.Common;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Infrastructure.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LioConecta.Api.Extensions;

public static class ObservabilityServiceExtensions
{
    public static IServiceCollection AddLioConectaObservability(
        this IServiceCollection services,
        IAppSettingsProvider settings,
        IWebHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return services;
        }

        if (!settings.GetBool(AppSettingKeys.ObservabilityOtelEnabled, true))
        {
            return services;
        }

        var serviceName = settings.GetString(AppSettingKeys.ObservabilityOtelServiceName, "LioConecta.Api");
        var otlpEndpoint = settings.GetString(AppSettingKeys.ObservabilityOtelOtlpEndpoint, "http://localhost:4317");
        var sampleRatio = ParseSampleRatio(settings.GetString(AppSettingKeys.ObservabilityOtelTraceSampleRatio, "1.0"));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(sampleRatio)))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            var correlationId = request.Headers[Application.Common.Audit.AuditContext.CorrelationHeaderName]
                                .FirstOrDefault();
                            if (!string.IsNullOrEmpty(correlationId))
                            {
                                activity.SetTag("correlation.id", correlationId);
                            }
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = false;
                    });

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (settings.GetBool(AppSettingKeys.ObservabilityOtelPrometheusEnabled, true))
                {
                    metrics.AddPrometheusExporter();
                }
            });

        return services;
    }

    public static WebApplication UseLioConectaObservability(
        this WebApplication app,
        IAppSettingsProvider settings)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return app;
        }

        if (!settings.GetBool(AppSettingKeys.ObservabilityOtelEnabled, true))
        {
            return app;
        }

        if (settings.GetBool(AppSettingKeys.ObservabilityOtelPrometheusEnabled, true))
        {
            app.MapPrometheusScrapingEndpoint("/metrics");
        }

        return app;
    }

    private static double ParseSampleRatio(string value) =>
        double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var ratio)
            ? Math.Clamp(ratio, 0, 1)
            : 1.0;
}
