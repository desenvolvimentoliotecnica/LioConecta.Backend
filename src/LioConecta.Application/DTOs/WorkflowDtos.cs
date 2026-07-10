namespace LioConecta.Application.DTOs;

public sealed record CreateMovimentacaoMeritoRequestDto(
    Guid SubjectPersonId,
    string? Cargo,
    decimal? NovoSalario,
    string? Justificativa);

public sealed record WorkflowStepDto(
    Guid Id,
    string StepKey,
    int Order,
    string? AssigneeRole,
    Guid? AssigneePersonId,
    string? AssigneeName,
    string Status,
    Guid? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? Comment);

public sealed record WorkflowInstanceDto(
    Guid Id,
    string DefinitionKey,
    string DefinitionName,
    string SubjectType,
    Guid SubjectId,
    string? SubjectName,
    string Status,
    string PayloadJson,
    Guid CreatedByPersonId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<WorkflowStepDto> Steps);

public sealed record WorkflowDecisionRequestDto(string? Comment);
