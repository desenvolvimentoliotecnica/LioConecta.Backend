using LioConecta.Application.DTOs;

namespace LioConecta.Application.Common;

public static class HelpDeskTicketCreateValidator
{
    public const int MaxSubjectLength = 120;
    public const int MinDescriptionLength = 10;

    public static void Validate(CreateHelpDeskTicketRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subject = request.Subject?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Assunto é obrigatório.");
        }

        if (subject.Length > MaxSubjectLength)
        {
            throw new ArgumentException($"Assunto deve ter no máximo {MaxSubjectLength} caracteres.");
        }

        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length < MinDescriptionLength)
        {
            throw new ArgumentException($"Descrição deve ter pelo menos {MinDescriptionLength} caracteres.");
        }

        if (request.EntityId <= 0)
        {
            throw new ArgumentException("Entidade inválida.");
        }

        if (request.CategoryId <= 0)
        {
            throw new ArgumentException("Categoria inválida.");
        }

        var priority = request.Priority?.Trim().ToLowerInvariant() ?? string.Empty;
        if (priority is not ("baixa" or "media" or "média" or "alta" or "critica" or "crítica" or "urgente"))
        {
            throw new ArgumentException("Prioridade inválida.");
        }
    }
}
