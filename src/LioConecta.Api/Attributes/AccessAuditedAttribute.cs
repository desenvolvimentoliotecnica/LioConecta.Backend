namespace LioConecta.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AccessAuditedAttribute : Attribute
{
    public string? Resource { get; init; }

    public string? Action { get; init; }

    public string EventName { get; init; } = Application.Common.Observability.ObservabilityEventNames.Resource.Viewed;
}
