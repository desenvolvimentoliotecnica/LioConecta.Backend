namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class RmEmployeeProfileRecord
{
    public string Chapa { get; set; } = string.Empty;

    public int? CodPessoa { get; set; }

    public string? Nome { get; set; }

    public string? SecaoDescricao { get; set; }

    public string? FuncaoDescricao { get; set; }

    public DateTime? DataAdmissao { get; set; }

    public string? Cpf { get; set; }

    public string? Banco { get; set; }

    public string? Agencia { get; set; }

    public string? Conta { get; set; }
}
