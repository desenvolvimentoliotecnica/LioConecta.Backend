using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioCertificate : BaseEntity
{
    public Guid PersonId { get; set; }

    public Guid CourseId { get; set; }

    public string CertificateCode { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; }

    public Person Person { get; set; } = null!;

    public UniLioCourse Course { get; set; } = null!;
}
