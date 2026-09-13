using Bangplanix.Core.Models;
using Bangplanix.Core.Parameters;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Core.Tests;

public class ParameterValidatorTests
{
    [Fact]
    public void ResolveAndValidate_RequiredParameterMissing_ShouldReturnValidationError()
    {
        var report = new ReportDefinition
        {
            Parameters =
            [
                new ParameterDefinition { Name = "CustomerCode", Label = "Customer Code", IsRequired = true, Type = ParameterType.String }
            ]
        };

        var userInputs = new Dictionary<string, object?>();
        var resolved = ParameterValidator.ResolveAndValidate(report, userInputs, out var errors);

        errors.Should().ContainSingle().Which.Should().Contain("Customer Code").And.Contain("required");
    }

    [Fact]
    public void ResolveAndValidate_DefaultValueFallback_ShouldPopulateWhenNoUserInput()
    {
        var report = new ReportDefinition
        {
            Parameters =
            [
                new ParameterDefinition { Name = "FiscalYear", DefaultValue = 2026, Type = ParameterType.Number },
                new ParameterDefinition { Name = "IsActive", DefaultValue = true, Type = ParameterType.Boolean }
            ]
        };

        var resolved = ParameterValidator.ResolveAndValidate(report, null, out var errors);

        errors.Should().BeEmpty();
        resolved["FiscalYear"].Should().Be(2026m);
        resolved["IsActive"].Should().Be(true);
    }

    [Fact]
    public void ResolveAndValidate_NumericMinMaxAndRegexValidation_ShouldEnforceConstraints()
    {
        var report = new ReportDefinition
        {
            Parameters =
            [
                new ParameterDefinition { Name = "Discount", Label = "Discount %", Type = ParameterType.Number, MinValue = 0, MaxValue = 100 },
                new ParameterDefinition { Name = "TaxId", Label = "Tax ID", Type = ParameterType.String, ValidationPattern = @"^\d{13}$" }
            ]
        };

        // Out of bounds input
        var invalidInputs = new Dictionary<string, object?>
        {
            ["Discount"] = 150,
            ["TaxId"] = "INVALID_TAX_ID"
        };

        var resolved = ParameterValidator.ResolveAndValidate(report, invalidInputs, out var errors);
        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.Contains("<= 100"));
        errors.Should().Contain(e => e.Contains("format is invalid"));

        // Valid inputs
        var validInputs = new Dictionary<string, object?>
        {
            ["Discount"] = 15,
            ["TaxId"] = "1234567890123"
        };

        var validResolved = ParameterValidator.ResolveAndValidate(report, validInputs, out var validErrors);
        validErrors.Should().BeEmpty();
        validResolved["Discount"].Should().Be(15m);
        validResolved["TaxId"].Should().Be("1234567890123");
    }
}