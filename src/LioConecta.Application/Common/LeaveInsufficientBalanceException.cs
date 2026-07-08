namespace LioConecta.Application.Common;

public sealed class LeaveInsufficientBalanceException(int requestedDays, int availableDays)
    : Exception($"Saldo insuficiente: solicitado {requestedDays} dia(s), disponível {availableDays}.")
{
    public int RequestedDays { get; } = requestedDays;

    public int AvailableDays { get; } = availableDays;
}
