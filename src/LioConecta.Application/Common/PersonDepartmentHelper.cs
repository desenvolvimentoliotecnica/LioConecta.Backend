using LioConecta.Domain.Entities;

namespace LioConecta.Application.Common;

public static class PersonDepartmentHelper
{
    /// <summary>
    /// Graph directory sync writes <see cref="Person.Dept"/>; seed FK must not override it.
    /// </summary>
    public static string? GetName(Person person)
    {
        if (!string.IsNullOrWhiteSpace(person.Dept))
        {
            return person.Dept.Trim();
        }

        return person.Department?.Name;
    }
}
