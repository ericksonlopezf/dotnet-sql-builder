// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Annotations;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests
{
    [Collection("SqlEntityCache")]
    public class SqlEntityCacheMissingBranchesTests
    {
        public class EntityNoTableField : ISqlEntity
        {
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>();
            public string GetTableName() => "dummy";
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        public class EntityNullTableField : ISqlEntity
        {
            public static readonly string TableName = null!;
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>();
            public string GetTableName() => "dummy";
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        public class EntityNotStringTableField : ISqlEntity
        {
            public static readonly int TableName = 1;
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>();
            public string GetTableName() => "dummy";
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        public class EntityNoPropMapField : ISqlEntity
        {
            public static readonly string TableName = "table";
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        }

        public class EntityNullPropMapField : ISqlEntity
        {
            public static readonly string TableName = "table";
#pragma warning disable CS8625
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = null;
#pragma warning restore CS8625
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        }

        public class EntityNotDictPropMapField : ISqlEntity
        {
            public static readonly string TableName = "table";
            public static readonly int PropertyMap = 1;
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        }

        public class EntityValidEmptyIndexed : ISqlEntity
        {
            public static readonly string TableName = "table";
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>();
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasNoTableField_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityNoTableField>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasNullTableField_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityNullTableField>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasNotStringTableField_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityNotStringTableField>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasNoPropMapField_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityNoPropMapField>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasNullPropMapField_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityNullPropMapField>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasNotDictPropMapField_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityNotDictPropMapField>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasValidEmptyIndexed_ShouldNotThrow()
        {
            Action act = () => { var x = SqlEntityCache<EntityValidEmptyIndexed>.TableName; };
            act.Should().NotThrow();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasInstanceWithNonEmptyIndexed_ShouldPopulateMetadata()
        {
            Action act = () => { var x = SqlEntityCache<EntityWithInstanceNonEmpty>.TableName; };
            act.Should().NotThrow();
            SqlEntityCache<EntityWithInstanceNonEmpty>.ColumnNames.Should().BeEquivalentTo("id");
            SqlEntityCache<EntityWithInstanceNonEmpty>.IndexedColumns.Should().BeEquivalentTo("id");
        }

        [Fact]
        public void SqlEntityCache_WhenEntityHasInstanceWithEmptyIndexed_ShouldPopulateMetadata()
        {
            Action act = () => { var x = SqlEntityCache<EntityWithInstanceEmpty>.TableName; };
            act.Should().NotThrow();
            SqlEntityCache<EntityWithInstanceEmpty>.ColumnNames.Should().BeEquivalentTo("id");
            SqlEntityCache<EntityWithInstanceEmpty>.IndexedColumns.Should().BeEmpty();
        }

        [Fact]
        public void SqlEntityCache_WhenEntityWithoutInstanceHasNonEmptyIndexed_ShouldPopulateMetadata()
        {
            Action act = () => { var x = SqlEntityCache<EntityWithoutInstanceNonEmpty>.TableName; };
            act.Should().NotThrow();
            SqlEntityCache<EntityWithoutInstanceNonEmpty>.ColumnNames.Should().BeEquivalentTo("id");
            SqlEntityCache<EntityWithoutInstanceNonEmpty>.IndexedColumns.Should().BeEquivalentTo("id");
        }

        [Fact]
        public void SqlEntityCache_WhenEntityIsUnannotatedPoco_ShouldThrowTypeInitializationException()
        {
            Action act = () => { var x = SqlEntityCache<UnannotatedPoco>.TableName; };
            act.Should().Throw<TypeInitializationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage($"Type {typeof(UnannotatedPoco).Name} does not implement ISqlEntity. NativeAOT paths require the [SqlEntity] attribute on all models. To use unannotated POCOs, use Sql.From<T>(\"tableName\").");
        }

        public class EntityWithInstanceNonEmpty : ISqlEntity
        {
            public static readonly ISqlEntity Instance = new EntityWithInstanceNonEmpty();
            public static readonly string TableName = "table";
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>();
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => new[] { "id" };
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        public class EntityWithInstanceEmpty : ISqlEntity
        {
            public static readonly ISqlEntity Instance = new EntityWithInstanceEmpty();
            public static readonly string TableName = "table";
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>();
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => Array.Empty<string>();
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        public class EntityWithoutInstanceNonEmpty : ISqlEntity
        {
            public string GetTableName() => "table";
            public string[] GetColumnNames() => new[] { "id" };
            public string[] GetIndexedColumns() => new[] { "id" };
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        }
        public class EntityValidStaticFields : ISqlEntity
        {
            public static readonly string TableName = "MyStaticTable";
            public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string> { { "Prop", "Col" } };
            public string GetTableName() => TableName;
            public string[] GetColumnNames() => new[] { "Col", "ExtraCol" };
            public string[] GetIndexedColumns() => new[] { "Col" };
            public object?[] GetValues() => Array.Empty<object?>();
            public string[] GetAllColumnNames() => GetColumnNames();
            public object?[] GetAllValues() => GetValues();
            public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
        }

        public class UnannotatedPoco
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void SqlEntityCache_ConcurrentAccessFromMultipleThreads_ShouldBeThreadSafeAndDeterministic()
        {
            const int threadCount = 100;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, i =>
            {
                try
                {
                    // Access multiple cached properties concurrently
                    var tableName = SqlEntityCache<EntityValidStaticFields>.TableName;
                    var columnNames = SqlEntityCache<EntityValidStaticFields>.ColumnNames;
                    var indexedColumns = SqlEntityCache<EntityValidStaticFields>.IndexedColumns;
                    var propertyMap = SqlEntityCache<EntityValidStaticFields>.PropertyMap;

                    if (tableName != "MyStaticTable")
                        throw new InvalidOperationException($"Unexpected table name: {tableName}");

                    if (columnNames.Length != 2 || columnNames[0] != "Col" || columnNames[1] != "ExtraCol")
                        throw new InvalidOperationException("Unexpected column names in concurrent access");

                    if (indexedColumns.Count != 1 || !indexedColumns.Contains("Col"))
                        throw new InvalidOperationException("Unexpected indexed columns in concurrent access");

                    if (!propertyMap.TryGetValue("Prop", out var col) || col != "Col")
                        throw new InvalidOperationException("Unexpected property map in concurrent access");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty("Concurrent access to SqlEntityCache<T> must never throw or corrupt state");
        }

        [Fact]
        public void SqlEntityCache_HighConcurrency_MultiEntityStressTest()
        {
            const int iterations = 200;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
            {
                try
                {
                    if (i % 2 == 0)
                    {
                        var t1 = SqlEntityCache<EntityValidStaticFields>.TableName;
                        var cols1 = SqlEntityCache<EntityValidStaticFields>.ColumnNames;
                        if (t1 != "MyStaticTable" || cols1.Length != 2)
                            throw new InvalidOperationException("EntityValidStaticFields cache corrupted");
                    }
                    else
                    {
                        var t2 = SqlEntityCache<EntityWithoutInstanceNonEmpty>.TableName;
                        var cols2 = SqlEntityCache<EntityWithoutInstanceNonEmpty>.ColumnNames;
                        if (t2 != "table" || cols2.Length != 1)
                            throw new InvalidOperationException("EntityWithoutInstanceNonEmpty cache corrupted");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty("Concurrent multi-entity access to SqlEntityCache must be completely thread-safe");
        }
    }
}







