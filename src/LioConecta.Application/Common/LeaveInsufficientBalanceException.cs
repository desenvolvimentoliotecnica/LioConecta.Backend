namespace LioConecta.Application.Common;

public sealed class LeaveInsufficientBalanceException : Exception
{
    public LeaveInsufficientBalanceException(int requestedDays, int availableDays)
        : this(
            requestedDays,
            availableDays,
            $"Saldo insuficiente: solicitado {requestedDays} dia(s), disponível {availableDays}.")
    {
    }

    public LeaveInsufficientBalanceException(int requestedDays, int availableDays, string message)
        : base(message)
    {
        RequestedDays = requestedDays;
        AvailableDays = availableDays;
    }

    public int RequestedDays { get; }

    public int AvailableDays { get; }
}
