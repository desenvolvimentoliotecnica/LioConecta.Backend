namespace LioConecta.Application.Common.Audit;

public interface IAuditContextAccessor
{
    AuditContext? Current { get; }

    void Set(AuditContext context);
}
