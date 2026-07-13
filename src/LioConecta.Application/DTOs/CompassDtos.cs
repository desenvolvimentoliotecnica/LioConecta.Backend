namespace LioConecta.Application.DTOs;

public sealed record CompassBootstrapDto(
    bool Enabled,
    bool CanAccess,
    IReadOnlyList<string> AllowedRoles,
    IReadOnlyList<string> AllowedEmails);

public sealed record CompassSnapshotDto(
    Guid Id,
    string Label,
    string VersionAtual,
    string VersionAnterior,
    string SourceSystem,
    DateTimeOffset ImportedAt,
    int RowCount);

public sealed record CompassMetaDto(
    CompassSnapshotDto Snapshot,
    IReadOnlyList<string> Directorias,
    IReadOnlyList<string> Unidades,
    IReadOnlyList<string> Familias,
    IReadOnlyList<string> Tipos);

public sealed record CompassKpiDto(
    string Id,
    string Label,
    string Value,
    string Delta,
    string Trend,
    string Icon,
    string Mod);

public sealed record CompassBridgeItemDto(
    string Diretoria,
    decimal IbpAtual,
    decimal IbpAnterior,
    decimal Variacao);

public sealed record CompassVarianceItemDto(
    string Tipo,
    string FamiliaComercial,
    string SkuCode,
    string SkuDescription,
    string Cliente,
    string Matriz,
    string Diretoria,
    string Unidade,
    decimal IbpAtual,
    decimal IbpAnterior,
    decimal Variacao);

public sealed record CompassAlertDto(
    string Id,
    string Severity,
    string Title,
    string Description,
    int Quantity,
    string Link);

public sealed record CompassDashboardDto(
    CompassSnapshotDto Snapshot,
    IReadOnlyList<CompassKpiDto> Kpis,
    IReadOnlyList<CompassBridgeItemDto> BridgeByDiretoria,
    IReadOnlyList<CompassVarianceItemDto> TopVariances,
    IReadOnlyList<CompassAlertDto> Alerts);

public sealed record CompassIbpRowDto(
    Guid Id,
    string Tipo,
    string FamiliaComercial,
    string SkuCode,
    string SkuDescription,
    string ClienteHyperion,
    string Cliente,
    string Matriz,
    string Diretoria,
    string Unidade,
    decimal IbpAtual,
    decimal IbpAnterior,
    decimal Variacao);

public sealed record CompassYtdPageDto(
    IReadOnlyList<CompassIbpRowDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CompassAggregateRowDto(
    string GroupKey,
    decimal IbpAtual,
    decimal IbpAnterior,
    decimal Variacao,
    int RowCount);

public sealed record CompassAggregatesDto(
    string GroupBy,
    IReadOnlyList<CompassAggregateRowDto> Items);

public sealed record CompassYtdQuery(
    string? Diretoria = null,
    string? Unidade = null,
    string? Familia = null,
    string? Tipo = null,
    string? Search = null,
    decimal? MinVariacao = null,
    bool OnlyNonZero = false,
    int Page = 1,
    int PageSize = 50);

public sealed record CompassAggregatesQuery(
    string GroupBy = "diretoria",
    string? Diretoria = null,
    string? Unidade = null,
    string? Familia = null,
    string? Tipo = null,
    string? Search = null);

public sealed record CompassScenariosQuery(
    string Version = "Oficial",
    string Scenario = "Orcado",
    string Years = "FY26",
    string Period = "Jan");

public sealed record CompassScenarioFiltersDto(
    string Version,
    string Scenario,
    string Years,
    string Period);

public sealed record CompassScenarioItemDto(
    string Id,
    string Account,
    string Name,
    string Description,
    long RowCount,
    decimal TotalAmount,
    string Status);

public sealed record CompassScenariosDto(
    bool Configured,
    string? Message,
    CompassScenarioFiltersDto Filters,
    IReadOnlyList<CompassScenarioItemDto> Scenarios);

public sealed record CompassScenarioRowsQuery(
    string Version = "Oficial",
    string Scenario = "Orcado",
    string Years = "FY26",
    string Period = "Jan",
    string? Search = null,
    string? Ung = null,
    int Page = 1,
    int PageSize = 25);

public sealed record CompassScenarioRowDto(
    string Sku,
    string SkuDescription,
    string Cliente,
    string ClienteNome,
    string Ung,
    string UngNome,
    string Entity,
    decimal Amount);

public sealed record CompassScenarioRowsPageDto(
    string ScenarioId,
    string Account,
    string Name,
    bool Configured,
    string? Message,
    CompassScenarioFiltersDto Filters,
    IReadOnlyList<CompassScenarioRowDto> Items,
    int Page,
    int PageSize,
    long TotalCount,
    decimal TotalAmount,
    int TotalPages);

