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
            "Enriquece pessoas com matrícula e data de admissão consultando PFUNC no SQL Server TOTVS RM (corporerm).",
            AppSettingKeys.WorkersTotvsSyncIntervalMinutes,
            30),
        new(
            WorkerKeys.GraphSync,
            "Sync Microsoft Graph",
            "Sincroniza documentos SharePoint e eventos de calendário.",
            AppSettingKeys.WorkersGraphSyncIntervalMinutes,
            60),
        new(
            WorkerKeys.GraphDirectorySync,
            "Sync diretório Microsoft Graph",
            "Importa colaboradores @liotecnica.com.br do Entra ID para o cadastro de pessoas.",
            AppSettingKeys.WorkersGraphDirectorySyncIntervalMinutes,
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
        new(
            WorkerKeys.TotvsPayslipSync,
            "Sync holerite TOTVS RM",
            "Atualiza cache de contracheques consultando SQL Server TOTVS RM (PFFINANC/PEVENTO/PFPERFF).",
            AppSettingKeys.WorkersTotvsPayslipSyncIntervalMinutes,
            30),
        new(
            WorkerKeys.TotvsLeaveSync,
            "Sync férias TOTVS RM",
            "Atualiza saldo e solicitações de férias consultando PFUFERIAS/PFUFERIASPER no SQL Server TOTVS RM.",
            AppSettingKeys.WorkersTotvsLeaveSyncIntervalMinutes,
            60),
        new(
            WorkerKeys.LeaveWriteBack,
            "Write-back férias RM",
            "Envia solicitações pendentes do portal para API/middleware TOTVS RM quando habilitado.",
            null,
            15),
        new(
            WorkerKeys.EmailDispatch,
            "Envio de e-mails",
            "Processa a fila de e-mails pendentes via SMTP configurado em banco.",
            null,
            1),
    ];
}
