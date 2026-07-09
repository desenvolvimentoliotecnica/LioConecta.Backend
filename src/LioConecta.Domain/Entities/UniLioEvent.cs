using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class UniLioEvent : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string EventType { get; set; } = "webinar";

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public Guid? InstructorPersonId { get; set; }

    public int MaxAttendees { get; set; }

    public string? MeetingUrl { get; set; }

    public Person? Instructor { get; set; }

    public ICollection<UniLioEventRegistration> Registrations { get; set; } = [];
}
