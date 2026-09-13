using Bangplanix.Core.Models;
using Bangplanix.Engine.Parameters;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class CascadingParameterResolverTests
{
    [Fact]
    public void GetExecutionOrder_ShouldOrderParentParametersBeforeChildren()
    {
        var report = new ReportDefinition
        {
            Parameters =
            [
                new ParameterDefinition { Name = "City", CascadingParent = "Province" },
                new ParameterDefinition { Name = "Country" },
                new ParameterDefinition { Name = "Province", CascadingParent = "Country" }
            ]
        };

        var order = CascadingParameterResolver.GetExecutionOrder(report);

        order.Select(p => p.Name).Should().ContainInOrder("Country", "Province", "City");
    }

    [Fact]
    public void FilterOptionsByParent_StaticAvailableValues_ShouldFilterCorrectly()
    {
        var provinceParam = new ParameterDefinition
        {
            Name = "Province",
            CascadingParent = "Country",
            AvailableValues =
            [
                new ParameterOption { Label = "Bangkok", Value = "TH_BKK" },
                new ParameterOption { Label = "Chiang Mai", Value = "TH_CNX" },
                new ParameterOption { Label = "Tokyo", Value = "JP_TYO" }
            ]
        };

        var thOptions = CascadingParameterResolver.FilterOptionsByParent(provinceParam, "TH");
        thOptions.Should().HaveCount(2);
        thOptions.Should().OnlyContain(o => o.Value!.ToString()!.StartsWith("TH"));

        var jpOptions = CascadingParameterResolver.FilterOptionsByParent(provinceParam, "JP");
        jpOptions.Should().HaveCount(1);
        jpOptions.Should().ContainSingle(o => o.Label == "Tokyo");
    }

    [Fact]
    public void FilterOptionsByParent_DynamicDatasetRows_ShouldFilterByParentField()
    {
        var cityParam = new ParameterDefinition
        {
            Name = "City",
            CascadingParent = "ProvinceCode",
            ValueField = "CityCode",
            LabelField = "CityName"
        };

        var datasetRows = new List<Dictionary<string, object?>>
        {
            new() { ["ProvinceCode"] = "BKK", ["CityCode"] = "BKK_01", ["CityName"] = "Phra Nakhon" },
            new() { ["ProvinceCode"] = "BKK", ["CityCode"] = "BKK_02", ["CityName"] = "Dusit" },
            new() { ["ProvinceCode"] = "CNX", ["CityCode"] = "CNX_01", ["CityName"] = "Mueang Chiang Mai" }
        };

        var bkkCities = CascadingParameterResolver.FilterOptionsByParent(cityParam, "BKK", datasetRows);
        bkkCities.Should().HaveCount(2);
        bkkCities.Select(c => c.Label).Should().BeEquivalentTo("Phra Nakhon", "Dusit");
    }
}