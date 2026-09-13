using System;
using System.IO;
using System.Linq;
using Bangplanix.Adapters.Access;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class MsAccessAdapterTests
{
    private readonly MsAccessReportAdapter _adapter = new();

    private const string SampleAccessSaveAsText = """
        Version =20
        VersionRequired =20
        Begin Report
            Caption ="Sales Invoice Report"
            RecordSource ="SELECT OrderID, CustomerName, TotalAmount, OrderDate FROM qryInvoices"
            Begin Section
                Section =1
                Height =1440
                Begin Label
                    Name ="lblTitle"
                    Left =1440
                    Top =288
                    Width =5760
                    Height =576
                    Caption ="OFFICIAL TAX INVOICE"
                    FontName ="Segoe UI"
                    FontSize =16
                    FontWeight =700
                End
            End
            Begin Section
                Section =0
                Height =720
                Begin TextBox
                    Name ="txtCustomer"
                    ControlSource ="=[FirstName] & \" \" & [LastName]"
                    Left =720
                    Top =144
                    Width =2880
                    Height =360
                End
                Begin TextBox
                    Name ="txtTotal"
                    ControlSource ="[TotalAmount]"
                    Left =4320
                    Top =144
                    Width =2160
                    Height =360
                End
            End
            Begin Section
                Section =2
                Height =1000
                Begin TextBox
                    Name ="txtGrandTotal"
                    ControlSource ="=Sum([TotalAmount])"
                    Left =4320
                    Top =200
                    Width =2160
                    Height =360
                End
            End
        End
        """;

    [Fact]
    public void Convert_FromSaveAsText_ShouldExtractSectionsControlsAndSql()
    {
        var report = _adapter.Convert(SampleAccessSaveAsText);

        report.Should().NotBeNull();
        report.Metadata.Title.Should().Be("Sales Invoice Report");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].QueryOrUrl.Should().Contain("qryInvoices");

        // Verify PageHeader
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements.Should().HaveCount(1);
        report.Bands.PageHeader.Elements[0].Text.Should().Be("OFFICIAL TAX INVOICE");
        report.Bands.PageHeader.Elements[0].Style?.FontWeight.Should().Be("Bold");

        // Verify Detail
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);

        var custElem = report.Bands.Detail.Elements[0];
        custElem.Expression.Should().Contain("string.Concat");
        custElem.Expression.Should().Contain("Fields.FirstName");
        custElem.Expression.Should().Contain("Fields.LastName");

        var totalElem = report.Bands.Detail.Elements[1];
        totalElem.Expression.Should().Contain("Fields.TotalAmount");

        // Verify PageFooter
        report.Bands.PageFooter.Should().NotBeNull();
        report.Bands.PageFooter!.Elements.Should().HaveCount(1);
        report.Bands.PageFooter.Elements[0].Expression.Should().Contain("Sum(Fields.TotalAmount)");
    }

    [Fact]
    public void LegacyAdapterFactory_ShouldDetectAccessReport()
    {
        var adapter = LegacyAdapterFactory.GetAdapterByExtension("accounting.accdb");
        adapter.Should().BeOfType<MsAccessReportAdapter>();

        var mdbAdapter = LegacyAdapterFactory.GetAdapterByExtension("legacy.mdb");
        mdbAdapter.Should().BeOfType<MsAccessReportAdapter>();

        var detected = LegacyAdapterFactory.DetectAdapter(SampleAccessSaveAsText);
        detected.Should().BeOfType<MsAccessReportAdapter>();
    }

    [Theory]
    [InlineData("=[FirstName] & \" \" & [LastName]", "string.Concat(Fields.FirstName, \" \", Fields.LastName)")]
    [InlineData("=Sum([TotalAmount])", "Sum(Fields.TotalAmount)")]
    [InlineData("=Nz([Discount], 0)", "(Fields.Discount ?? 0)")]
    [InlineData("=IIf([Qty]>0, [UnitPrice], 0)", "(Fields.Qty > 0 ? Fields.UnitPrice : 0)")]
    public void LegacyExpressionTranspiler_ShouldTranspileAccessVba(string vbaExpr, string expectedSubstring)
    {
        var result = LegacyExpressionTranspiler.Transpile(vbaExpr, "ACCESS");
        result.Should().Contain(expectedSubstring);
    }
}
