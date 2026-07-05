using LioConecta.Application.Common.Audit;

namespace LioConecta.Infrastructure.Services;

public sealed class AuditContextAccessor : IAuditContextAccessor
{
    private static readonly AsyncLocal<AuditContext?> CurrentContext = new();

    public AuditContext? Current => CurrentContext.Value;

    public void Set(AuditContext context) => CurrentContext.Value = context;
}
