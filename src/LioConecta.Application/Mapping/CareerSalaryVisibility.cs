using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Mapping;

public static class CareerSalaryVisibility
{
    private static readonly string[] DirectorTitleKeywords =
    [
        "diretor",
        "director",
        "ceo",
        "presidente",
        "vice-presidente",
        "vice presidente",
    ];

    public static bool CanViewSalaryValues(
        ViewerContext viewerContext,
        IReadOnlyList<UserRole> roles,
        Person? viewer,
        bool viewerHasDirectReports)
    {
        if (viewerContext is ViewerContext.Self or ViewerContext.HR or ViewerContext.Admin)
        {
            return true;
        }

        if (roles.Contains(UserRole.Manager))
        {
            return true;
        }

        if (viewer is null)
        {
            return false;
        }

        if (viewerHasDirectReports || IsDirector(viewer))
        {
            return true;
        }

        return false;
    }

    public static bool IsDirector(Person person)
    {
        var tags = JsonMapper.DeserializeStringList(person.TagsJson);
        if (tags.Any(tag =>
                tag.Equals("ceo", StringComparison.OrdinalIgnoreCase)
                || tag.Equals("leadership", StringComparison.OrdinalIgnoreCase)
                || tag.Equals("lideranca", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var title = person.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        return DirectorTitleKeywords.Any(keyword =>
            title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
