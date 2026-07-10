namespace EnterpriseFlow.Application.Features.Companies.GetCompanyById;

public sealed record CompanyDto(Guid Id, string Name, string? TaxId);
