// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.SourceGenerators.Tests
{
    
    public class FilterGeneratorTests : GeneratorTestBase
    {
        [Fact]
        public Task Generator_CreatesFilters_ForAnnotatedClass()
        {
            var source = @"
using EricksonLopez.SqlBuilder.Annotations;

namespace TestNamespace
{
    [SqlEntity(""users"")]
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_Nullable_Properties()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""orders"")]
    public partial class Order
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? Notes { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_NestedClasses()
        {
            var source = @"

namespace TestNamespace
{
    public class Wrapper
    {
        [SqlEntity(""items"")]
        public partial class Item
        {
            public int Id { get; set; }
            public decimal Price { get; set; }
        }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_NoNamespace()
        {
            var source = @"

[SqlEntity(""globals"")]
public partial class GlobalConfig
{
    public int Key { get; set; }
    public string Value { get; set; }
}
";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }
        
        [Fact]
        public Task Generator_Handles_Structs()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""structs"")]
    public partial struct DataStruct
    {
        public int Id { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_Records()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""records"")]
    public partial record DataRecord
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_AllSupportedTypes()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""all_types"")]
    public partial class AllTypesEntity
    {
        public int IntVal { get; set; }
        public long LongVal { get; set; }
        public decimal DecimalVal { get; set; }
        public double DoubleVal { get; set; }
        public DateTime DateTimeVal { get; set; }
        public DateOnly DateOnlyVal { get; set; }
        public string StringVal { get; set; }
        public bool BoolVal { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }
        [Fact]
        public Task Generator_Ignores_InvalidMembers()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""invalid_table"")]
    public partial class InvalidEntity
    {
        public int Id { get; set; }
        public static string StaticProp { get; set; }
        public string PrivateSet { get; private set; }
        public string NoGet { set { } }
        private string PrivateProp { get; set; }
        protected string ProtectedProp { get; set; }
        
        public void SomeMethod() { }
        public string SomeField = ""test"";
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }
        [Fact]
        public Task Generator_Handles_InvalidSyntax()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity]
    public partial class // syntax error here missing class name!
    {
        public int Id { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<FilterGenerator>(source);
        }
    }
}



