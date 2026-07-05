using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class EmailConfiguration : BaseEntity
{
    public bool IsEnabled { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public string SmtpUsername { get; set; } = string.Empty;

    public string? SmtpPasswordProtected { get; set; }

    public bool UseStartTls { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxAttempts { get; set; } = 5;

    public int InitialRetryDelaySeconds { get; set; } = 60;

    public int MaxRetryDelaySeconds { get; set; } = 21600;

    public int DispatchBatchSize { get; set; } = 20;

    public int DispatchIntervalSeconds { get; set; } = 30;

    public Guid? UpdatedById { get; set; }

    public Person? UpdatedBy { get; set; }
}
