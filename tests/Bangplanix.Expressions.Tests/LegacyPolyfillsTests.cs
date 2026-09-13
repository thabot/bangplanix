using Bangplanix.Expressions.Compiler;
using Bangplanix.Expressions.Polyfills;
using Xunit;

namespace Bangplanix.Expressions.Tests;

public class LegacyPolyfillsTests
{
    private readonly RoslynExpressionCompiler _compiler = new();

    [Fact]
    public void CrystalPolyfillsShouldEvaluateCorrectly()
    {
        Assert.True(LegacyExpressionPolyfills.IsNull(null));
        Assert.True(LegacyExpressionPolyfills.IsNull(string.Empty));
        Assert.False(LegacyExpressionPolyfills.IsNull("Data"));

        Assert.Equal("1234.50", LegacyExpressionPolyfills.ToText(1234.5m, "0.00"));
        Assert.Equal(1234.5m, LegacyExpressionPolyfills.ToNumber("1234.5"));

        Assert.Equal("Ban", LegacyExpressionPolyfills.Left("Bangplanix", 3));
        Assert.Equal("nix", LegacyExpressionPolyfills.Right("Bangplanix", 3));
        Assert.Equal("gpl", LegacyExpressionPolyfills.Mid("Bangplanix", 4, 3));

        Assert.Equal("Active", LegacyExpressionPolyfills.IIF(true, "Active", "Inactive"));
        Assert.Equal("Inactive", LegacyExpressionPolyfills.IIF(false, "Active", "Inactive"));

        var dt = new DateTime(2026, 1, 1);
        Assert.Equal(new DateTime(2027, 1, 1), LegacyExpressionPolyfills.DateAdd("yyyy", 1, dt));
        Assert.Equal(new DateTime(2026, 2, 1), LegacyExpressionPolyfills.DateAdd("m", 1, dt));
        Assert.Equal(new DateTime(2026, 1, 6), LegacyExpressionPolyfills.DateAdd("d", 5, dt));
    }

    [Fact]
    public void OraclePolyfillsShouldEvaluateCorrectly()
    {
        Assert.Equal("DefaultValue", LegacyExpressionPolyfills.NVL(null, "DefaultValue"));
        Assert.Equal("ExistingValue", LegacyExpressionPolyfills.NVL("ExistingValue", "DefaultValue"));

        Assert.Equal("HasValue", LegacyExpressionPolyfills.NVL2("ExistingValue", "HasValue", "NoValue"));
        Assert.Equal("NoValue", LegacyExpressionPolyfills.NVL2(null, "HasValue", "NoValue"));

        // DECODE(val, 1, 'One', 2, 'Two', 'Other')
        Assert.Equal("One", LegacyExpressionPolyfills.DECODE(1, 1, "One", 2, "Two", "Other"));
        Assert.Equal("Two", LegacyExpressionPolyfills.DECODE(2, 1, "One", 2, "Two", "Other"));
        Assert.Equal("Other", LegacyExpressionPolyfills.DECODE(3, 1, "One", 2, "Two", "Other"));
    }

    [Fact]
    public void BirtPolyfillsShouldEvaluateCorrectly()
    {
        var dt = new DateTime(2026, 9, 12);
        Assert.Equal(2026, BirtDateTime.Year(dt));
        Assert.Equal(9, BirtDateTime.Month(dt));
        Assert.Equal(12, BirtDateTime.Day(dt));

        var dt2 = new DateTime(2026, 9, 22);
        Assert.Equal(10, BirtDateTime.DiffDay(dt, dt2));

        Assert.Equal(123.46m, BirtMath.Round(123.456m, 2));
        Assert.Equal(124m, BirtMath.Ceil(123.1m));
        Assert.Equal(123m, BirtMath.Floor(123.9m));
        Assert.Equal(50m, BirtMath.Abs(-50m));

        Assert.Equal("Bangplanix Engine", BirtStr.Concat("Bangplanix", " ", "Engine"));
        Assert.Equal("HELLO", BirtStr.ToUpper("hello"));
        Assert.Equal("plan", BirtStr.Substring("Bangplanix", 4, 4));
    }

    [Fact]
    public void RoslynCompilerShouldNativelyExecutePolyfillExpressions()
    {
        var result1 = _compiler.Evaluate<string>("=Left(\"Bangplanix\", 4)");
        Assert.Equal("Bang", result1);

        var result2 = _compiler.Evaluate<object>("=NVL(null, \"ReplacedValue\")");
        Assert.Equal("ReplacedValue", result2?.ToString());

        var result3 = _compiler.Evaluate<decimal>("=BirtMath.Round(99.987m, 2)");
        Assert.Equal(99.99m, result3);
    }
}
