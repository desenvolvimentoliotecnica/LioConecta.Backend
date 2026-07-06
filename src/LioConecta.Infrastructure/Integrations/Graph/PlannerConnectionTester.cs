using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Integrations;
using Microsoft.Extensions.Logging;

namespace LioConecta.Infrastructure.Integrations.Graph;

public sealed class PlannerConnectionTester(ILogger<PlannerConnectionTester> logger)
{
    public async Task<PlannerConnectionTestResponse> TestAsync(
        string planId,
        IPlannerAdapter plannerAdapter,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await plannerAdapter.GetPlanAsync(planId, cancellationToken);
            if (plan is null)
            {
                return Fail(
                    "Plano Planner não encontrado.",
                    $"Verifique o plan_id ({planId}) e a permissão Tasks.ReadWrite.All (application).");
            }

            var buckets = await plannerAdapter.GetBucketsAsync(planId, cancellationToken);
            var tasks = await plannerAdapter.GetPlanTasksAsync(planId, cancellationToken);

            var detail =
                $"Plano «{plan.Title}» acessível. Colunas: {buckets.Count}. Tarefas: {tasks.Count}. " +
                "Permissão necessária: Tasks.ReadWrite.All (application) + credenciais Graph configuradas.";

            return new PlannerConnectionTestResponse(
                true,
                "Conexão com Microsoft Planner realizada com sucesso.",
                detail,
                UsesDevAdapters: false,
                PlannerEnabled: true,
                PlanId: planId,
                PlanTitle: plan.Title,
                BucketCount: buckets.Count,
                TaskCount: tasks.Count);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao testar conexão Microsoft Planner.");
            return Fail(
                "Não foi possível acessar o plano Microsoft Planner.",
                exception.Message);
        }
    }

    private static PlannerConnectionTestResponse Fail(string message, string? detail) =>
        new(
            false,
            message,
            detail,
            UsesDevAdapters: false,
            PlannerEnabled: true,
            PlanId: null,
            PlanTitle: null,
            BucketCount: null,
            TaskCount: null);
}
