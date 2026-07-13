using System.Text.Json.Serialization;

namespace LioConecta.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceRequestStatus
{
    Draft,
    Submitted,
    InReview,
    Approved,
    Rejected,
    Completed,
    Cancelled,
    /// <summary>RH encerrou o atendimento; aguarda confirmação do solicitante.</summary>
    AwaitingConfirmation,
}
