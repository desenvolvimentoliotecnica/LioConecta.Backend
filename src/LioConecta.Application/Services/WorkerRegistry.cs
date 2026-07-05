using LioConecta.Application.Common;
using LioConecta.Application.DTOs;

namespace LioConecta.Application.Services;

public static class WorkerRegistry
{
    public static IReadOnlyList<WorkerDefinitionDto> All { get; } =
    [
        new(
            WorkerKeys.TotvsEmployeeSync,
            "Sync colaboradores TOTVS",
            "Sincroniza colaboradores da API REST TOTVS com o cadastro de pessoas.",
            AppSettingKeys.WorkersTotvsSyncIntervalMinutes,
            30),
        new(
            WorkerKeys.GraphSync,
            "Sync Microsoft Graph",
            "Sincroniza documentos SharePoint e eventos de calendário.",
            AppSettingKeys.WorkersGraphSyncIntervalMinutes,
            60),
        new(
            WorkerKeys.PollClosure,
            "Encerramento de enquetes",
            "Notifica participantes quando enquetes são encerradas.",
            AppSettingKeys.WorkersPollClosureIntervalMinutes,
            1),
        new(
            WorkerKeys.TotvsTimesheetSync,
            "Sync ponto TOTVS RM",
            "Atualiza cache de espelho de ponto consultando SQL Server TOTVS RM.",
            AppSettingKeys.WorkersTotvsTimesheetSyncIntervalMinutes,
            30),
    ];
}
