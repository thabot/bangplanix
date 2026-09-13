using Bangplanix.Adapters.Common;
using Bangplanix.Adapters.DevExpress;
using Bangplanix.Adapters.FastReport;
using Bangplanix.Adapters.Jaspersoft;
using Bangplanix.Adapters.Office;
using Bangplanix.Adapters.Ssrs;
using Bangplanix.Adapters.Stimulsoft;
using Bangplanix.Adapters.Telerik;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class CommonAdapterTests
{
    [Fact]
    public void LegacyUnitNormalizer_ShouldConvertVariousUnitsCorrectly()
    {
        LegacyUnitNormalizer.ConvertToPoints("1in", 0).Should().BeApproximately(72.0, 0.01);
        LegacyUnitNormalizer.ConvertToPoints("10mm", 0).Should().BeApproximately(28.35, 0.01);
        LegacyUnitNormalizer.ConvertToPoints("2cm", 0).Should().BeApproximately(56.69, 0.01);
        LegacyUnitNormalizer.ConvertToPoints("100pt", 0).Should().Be(100.0);
        LegacyUnitNormalizer.ConvertToPoints("96px", 0).Should().Be(72.0);
        LegacyUnitNormalizer.ConvertToPoints("invalid", 42.0).Should().Be(42.0);
    }

    [Fact]
    public void LegacyExpressionTranspiler_ShouldTranspileSsrsExpressions()
    {
        var expr1 = "=IIf(Fields!Total.Value > 100, \"High\", \"Low\")";
        var res1 = LegacyExpressionTranspiler.Transpile(expr1, "SSRS");
        res1.Should().Be("=(Fields.Total > 100 ? \"High\" : \"Low\")");

        var expr2 = "=Parameters!Dept.Value";
        var res2 = LegacyExpressionTranspiler.Transpile(expr2, "SSRS");
        res2.Should().Be("=Parameters.Dept");
    }

    [Fact]
    public void LegacyExpressionTranspiler_ShouldTranspileJaspersoftExpressions()
    {
        var expr = "$F{EmployeeName} + \" - \" + $P{Department}";
        var res = LegacyExpressionTranspiler.Transpile(expr, "JASPER");
        res.Should().Be("=Fields.EmployeeName + \" - \" + Parameters.Department");
    }

    [Fact]
    public void LegacyExpressionTranspiler_ShouldTranspileFastReportExpressions()
    {
        var expr = "Invoice #[Orders.Id] for [Customers.Name]";
        var res = LegacyExpressionTranspiler.Transpile(expr, "FASTREPORT");
        res.Should().Be("=Invoice #Fields.Orders.Id for Fields.Customers.Name");
    }

    [Fact]
    public void LegacyExpressionTranspiler_ShouldTranspileStimulsoftExpressions()
    {
        var expr = "Dear {Customers.ContactName}, balance {Orders.Amount}";
        var res = LegacyExpressionTranspiler.Transpile(expr, "STIMULSOFT");
        res.Should().Be("=Dear Fields.Customers.ContactName, balance Fields.Orders.Amount");
    }

    [Fact]
    public void LegacyExpressionTranspiler_ShouldTranspileLiquidExpressions()
    {
        var expr = "Hello {{ user.name }}, total {{ order.amount }}";
        var res = LegacyExpressionTranspiler.Transpile(expr, "LIQUID");
        res.Should().Be("=Hello Fields.user.name, total Fields.order.amount");
    }

    [Fact]
    public void LegacyScriptSanitizer_ShouldRemoveUnsafeCodeBlocks()
    {
        var rawXml = @"<Report><Code>System.Reflection.Assembly.Load(...);</Code><Script>eval('rm -rf /');</Script><Name>Test</Name></Report>";
        var sanitized = LegacyScriptSanitizer.Sanitize(rawXml);
        sanitized.Should().NotContain("System.Reflection");
        sanitized.Should().NotContain("eval(");
        sanitized.Should().Contain("<Name>Test</Name>");
    }

    [Fact]
    public void LegacyAdapterFactory_ShouldResolveAdaptersByExtension()
    {
        LegacyAdapterFactory.GetAdapterByExtension("report.rdl").Should().BeOfType<SsrsRdlAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension(".jrxml").Should().BeOfType<JaspersoftJrxmlAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("file.frx").Should().BeOfType<FastReportFrxAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("file.mrt").Should().BeOfType<StimulsoftMrtAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("file.trdx").Should().BeOfType<TelerikTrdxAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("file.trdp").Should().BeOfType<TelerikTrdxAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("file.repx").Should().BeOfType<DevExpressRepxAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("file.html").Should().BeOfType<OfficeAndHtmlAdapter>();
    }

    [Fact]
    public void LegacyAdapterFactory_ShouldDetectAdapterFromContent()
    {
        var jrxml = "<jasperReport name=\"TestReport\"></jasperReport>";
        LegacyAdapterFactory.DetectAdapter(jrxml).Should().BeOfType<JaspersoftJrxmlAdapter>();

        var rdl = "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition\"></Report>";
        LegacyAdapterFactory.DetectAdapter(rdl).Should().BeOfType<SsrsRdlAdapter>();

        var mrt = "{\"ReportVersion\": \"2024.1\", \"Pages\": {}}";
        LegacyAdapterFactory.DetectAdapter(mrt).Should().BeOfType<StimulsoftMrtAdapter>();
    }
}
