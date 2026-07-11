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
            "Enriquece pessoas com matrÃ­cula e data de admissÃ£o consultando PFUNC no SQL Server TOTVS RM (corporerm).",
            AppSettingKeys.WorkersTotvsSyncIntervalMinutes,
            30,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.TotvsRm, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.GraphSync,
            "Sync Microsoft Graph",
            "Sincroniza documentos SharePoint e eventos de calendÃ¡rio.",
            AppSettingKeys.WorkersGraphSyncIntervalMinutes,
            60,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.Api, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.GraphDirectorySync,
            "Sync diretÃ³rio Microsoft Graph",
            "Importa colaboradores @liotecnica.com.br do Entra ID para o cadastro de pessoas.",
            AppSettingKeys.WorkersGraphDirectorySyncIntervalMinutes,
            60,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.Api, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.PollClosure,
            "Encerramento de enquetes",
            "Notifica participantes quando enquetes sÃ£o encerradas.",
            AppSettingKeys.WorkersPollClosureIntervalMinutes,
            1,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.TotvsTimesheetSync,
            "Sync ponto TOTVS RM",
            "Atualiza cache de espelho de ponto consultando SQL Server TOTVS RM.",
            AppSettingKeys.WorkersTotvsTimesheetSyncIntervalMinutes,
            30,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.TotvsRm, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.TotvsPayslipSync,
            "Sync holerite TOTVS RM",
            "Atualiza cache de contracheques consultando SQL Server TOTVS RM (PFFINANC/PEVENTO/PFPERFF).",
            AppSettingKeys.WorkersTotvsPayslipSyncIntervalMinutes,
            30,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.TotvsRm, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.TotvsLeaveSync,
            "Sync fÃ©rias TOTVS RM",
            "Atualiza saldo e solicitaÃ§Ãµes de fÃ©rias consultando PFUFERIAS/PFUFERIASPER no SQL Server TOTVS RM.",
            AppSettingKeys.WorkersTotvsLeaveSyncIntervalMinutes,
            60,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.TotvsRm, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.LeaveWriteBack,
            "Write-back fÃ©rias RM",
            "Envia solicitaÃ§Ãµes pendentes do portal para API/middleware TOTVS RM quando habilitado.",
            null,
            15,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.TotvsRm, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.PontoWriteBack,
            "Write-back ponto RM",
            "Envia ajustes de ponto aprovados para o TOTVS RM (INSERT ABATFUN) quando habilitado.",
            null,
            15,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.TotvsRm, WorkerConnectivityIds.Postgres]),
        new(
            WorkerKeys.EmailDispatch,
            "Envio de e-mails",
            "Processa a fila de e-mails pendentes via SMTP configurado em banco.",
            null,
            1,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.Postgres, WorkerConnectivityIds.Api]),
        new(
            WorkerKeys.ComunicadoSchedule,
            "PublicaÃ§Ã£o programada de comunicados",
            "Publica comunicados agendados quando sua data e hora sÃ£o atingidas.",
            AppSettingKeys.WorkersComunicadoScheduleIntervalMinutes,
            1,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.Postgres, WorkerConnectivityIds.Api]),
        new(
            WorkerKeys.NewHireAnnouncement,
            "Boas-vindas a novos colaboradores",
            "Publica no feed as admiss?es recentes ainda n?o anunciadas.",
            AppSettingKeys.WorkersNewHireAnnouncementIntervalMinutes,
            60,
            HostedInWorkersProcess: true,
            DependsOn: [WorkerConnectivityIds.Postgres, WorkerConnectivityIds.Api]),
    ];
}
