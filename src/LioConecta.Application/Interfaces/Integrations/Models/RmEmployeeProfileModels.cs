namespace LioConecta.Application.Interfaces.Integrations.Models;

public sealed class RmEmployeeProfileRecord
{
    public string Chapa { get; set; } = string.Empty;

    public int? CodPessoa { get; set; }

    public string? Nome { get; set; }

    public string? CodSecao { get; set; }

    public string? SecaoDescricao { get; set; }

    public string? CodFuncao { get; set; }

    public string? FuncaoDescricao { get; set; }

    public DateTime? DataAdmissao { get; set; }

    public string? Cpf { get; set; }

    public string? Rg { get; set; }

    public string? Telefone { get; set; }

    public string? EmailPessoal { get; set; }

    public string? Cidade { get; set; }

    public string? Estado { get; set; }

    public string? Endereco { get; set; }

    public string? GestorNome { get; set; }

    public string? Banco { get; set; }

    public string? Agencia { get; set; }

    public string? Conta { get; set; }
}

public sealed class RmEmployeeAdmissionRecord
{
    public string Chapa { get; set; } = string.Empty;

    public DateTime? DataAdmissao { get; set; }

    public DateTime? DataNascimento { get; set; }

    public string? EmailPessoal { get; set; }
}
