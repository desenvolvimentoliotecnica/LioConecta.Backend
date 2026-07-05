namespace LioConecta.Application.DTOs;

public sealed record PayslipPdfLineDto(
    string Code,
    string Description,
    string Reference,
    decimal Amount);

public sealed record PayslipPdfDocumentDto(
    string CompanyName,
    string CompanyCnpj,
    string PeriodLabel,
    string ReferenceMonth,
    string EmployeeName,
    string EmployeeRegistration,
    string EmployeeCpf,
    string EmployeeRole,
    string EmployeeDepartment,
    string EmployeeAdmissionDate,
    IReadOnlyList<PayslipPdfLineDto> Earnings,
    IReadOnlyList<PayslipPdfLineDto> Deductions,
    decimal BaseSalary,
    decimal BaseInss,
    decimal BaseFgts,
    decimal FgtsAmount,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal NetAmount,
    string BankCode,
    string BankAgency,
    string BankAccount,
    string PaymentDate);
