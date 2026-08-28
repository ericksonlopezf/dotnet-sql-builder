// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class TestingLibraryTests
{
    [Fact]
    public void QueryAssert_ParametersMatch_ValidatesCorrectly()
    {
        var result = new SqlResult("SELECT 1", new Dictionary<string, object> { { "p0", 42 }, { "p1", "test" } });
        QueryAssert.ParametersMatch(result, ("p0", 42), ("p1", "test"));

        // Mismatch in count
        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.ParametersMatch(result, ("p0", 42)));

        // Missing key
        Assert.Throws<Xunit.Sdk.TrueException>(() =>
            QueryAssert.ParametersMatch(result, ("p0", 42), ("p99", "missing")));

        // Value mismatch
        Assert.Throws<Xunit.Sdk.EqualException>(() =>
            QueryAssert.ParametersMatch(result, ("p0", 100), ("p1", "test")));
    }

    [Fact]
    public void QueryAssert_SqlMatches_VariousDialects()
    {
        var query = Sql.From<TestingUser>().Where(u => u.Id == 1);

        QueryAssert.SqlMatchesPostgreSql(query, "SELECT * FROM \"testingusers\" WHERE (id = @p0)");
        QueryAssert.SqlMatchesSqlServer(query, "SELECT * FROM [testingusers] WHERE (id = @p0)");
        QueryAssert.SqlMatchesSqlite(query, "SELECT * FROM \"testingusers\" WHERE (id = @p0)");
        QueryAssert.SqlMatchesMySql(query, "SELECT * FROM `testingusers` WHERE (id = @p0)");
        QueryAssert.SqlMatchesOracle(query, "SELECT * FROM \"TESTINGUSERS\" WHERE (id = :p0)");
    }

    [Fact]
    public void QueryAssert_QueriesMatch_ValidatesEquivalence()
    {
        var compiler = new PostgreSqlCompiler();
        var q1 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var q2 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var q3 = Sql.From<TestingUser>().Where(u => u.IsActive);

        QueryAssert.QueriesMatch(q1, q2, compiler);

        Assert.Throws<Xunit.Sdk.XunitException>(() =>
            QueryAssert.QueriesMatch(q1, q3, compiler));
    }

    [Fact]
    public void QueryComparer_DetectsAllDifferences()
    {
        var compiler = new PostgreSqlCompiler();

        // 1. SQL mismatch
        var q1 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var q2 = Sql.From<TestingUser>().Where(u => u.IsActive);
        var res1 = QueryComparer.Compare(q1, q2, compiler);
        Assert.False(res1.AreEqual);
        Assert.Contains(res1.Differences, d => d.Contains("SQL mismatch"));

        // 2. Parameter count mismatch
        var qParams1 = new RawQuery("SELECT * FROM u WHERE a = @p0", new Dictionary<string, object?> { { "p0", 1 } });
        var qParams2 = new RawQuery("SELECT * FROM u WHERE a = @p0", new Dictionary<string, object?> { { "p0", 1 }, { "p1", 2 } });
        var res2 = QueryComparer.Compare(qParams1, qParams2, compiler);
        Assert.False(res2.AreEqual);
        Assert.Contains(res2.Differences, d => d.Contains("Parameter count mismatch"));

        // 3. Parameter value mismatch
        var qVal1 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 1 } });
        var qVal2 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 2 } });
        var res3 = QueryComparer.Compare(qVal1, qVal2, compiler);
        Assert.False(res3.AreEqual);
        Assert.Contains(res3.Differences, d => d.Contains("Parameter value mismatch"));

        // 4. Missing parameter in actual
        var qMiss1 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 1 } });
        var qMiss2 = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p1", 1 } });
        var res4 = QueryComparer.Compare(qMiss1, qMiss2, compiler);
        Assert.False(res4.AreEqual);
        Assert.Contains(res4.Differences, d => d.Contains("Missing parameter"));

        // 5. AST node count mismatch
        var qAst1 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var qAst2 = Sql.From<TestingUser>().Where(u => u.Age > 18).OrderBy(u => u.Name);
        var res5 = QueryComparer.Compare(qAst1, qAst2, compiler);
        Assert.False(res5.AreEqual);
        Assert.Contains(res5.Differences, d => d.Contains("AST Node count mismatch"));

        // 6. AST node type mismatch
        var qType1 = Sql.From<TestingUser>().OrderBy(u => u.Name);
        var qType2 = Sql.From<TestingUser>().Where(u => u.Age > 18);
        var res6 = QueryComparer.Compare(qType1, qType2, compiler);
        Assert.False(res6.AreEqual);
        Assert.Contains(res6.Differences, d => d.Contains("AST Node type mismatch"));
    }

    [Fact]
    public void QueryTestingExtensions_ShouldGenerate_ValidatesProperly()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.Id == 5);

        // Success with parameters
        query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)", 5);

        // Success without parameters passed
        query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)");

        // SQL mismatch
        Assert.Throws<Exception>(() =>
            query.ShouldGenerate(compiler, "SELECT * FROM other_table"));

        // Parameter count mismatch
        Assert.Throws<Exception>(() =>
            query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)", 5, 10));

        // Parameter value mismatch
        Assert.Throws<Exception>(() =>
            query.ShouldGenerate(compiler, "SELECT * FROM \"testingusers\" WHERE (id = @p0)", 99));
    }

    [Fact]
    public void SnapshotAssert_MatchesSnapshot_And_MatchesContract()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Where(u => u.IsActive);

        // MatchesSnapshot
        SnapshotAssert.MatchesSnapshot(query, compiler, "SELECT * FROM \"testingusers\" WHERE is_active");

        Assert.Throws<InvalidOperationException>(() =>
            SnapshotAssert.MatchesSnapshot(query, compiler, "SELECT * FROM wrong_table"));

        // MatchesContract
        var contract = query.GetContract();
        SnapshotAssert.MatchesContract(query, contract.Fingerprint);

        Assert.Throws<InvalidOperationException>(() =>
            SnapshotAssert.MatchesContract(query, "wrong_fingerprint"));
    }

    [Fact]
    public void DiagnosticActivityScope_CapturesActivities()
    {
        using (var scope = DiagnosticActivityScope.Start("TestCustomSource"))
        {
            var source = new System.Diagnostics.ActivitySource("TestCustomSource");
            using (var activity = source.StartActivity("TestOp"))
            {
                activity?.SetTag("test.tag", "value");
            }

            Assert.NotEmpty(scope.Activities);
            Assert.Contains(scope.Activities, a => a.OperationName == "TestOp");
        }
    }

    [Fact]
    public void DummyEntity_And_ThreeColumnEntity_PropertiesAndMetadata()
    {
        var dummy = new DummyEntity
        {
            Id = 10,
            Name = "Dummy",
            Version = 2,
            RowGuid = Guid.NewGuid(),
            IsActive = true
        };

        Assert.Equal("dummy_entity", dummy.GetTableName());
        Assert.Equal(2, dummy.GetColumnNames().Length);
        Assert.Equal(2, dummy.GetValues().Length);
        Assert.Equal(2, dummy.GetAllColumnNames().Length);
        Assert.Equal(2, dummy.GetAllValues().Length);
        Assert.Equal("id", dummy.GetPropertyMap()["Id"]);
        Assert.Empty(dummy.GetIndexedColumns());
        Assert.Equal("dummy", DummyEntity.TableName);
        Assert.Equal(2, DummyEntity.ColumnCount);
        Assert.Equal(2, DummyEntity.GetColumns().Length);
        Assert.False(DummyEntity.IsNull(dummy, 0));
        Assert.False(DummyEntity.IsDefault(dummy, 0));
        Assert.False(DummyEntity.AreEqual(dummy, dummy, 0));
        Assert.Equal("Id", DummyEntity.GetColumnName(0));
        Assert.Equal("Name", DummyEntity.GetColumnName(1));

        var pm = new ParameterManager();
        Assert.NotNull(DummyEntity.BindParameter(dummy, 0, pm));
        Assert.NotNull(DummyEntity.BindParameter(dummy, 1, pm));
        DummyEntity.ExtractColumnArrays(new[] { dummy }, new[] { true, true }, pm);
        Assert.NotNull(DummyEntity.FromReader(Substitute.For<IDataReader>()));
        Assert.NotNull(DummyEntity.GetReaderParser());

        var three = new ThreeColumnEntity
        {
            Id = "1",
            Name = "Name1",
            Status = "Active"
        };
        Assert.Equal("TestEntity", three.GetTableName());
        Assert.Equal(3, three.GetColumnNames().Length);
        Assert.Equal(3, three.GetValues().Length);
        Assert.Equal(3, three.GetAllColumnNames().Length);
        Assert.Equal(3, three.GetAllValues().Length);
        Assert.Equal("Name", three.GetPropertyMap()["Name"]);
        Assert.Empty(three.GetIndexedColumns());
        Assert.Equal(3, ThreeColumnEntity.ColumnCount);
        Assert.Equal("Id", ThreeColumnEntity.GetColumnName(0));
        Assert.Equal("Name", ThreeColumnEntity.GetColumnName(1));
        Assert.Equal("Status", ThreeColumnEntity.GetColumnName(2));
        Assert.Equal(3, ThreeColumnEntity.GetColumns().Length);
        Assert.NotNull(ThreeColumnEntity.BindParameter(three, 0, pm));
        Assert.NotNull(ThreeColumnEntity.BindParameter(three, 1, pm));
        Assert.NotNull(ThreeColumnEntity.BindParameter(three, 2, pm));
        Assert.False(ThreeColumnEntity.IsNull(three, 0));
        Assert.False(ThreeColumnEntity.IsDefault(three, 0));
        Assert.False(ThreeColumnEntity.AreEqual(three, three, 0));
        ThreeColumnEntity.ExtractColumnArrays(new[] { three }, new[] { true, true, true }, pm);
        Assert.NotNull(ThreeColumnEntity.FromReader(Substitute.For<IDataReader>()));
        Assert.NotNull(ThreeColumnEntity.GetReaderParser());
    }

    [Fact]
    public void MockSqlCompiler_ExercisesAllMembers()
    {
        var mock = new MockSqlCompiler();
        Assert.True(mock.SupportsCapability(ProviderCapability.Merge));
        Assert.Equal("abc", mock.Escape("abc"));
        Assert.Equal("abc", mock.EscapeIdentifier("abc"));

        var sb = new System.Text.StringBuilder();
        mock.EscapeIdentifier(sb, "ident");
        Assert.Equal("ident", sb.ToString());

        var query = Sql.From<TestingUser>().Where(u => u.Id == 1);
        var res1 = mock.Compile(query);
        Assert.NotNull(res1.Sql);

        var rawQuery = new RawQuery("SELECT 42");
        var res2 = mock.Compile(rawQuery, new ParameterManager());
        Assert.Equal("SELECT 42", res2.Sql);

        mock.CompileSelect(Array.Empty<ISqlNode>(), Substitute.For<ISqlVisitor>());
        mock.CompileInsert(Array.Empty<ISqlNode>(), Substitute.For<ISqlVisitor>());
        mock.CompileUpdate(Array.Empty<ISqlNode>(), Substitute.For<ISqlVisitor>());
        mock.CompileDelete(Array.Empty<ISqlNode>(), Substitute.For<ISqlVisitor>());
        Assert.NotNull(mock.CreateParameterManager());
    }

    [Fact]
    public void DataBuilders_And_ObjectMother_ExerciseAllBuilders()
    {
        var customer = EricksonLopez.SqlBuilder.Testing.DataBuilders.CustomerBuilder.Create()
            .WithId(2)
            .WithName("Cust2")
            .WithEmail("cust2@test.com")
            .WithPhone("123")
            .WithActive(true)
            .Build();
        Assert.Equal(2, customer.Id);
        Assert.Equal("Cust2", customer.Name);

        var order = EricksonLopez.SqlBuilder.Testing.DataBuilders.OrderBuilder.Create()
            .WithId(3)
            .WithCustomerId(2)
            .WithStatus("shipped")
            .WithTotalAmount(200m)
            .WithCurrency("EUR")
            .WithDeleted(false)
            .Build();
        Assert.Equal(3, order.Id);
        Assert.Equal(200m, order.TotalAmount);

        var product = EricksonLopez.SqlBuilder.Testing.DataBuilders.ProductBuilder.Create()
            .WithId(4)
            .WithCategoryId(1)
            .WithName("Prod4")
            .WithSku("SKU-4")
            .WithPrice(50m)
            .WithCostPrice(30m)
            .WithStock(10)
            .WithActive(true)
            .Build();
        Assert.Equal(4, product.Id);
        Assert.Equal(50m, product.Price);

        var user = EricksonLopez.SqlBuilder.Testing.DataBuilders.UserBuilder.Create()
            .WithId(5)
            .WithUsername("U5")
            .WithEmail("u5@test.com")
            .WithActive(true)
            .WithFailedLoginAttempts(1)
            .WithCreatedAt(DateTime.UtcNow)
            .Build();
        Assert.Equal(5, user.Id);
        Assert.Equal("U5", user.Username);

        // ObjectMother
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateUser());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateTestEntity());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateProduct());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateOrder());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateOrderItem());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateCustomer());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateAddress());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateCategory());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateInvoice());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreatePayment());
        Assert.NotNull(EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother.CreateAuditLog());
    }

    [Fact]
    public void TestDataSeeder_GeneratesReproducibleDatasets()
    {
        var dataset = EricksonLopez.SqlBuilder.Testing.Seeders.TestDataSeeder.Generate(42);
        Assert.NotEmpty(dataset.Customers);
        Assert.NotEmpty(dataset.Categories);
        Assert.NotEmpty(dataset.Products);
        Assert.NotEmpty(dataset.Orders);
        Assert.NotEmpty(dataset.OrderItems);
        Assert.NotEmpty(dataset.Invoices);
        Assert.NotEmpty(dataset.Payments);

        var users = EricksonLopez.SqlBuilder.Testing.Seeders.TestDataSeeder.Users(5, 42);
        Assert.Equal(5, users.Count);

        var addresses = EricksonLopez.SqlBuilder.Testing.Seeders.TestDataSeeder.Addresses(dataset.Customers.Take(3).ToList(), 42);
        Assert.NotEmpty(addresses);
    }

    [Fact]
    public void GoldenFileAssert_AllBranches_And_EdgeCases()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("id", "name");
        var tempDir = Path.Combine(Path.GetTempPath(), "golden_tests_" + Guid.NewGuid().ToString("N"));
        var tempFile = Path.Combine(tempDir, "test.sql");

        try
        {
            // 1. Create file and directory when updateGoldenFiles is true
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: true, normalizeWhitespace: true);
            Assert.True(File.Exists(tempFile));

            // 2. Matches existing file without update
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: false, normalizeWhitespace: true);

            // 3. Matches without whitespace normalization
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: true, normalizeWhitespace: false);
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: false, normalizeWhitespace: false);

            // 4. Mismatch throws InvalidOperationException
            var mismatchQuery = Sql.From<TestingUser>().Select("id");
            Assert.Throws<InvalidOperationException>(() =>
                GoldenFileAssert.MatchesGoldenFile(mismatchQuery, compiler, tempFile, updateGoldenFiles: false));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Normalization_And_EmptyChecks()
    {
        var compiler = new PostgreSqlCompiler();
        var wsQuery = new RawQuery("  SELECT   1 \r\n \t ");

        QueryAssert.SqlMatches(wsQuery, compiler, "SELECT 1");
        SnapshotAssert.MatchesSnapshot(wsQuery, compiler, "SELECT 1");
        
        var q1 = new RawQuery(" SELECT 1 ");
        var q2 = new RawQuery("  SELECT   1\t");
        var res = QueryComparer.Compare(q1, q2, compiler);
        Assert.True(res.AreEqual);
    }
}

public enum TestingStatus
{
    Active,
    Inactive
}

public class TestingUser : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }

    public string GetTableName() => "testingusers";
    public string[] GetColumnNames() => new[] { "id", "name", "age", "is_active" };
    public object?[] GetValues() => new object?[] { Id, Name, Age, IsActive };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Id", "id" }, { "Name", "name" }, { "Age", "age" }, { "IsActive", "is_active" }
    };
    public string[] GetIndexedColumns() => System.Array.Empty<string>();
}




