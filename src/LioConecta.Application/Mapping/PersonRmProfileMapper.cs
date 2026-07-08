using System.Text.Json;
using LioConecta.Application.Common;
using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations.Models;
using LioConecta.Application.Services;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;

namespace LioConecta.Application.Mapping;

public static class PersonRmProfileMapper
{
    public static PersonProfileDto ApplyRmProfile(
        Person person,
        RmEmployeeProfileRecord rm,
        RmEmployeeCareerHistoryData careerHistory,
        ViewerContext viewerContext,
        bool includeSalaryValues)
    {
        var showSensitive = viewerContext is ViewerContext.Self or ViewerContext.HR or ViewerContext.Admin;
        var hireDate = rm.DataAdmissao.HasValue
            ? DateOnly.FromDateTime(rm.DataAdmissao.Value.Date)
            : person.HireDate;
        var location = FormatCityState(rm.Cidade, rm.Estado) ?? person.Location;

        var personalData = BuildPersonalData(
            rm,
            careerHistory,
            showSensitive,
            includeSalaryValues,
            hireDate,
            rm.FuncaoDescricao,
            rm.SecaoDescricao);
        PersonProfileEditor.MergeEditableFields(person, personalData);
        var skills = JsonMapper.DeserializeSkills(person.SkillsJson);
        var bio = PersonProfileEditor.ReadBio(PersonProfileEditor.LoadPersonalData(person))
            ?? PersonProfileEditor.ReadBio(personalData);

        return new PersonProfileDto(
            person.Id,
            person.Slug,
            person.OrgChartId,
            string.IsNullOrWhiteSpace(rm.Nome) ? person.Name : rm.Nome.Trim(),
            string.IsNullOrWhiteSpace(rm.FuncaoDescricao) ? person.Title : rm.FuncaoDescricao.Trim(),
            showSensitive ? person.Email : MaskEmail(person.Email),
            showSensitive ? FirstNonEmpty(rm.Telefone, person.Phone) : null,
            location,
            PersonPhotoResolver.ResolveEffectivePhotoUrl(person),
            string.IsNullOrWhiteSpace(rm.SecaoDescricao)
                ? PersonDepartmentHelper.GetName(person)
                : rm.SecaoDescricao.Trim(),
            string.IsNullOrWhiteSpace(rm.GestorNome) ? person.Manager?.Name : rm.GestorNome.Trim(),
            person.Manager?.Slug,
            string.IsNullOrWhiteSpace(person.TeamsUpn) ? person.Email : person.TeamsUpn,
            bio,
            ReadPersonalString(personalData, "pronouns"),
            null,
            hireDate,
            person.Status,
            JsonMapper.DeserializeStringList(person.TagsJson),
            skills,
            personalData,
            viewerContext,
            PersonPhotoResolver.GetGraphPhotoUrl(person),
            PersonPhotoResolver.GetPortalAvatarUrl(person));
    }

    private static string? ReadPersonalString(IReadOnlyDictionary<string, object?> personalData, string key)
    {
        if (!personalData.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => value.ToString(),
        };
    }

    private static Dictionary<string, object?> BuildPersonalData(
        RmEmployeeProfileRecord rm,
        RmEmployeeCareerHistoryData careerHistory,
        bool showSensitive,
        bool includeSalaryValues,
        DateOnly? hireDate,
        string? roleTitle,
        string? department)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["visibility"] = "public",
            ["source"] = "totvs-rm",
        };

        if (!string.IsNullOrWhiteSpace(rm.Nome))
        {
            data["fullName"] = rm.Nome.Trim();
        }

        if (!string.IsNullOrWhiteSpace(rm.Chapa))
        {
            data["matricula"] = rm.Chapa.Trim();
        }

        if (showSensitive)
        {
            if (!string.IsNullOrWhiteSpace(rm.Cpf))
            {
                data["cpf"] = PayslipRmMapper.MaskCpf(rm.Cpf);
            }

            if (!string.IsNullOrWhiteSpace(rm.Rg))
            {
                data["rg"] = rm.Rg.Trim();
            }

            if (!string.IsNullOrWhiteSpace(rm.Banco))
            {
                data["bank"] = rm.Banco.Trim();
            }

            if (!string.IsNullOrWhiteSpace(rm.Agencia))
            {
                data["agency"] = rm.Agencia.Trim();
            }

            if (!string.IsNullOrWhiteSpace(rm.Conta))
            {
                data["account"] = rm.Conta.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(rm.Endereco))
        {
            data["address"] = rm.Endereco.Trim();
        }

        var cityState = FormatCityState(rm.Cidade, rm.Estado);
        if (!string.IsNullOrWhiteSpace(cityState))
        {
            data["cityState"] = cityState;
        }

        if (hireDate.HasValue)
        {
            var tenureYears = CalculateTenureYears(hireDate.Value);
            data["stats"] = new Dictionary<string, object?>
            {
                ["tenureYears"] = tenureYears,
                ["directReports"] = 0,
                ["groups"] = 0,
                ["recognitions"] = 0,
                ["projectsCount"] = 0,
            };

            var history = PersonRmCareerHistoryBuilder.Build(rm, careerHistory, includeSalaryValues);
            if (history.Count > 0)
            {
                data["history"] = history;
            }
        }

        return data;
    }

    private static int CalculateTenureYears(DateOnly hireDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var years = today.Year - hireDate.Year;
        if (today.Month < hireDate.Month ||
            (today.Month == hireDate.Month && today.Day < hireDate.Day))
        {
            years--;
        }

        return Math.Max(0, years);
    }

    private static string? FormatCityState(string? city, string? state)
    {
        var trimmedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        var trimmedState = string.IsNullOrWhiteSpace(state) ? null : state.Trim();

        return (trimmedCity, trimmedState) switch
        {
            (null, null) => null,
            (not null, null) => trimmedCity,
            (null, not null) => trimmedState,
            _ => $"{trimmedCity}, {trimmedState}",
        };
    }

    private static string? FirstNonEmpty(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email[atIndex..]}";
    }
}
