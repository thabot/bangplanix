using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class Group3ExtensionsTests
{
    [Fact]
    public void SubreportLinkageEngineShouldExtractCrystalAndBirtSubreports()
    {
        var xml = @"
        <CrystalReport>
            <Subreport Name=""InvoiceLinesSub"" ReportName=""InvoiceLines.rpt"">
                <SubreportLink MasterField=""{Customer.ID}"" ChildParameter=""@CustomerID"" />
            </Subreport>
        </CrystalReport>";

        var doc = XDocument.Parse(xml);
        var subreports = SubreportLinkageEngine.ExtractSubreports(doc);

        Assert.Single(subreports);
        var sub = subreports[0];
        Assert.Equal("InvoiceLinesSub", sub.SubreportName);
        Assert.Equal("InvoiceLines.rpt", sub.ReportPath);
        Assert.True(sub.ParameterBindings.ContainsKey("@CustomerID"));
        Assert.Equal("=Fields.ID", sub.ParameterBindings["@CustomerID"]);

        var placeholder = SubreportLinkageEngine.CreateSubreportPlaceholder(sub);
        Assert.Equal(ElementType.Text, placeholder.Type);
        Assert.Contains("InvoiceLinesSub", placeholder.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConditionalFormattingTranspilerShouldTranspileCrystalAndBirtRules()
    {
        // Crystal condition
        var crystalRule = ConditionalFormattingTranspiler.TranspileCrystalCondition("if {@Balance} < 0 then crRed else crBlack");
        Assert.Equal("#DC2626", crystalRule.FontColor);
        Assert.Contains("< 0", crystalRule.ConditionExpression, StringComparison.OrdinalIgnoreCase);

        // BIRT highlight rule
        var birtXml = @"
        <highlight-rule operator=""lt"">
            <test-expr>row[""Amount""]</test-expr>
            <value1>0</value1>
            <color>#EF4444</color>
            <font-weight>bold</font-weight>
        </highlight-rule>";
        var elem = XElement.Parse(birtXml);
        var birtRule = ConditionalFormattingTranspiler.TranspileBirtHighlight(elem);

        Assert.Equal("#EF4444", birtRule.FontColor);
        Assert.Equal("bold", birtRule.FontWeight);
        Assert.Equal("=Fields.Amount < 0", birtRule.ConditionExpression);

        var reportElem = new ElementDefinition { Id = "Txt1" };
        ConditionalFormattingTranspiler.ApplyRuleToElement(reportElem, birtRule);

        Assert.NotNull(reportElem.Style);
        Assert.Equal("#EF4444", reportElem.Style.Color);
        Assert.Equal("bold", reportElem.Style.FontWeight);
    }

    [Fact]
    public void MigrationFidelityAuditorShouldScoreReportAndDetectSubreports()
    {
        var sampleContent = @"
        <CrystalReport Version=""14.0"">
            <Section Type=""ReportHeader"">
                <TextObject Name=""Title"" Text=""Sales Report"" />
            </Section>
            <Section Type=""Detail"">
                <FieldObject Name=""Field1"" Field=""{Sales.Amount}"" />
                <Subreport Name=""ItemDetails"" ReportName=""ItemDetails.rpt"">
                    <SubreportLink MasterField=""{Sales.ID}"" ChildParameter=""@ID"" />
                </Subreport>
            </Section>
        </CrystalReport>";

        var audit = MigrationFidelityAuditor.AuditFile("SalesReport.rpt.xml", sampleContent);

        Assert.Equal("SAP Crystal Reports", audit.ReportFormat);
        Assert.True(audit.FidelityScore >= 95.0, $"Expected FidelityScore >= 95.0, but was {audit.FidelityScore}");
        Assert.Equal(1, audit.SubreportsCount);
        Assert.True(audit.TotalElements > 0);
        Assert.Equal(0, audit.UnsupportedElements);
    }

    [Fact]
    public void MigrationFidelityAuditorShouldAuditDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"Bangplanix_Audit_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "Report1.rpt.xml"), "<CrystalReport><Section Type=\"Detail\"><TextObject Text=\"Hello\"/></Section></CrystalReport>");
            File.WriteAllText(Path.Combine(tempDir, "Report2.rptdesign"), "<report xmlns=\"http://www.eclipse.org/birt/2005/design\"><page-setup><simple-master-page/></page-setup></report>");

            var summary = MigrationFidelityAuditor.AuditDirectory(tempDir);

            Assert.Equal(2, summary.TotalFilesAudited);
            Assert.True(summary.AverageFidelityScore > 90.0);
            Assert.Contains("SAP Crystal Reports", summary.DetectedEngines);
            Assert.Contains("Eclipse BIRT", summary.DetectedEngines);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
