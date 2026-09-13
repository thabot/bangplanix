using Bangplanix.Connectors;
using Bangplanix.Connectors.Federation;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Connectors.Tests;

public class DataFederationTests
{
    [Fact]
    public void DataJoinEngine_InnerJoin_ShouldOnlyMatchEqualKeys()
    {
        var customers = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "Thabot Corp" },
            new Dictionary<string, object?> { ["Id"] = 2, ["Name"] = "Bangplanix Tech" },
            new Dictionary<string, object?> { ["Id"] = 3, ["Name"] = "Unmatched Customer" }
        };

        var orders = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["OrderId"] = "ORD-001", ["CustomerId"] = 1, ["Amount"] = 5000.0 },
            new Dictionary<string, object?> { ["OrderId"] = "ORD-002", ["CustomerId"] = 2, ["Amount"] = 12000.0 },
            new Dictionary<string, object?> { ["OrderId"] = "ORD-003", ["CustomerId"] = 99, ["Amount"] = 700.0 }
        };

        var joinDef = new DataJoinDefinition
        {
            LeftDataset = "Customers",
            RightDataset = "Orders",
            JoinType = JoinType.Inner,
            KeyMappings = [new JoinKeyMapping { LeftField = "Id", RightField = "CustomerId" }]
        };

        var result = DataJoinEngine.ExecuteJoin(customers, orders, joinDef);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r["Name"]!.ToString() == "Thabot Corp" && Convert.ToDouble(r["Amount"]) == 5000.0);
        result.Should().Contain(r => r["Name"]!.ToString() == "Bangplanix Tech" && Convert.ToDouble(r["Amount"]) == 12000.0);
    }

    [Fact]
    public void DataJoinEngine_LeftOuterJoin_ShouldPreserveLeftRowsWithNulls()
    {
        var customers = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "Thabot Corp" },
            new Dictionary<string, object?> { ["Id"] = 3, ["Name"] = "Unmatched Customer" }
        };

        var orders = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["OrderId"] = "ORD-001", ["CustomerId"] = 1, ["Amount"] = 5000.0 }
        };

        var joinDef = new DataJoinDefinition
        {
            LeftDataset = "Customers",
            RightDataset = "Orders",
            JoinType = JoinType.LeftOuter,
            RightPrefix = "Order",
            KeyMappings = [new JoinKeyMapping { LeftField = "Id", RightField = "CustomerId" }]
        };

        var result = DataJoinEngine.ExecuteJoin(customers, orders, joinDef);

        result.Should().HaveCount(2);
        var unmatched = result.First(r => r["Name"]!.ToString() == "Unmatched Customer");
        unmatched["Order_OrderId"].Should().BeNull();
        unmatched["Order_Amount"].Should().BeNull();

        var matched = result.First(r => r["Name"]!.ToString() == "Thabot Corp");
        matched["Order_OrderId"].Should().Be("ORD-001");
    }

    [Fact]
    public void DataJoinEngine_RightOuterJoin_ShouldPreserveRightRows()
    {
        var left = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["DeptId"] = "D1", ["DeptName"] = "Sales" }
        };

        var right = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["EmpId"] = "E1", ["DeptId"] = "D1", ["EmpName"] = "Alice" },
            new Dictionary<string, object?> { ["EmpId"] = "E2", ["DeptId"] = "D2", ["EmpName"] = "Bob" }
        };

        var joinDef = new DataJoinDefinition
        {
            LeftDataset = "Depts",
            RightDataset = "Employees",
            JoinType = JoinType.RightOuter,
            KeyMappings = [new JoinKeyMapping { LeftField = "DeptId", RightField = "DeptId" }]
        };

        var result = DataJoinEngine.ExecuteJoin(left, right, joinDef);

        result.Should().HaveCount(2);
        var bob = result.First(r => r["EmpName"]!.ToString() == "Bob");
        bob["DeptName"].Should().BeNull();
        bob["EmpId"].Should().Be("E2");
    }

    [Fact]
    public void DataJoinEngine_FullOuterJoin_ShouldIncludeAllRows()
    {
        var left = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Key"] = "A", ["LeftVal"] = "LeftA" },
            new Dictionary<string, object?> { ["Key"] = "B", ["LeftVal"] = "LeftB" }
        };

        var right = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Key"] = "B", ["RightVal"] = "RightB" },
            new Dictionary<string, object?> { ["Key"] = "C", ["RightVal"] = "RightC" }
        };

        var joinDef = new DataJoinDefinition
        {
            LeftDataset = "LeftTable",
            RightDataset = "RightTable",
            JoinType = JoinType.FullOuter,
            KeyMappings = [new JoinKeyMapping { LeftField = "Key", RightField = "Key" }]
        };

        var result = DataJoinEngine.ExecuteJoin(left, right, joinDef);

        result.Should().HaveCount(3);
        result.Should().Contain(r => r["LeftVal"] != null && r["LeftVal"]!.ToString() == "LeftA" && r["RightVal"] == null);
        result.Should().Contain(r => r["LeftVal"] != null && r["LeftVal"]!.ToString() == "LeftB" && r["RightVal"] != null && r["RightVal"]!.ToString() == "RightB");
        result.Should().Contain(r => r["RightVal"] != null && r["RightVal"]!.ToString() == "RightC" && r["LeftVal"] == null);
    }

    [Fact]
    public void DataJoinEngine_CrossJoin_ShouldProduceCartesianProduct()
    {
        var colors = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Color"] = "Red" },
            new Dictionary<string, object?> { ["Color"] = "Blue" }
        };

        var sizes = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Size"] = "S" },
            new Dictionary<string, object?> { ["Size"] = "M" },
            new Dictionary<string, object?> { ["Size"] = "L" }
        };

        var joinDef = new DataJoinDefinition
        {
            LeftDataset = "Colors",
            RightDataset = "Sizes",
            JoinType = JoinType.Cross
        };

        var result = DataJoinEngine.ExecuteJoin(colors, sizes, joinDef);
        result.Should().HaveCount(6);
    }

    [Fact]
    public void MasterDetailCorrelator_ShouldAttachChildRowsToParent()
    {
        var invoices = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-01", ["Customer"] = "Alpha" },
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-02", ["Customer"] = "Beta" }
        };

        var items = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-01", ["Item"] = "Pen", ["Price"] = 10 },
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-01", ["Item"] = "Notebook", ["Price"] = 50 },
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-02", ["Item"] = "Monitor", ["Price"] = 4000 }
        };

        var relDef = new MasterDetailRelationDefinition
        {
            RelationName = "LineItems",
            ParentDataset = "Invoices",
            ChildDataset = "Items",
            ParentKey = "InvoiceNo",
            ChildKey = "InvoiceNo"
        };

        var correlated = MasterDetailCorrelator.Correlate(invoices, items, relDef);

        correlated.Should().HaveCount(2);
        var inv1 = correlated.First(r => r["InvoiceNo"]!.ToString() == "INV-01");
        var details1 = inv1["LineItems"] as IEnumerable<IDictionary<string, object?>>;
        details1.Should().NotBeNull();
        details1!.Count().Should().Be(2);

        var inv2 = correlated.First(r => r["InvoiceNo"]!.ToString() == "INV-02");
        var details2 = inv2["LineItems"] as IEnumerable<IDictionary<string, object?>>;
        details2!.Count().Should().Be(1);
    }

    [Fact]
    public void DatasetAggregator_GroupBy_ShouldComputeSummaryFunctionsAndBahtText()
    {
        var sales = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Region"] = "North", ["Product"] = "Rice", ["Amount"] = 1000.0 },
            new Dictionary<string, object?> { ["Region"] = "North", ["Product"] = "Corn", ["Amount"] = 2000.0 },
            new Dictionary<string, object?> { ["Region"] = "South", ["Product"] = "Rubber", ["Amount"] = 5000.0 }
        };

        var aggDef = new DatasetAggregationDefinition
        {
            GroupByFields = ["Region"],
            Aggregations =
            [
                new AggregateFieldDefinition { SourceField = "Amount", TargetField = "TotalAmount", Function = AggregationFunction.Sum },
                new AggregateFieldDefinition { SourceField = "Amount", TargetField = "AvgAmount", Function = AggregationFunction.Avg },
                new AggregateFieldDefinition { SourceField = "Product", TargetField = "ProductCount", Function = AggregationFunction.Count },
                new AggregateFieldDefinition { SourceField = "Amount", TargetField = "AmountBahtText", Function = AggregationFunction.BahtText }
            ]
        };

        var result = DatasetAggregator.Aggregate(sales, aggDef);

        result.Should().HaveCount(2);
        var north = result.First(r => r["Region"]!.ToString() == "North");
        Convert.ToDouble(north["TotalAmount"]).Should().Be(3000.0);
        Convert.ToDouble(north["AvgAmount"]).Should().Be(1500.0);
        Convert.ToInt32(north["ProductCount"]).Should().Be(2);
        north["AmountBahtText"]!.ToString().Should().Be("สามพันบาทถ้วน");

        var south = result.First(r => r["Region"]!.ToString() == "South");
        Convert.ToDouble(south["TotalAmount"]).Should().Be(5000.0);
        south["AmountBahtText"]!.ToString().Should().Be("ห้าพันบาทถ้วน");
    }

    [Fact]
    public void DatasetAggregator_CalculatedColumns_ShouldEvaluateFormulas()
    {
        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["UnitPrice"] = 100.0, ["Quantity"] = 5, ["Discount"] = 0.1 },
            new Dictionary<string, object?> { ["UnitPrice"] = 250.0, ["Quantity"] = 2, ["Discount"] = 0.0 }
        };

        var calculated = new List<CalculatedColumnDefinition>
        {
            new CalculatedColumnDefinition { Name = "GrossTotal", Expression = "[UnitPrice] * [Quantity]" },
            new CalculatedColumnDefinition { Name = "NetTotal", Expression = "([UnitPrice] * [Quantity]) * (1 - [Discount])" }
        };

        var computedRows = DatasetAggregator.EvaluateCalculatedColumns(rows, calculated);

        computedRows.Should().HaveCount(2);
        Convert.ToDouble(computedRows[0]["GrossTotal"]).Should().Be(500.0);
        Convert.ToDouble(computedRows[0]["NetTotal"]).Should().Be(450.0);

        Convert.ToDouble(computedRows[1]["GrossTotal"]).Should().Be(500.0);
        Convert.ToDouble(computedRows[1]["NetTotal"]).Should().Be(500.0);
    }

    [Fact]
    public void DatasetAggregator_HierarchicalRollup_ShouldGenerateSubtotalsAndGrandTotal()
    {
        var sales = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Region"] = "North", ["Province"] = "ChiangMai", ["Amount"] = 1000.0 },
            new Dictionary<string, object?> { ["Region"] = "North", ["Province"] = "ChiangRai", ["Amount"] = 2000.0 },
            new Dictionary<string, object?> { ["Region"] = "South", ["Province"] = "Phuket", ["Amount"] = 5000.0 }
        };

        var aggDef = new DatasetAggregationDefinition
        {
            GroupByFields = ["Region", "Province"],
            Aggregations = [new AggregateFieldDefinition { SourceField = "Amount", TargetField = "TotalAmount", Function = AggregationFunction.Sum }],
            EnableRollup = true,
            IncludeGrandTotal = true
        };

        var rollup = DatasetAggregator.Aggregate(sales, aggDef);

        // 3 detailed rows + 2 subtotal rows (North, South) + 1 grand total row = 6 rows
        rollup.Should().HaveCount(6);

        // Verify leaf detailed rows
        rollup.Should().Contain(r => (int)r["__GroupingLevel"]! == 2 && r["Province"]!.ToString() == "ChiangMai" && Convert.ToDouble(r["TotalAmount"]) == 1000.0);

        // Verify subtotal rows
        var northSubtotal = rollup.First(r => (int)r["__GroupingLevel"]! == 1 && r["Region"]!.ToString() == "North");
        northSubtotal["__IsSubtotal"].Should().Be(true);
        Convert.ToDouble(northSubtotal["TotalAmount"]).Should().Be(3000.0);

        // Verify grand total row
        var grandTotal = rollup.First(r => (int)r["__GroupingLevel"]! == 0);
        grandTotal["__IsGrandTotal"].Should().Be(true);
        Convert.ToDouble(grandTotal["TotalAmount"]).Should().Be(8000.0);
    }

    [Fact]
    public void ParameterTemplateResolver_ShouldReplaceTemplatePlaceholdersAndHeaders()
    {
        var template = "SELECT * FROM Orders WHERE CustomerId = {CustomerId} AND Year = {{Parameters.Year}}";
        var parameters = new Dictionary<string, object?>
        {
            ["CustomerId"] = 1001,
            ["Year"] = 2026,
            ["ApiKey"] = "Secret_Token_123"
        };

        var resolved = ParameterTemplateResolver.Resolve(template, parameters);
        resolved.Should().Be("SELECT * FROM Orders WHERE CustomerId = 1001 AND Year = 2026");

        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer {{Parameters.ApiKey}}",
            ["X-App"] = "Bangplanix"
        };

        var resolvedHeaders = ParameterTemplateResolver.ResolveHeaders(headers, parameters);
        resolvedHeaders["Authorization"].Should().Be("Bearer Secret_Token_123");
        resolvedHeaders["X-App"].Should().Be("Bangplanix");
    }

    [Fact]
    public async Task DatasetMemoryCache_GetOrCreateAsync_ShouldCacheDataAndPreventDuplicateFetches()
    {
        var cache = new DatasetMemoryCache();
        var fetchCount = 0;

        Task<IReadOnlyList<IDictionary<string, object?>>> FetchData()
        {
            fetchCount++;
            return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>([new Dictionary<string, object?> { ["Id"] = 1 }]);
        }

        var result1 = await cache.GetOrCreateAsync("KEY_A", TimeSpan.FromMinutes(5), FetchData);
        var result2 = await cache.GetOrCreateAsync("KEY_A", TimeSpan.FromMinutes(5), FetchData);

        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
        fetchCount.Should().Be(1); // Fetched only once due to cache hit
    }

    [Fact]
    public async Task DataFederator_FetchPolicyFallback_ShouldReturnFallbackOnTimeoutOrError()
    {
        var report = new ReportDefinition
        {
            Datasets =
            [
                new DatasetDefinition
                {
                    Name = "FailingApi",
                    Type = DatasetType.Rest,
                    QueryOrUrl = "http://127.0.0.1:9999/unreachable",
                    FetchPolicy = new DatasetFetchPolicy
                    {
                        TimeoutSeconds = 1,
                        FallbackMode = DatasetFallbackMode.EmptyList
                    }
                }
            ]
        };

        var federator = new DataFederator();
        var result = await federator.FederateAsync(report);

        result.Datasets.Should().ContainKey("FailingApi");
        result.Datasets["FailingApi"].Should().BeEmpty();
    }

    [Fact]
    public async Task DataFederator_FederateAsync_ShouldCoordinateMultiSourceAndJoins()
    {
        var report = new ReportDefinition
        {
            Datasets =
            [
                new DatasetDefinition
                {
                    Name = "Employees",
                    Type = DatasetType.Static,
                    StaticData = new[]
                    {
                        new Dictionary<string, object?> { ["EmpId"] = 101, ["Name"] = "Somchai", ["DeptId"] = "D1" },
                        new Dictionary<string, object?> { ["EmpId"] = 102, ["Name"] = "Somsri", ["DeptId"] = "D2" }
                    }
                },
                new DatasetDefinition
                {
                    Name = "Departments",
                    Type = DatasetType.Static,
                    StaticData = new[]
                    {
                        new Dictionary<string, object?> { ["DeptId"] = "D1", ["DeptName"] = "Engineering" },
                        new Dictionary<string, object?> { ["DeptId"] = "D2", ["DeptName"] = "Marketing" }
                    }
                }
            ],
            Joins =
            [
                new DataJoinDefinition
                {
                    LeftDataset = "Employees",
                    RightDataset = "Departments",
                    OutputDatasetName = "EmployeeWithDept",
                    JoinType = JoinType.Inner,
                    KeyMappings = [new JoinKeyMapping { LeftField = "DeptId", RightField = "DeptId" }]
                }
            ]
        };

        var federator = new DataFederator();
        var result = await federator.FederateAsync(report);

        result.Datasets.Should().ContainKey("Employees");
        result.Datasets.Should().ContainKey("Departments");
        result.Datasets.Should().ContainKey("EmployeeWithDept");

        var joined = result.Datasets["EmployeeWithDept"];
        joined.Should().HaveCount(2);
        joined.Should().Contain(r => r["Name"]!.ToString() == "Somchai" && r["DeptName"]!.ToString() == "Engineering");
        joined.Should().Contain(r => r["Name"]!.ToString() == "Somsri" && r["DeptName"]!.ToString() == "Marketing");
    }
}
