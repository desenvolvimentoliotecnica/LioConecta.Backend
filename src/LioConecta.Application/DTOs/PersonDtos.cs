using LioConecta.Domain.Enums;

namespace LioConecta.Application.DTOs;

public sealed record MeDto(
    Guid Id,
    string Slug,
    string Name,
    string Email,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    IReadOnlyList<UserRole> Roles);

public sealed record PersonSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    string? Location,
    bool IsActive);

public sealed record PersonProfileDto(
    Guid Id,
    string Slug,
    string Name,
    string? Title,
    string Email,
    string? Phone,
    string? Location,
    string? PhotoUrl,
    string? DepartmentName,
    string? ManagerName,
    DateOnly? BirthDate,
    DateOnly? HireDate,
    string? Status,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Skills,
    IReadOnlyDictionary<string, string>? PersonalData,
    ViewerContext ViewerContext);

public sealed record OrgChartNodeDto(
    Guid Id,
    string? OrgChartId,
    string Slug,
    string Name,
    string? Title,
    string? PhotoUrl,
    string? DepartmentName,
    Guid? ManagerId,
    IReadOnlyList<string> Tags);

public sealed record OrgChartDto(
    IReadOnlyList<OrgChartNodeDto> Nodes,
    Guid? RootId);
