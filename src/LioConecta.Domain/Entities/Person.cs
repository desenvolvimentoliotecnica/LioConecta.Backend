using LioConecta.Domain.Common;

namespace LioConecta.Domain.Entities;

public class Person : BaseEntity
{
    public string Slug { get; set; } = string.Empty;

    public Guid? AzureAdObjectId { get; set; }

    public string? OrgChartId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Dept { get; set; }

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? EmployeeId { get; set; }

    public string? Phone { get; set; }

    public string? Location { get; set; }

    public string? TeamsUpn { get; set; }

    public Guid? ManagerId { get; set; }

    public Person? Manager { get; set; }

    public ICollection<Person> DirectReports { get; set; } = [];

    public string? PhotoUrl { get; set; }

    public DateOnly? BirthDate { get; set; }

    public DateOnly? HireDate { get; set; }

    public string? Status { get; set; }

    public string TagsJson { get; set; } = "[]";

    public string? PersonalDataJson { get; set; }

    public string SkillsJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;
}
