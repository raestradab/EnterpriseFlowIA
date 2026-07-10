using EnterpriseFlow.Application.Features.Companies.CreateCompany;
using FluentValidation.TestHelper;

namespace EnterpriseFlow.Application.UnitTests.Features.Companies;

public class CreateCompanyValidatorTests
{
    private readonly CreateCompanyValidator _validator = new();

    [Fact]
    public void Empty_Name_Fails_Validation()
    {
        var result = _validator.TestValidate(new CreateCompanyCommand(string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Name_Longer_Than_200_Characters_Fails_Validation()
    {
        var result = _validator.TestValidate(new CreateCompanyCommand(new string('a', 201), null));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Valid_Command_Passes_Validation()
    {
        var result = _validator.TestValidate(new CreateCompanyCommand("Acme Corp", "123-456"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
