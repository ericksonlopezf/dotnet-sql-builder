// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.SourceGenerators.Tests
{
    
    public class SqlEntityGeneratorTests : GeneratorTestBase
    {
        [Fact]
        public Task Generator_CreatesMapAndConstants_ForAnnotatedClass()
        {
            var source = @"
using EricksonLopez.SqlBuilder.Annotations;

namespace TestNamespace
{
    [SqlEntity(""users"")]
    public partial class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
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
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
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
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
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
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
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
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_RecordStructs()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""record_structs"")]
    public partial record struct DataRecordStruct
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_ReportsDiagnostic_WhenNotPartial()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""not_partial"")]
    public class NotPartialClass
    {
        public int Id { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_ColumnAttribute()
        {
            var source = @"
using System.ComponentModel.DataAnnotations.Schema;

namespace TestNamespace
{
    [SqlEntity(""custom_table"")]
    public partial class CustomEntity
    {
        [Column(""custom_id"")]
        public int Id { get; set; }
        
        [Column(""full_name"")]
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }
        
        [Fact]
        public Task Generator_Handles_KeyAttribute()
        {
            var source = @"
using System.ComponentModel.DataAnnotations;

namespace TestNamespace
{
    [SqlEntity(""key_table"")]
    public partial class KeyEntity
    {
        [Key]
        public int SpecialId { get; set; }
        
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_SpecialAttributes()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""special_table"")]
    public partial class SpecialEntity
    {
        [Key, DatabaseGenerated]
        public int Id { get; set; }
        
        [Indexed]
        public string SearchTerm { get; set; }
        
        [Indexed]
        public int SortOrder { get; set; }
        
        [DatabaseGenerated]
        public string ComputedHash { get; set; }
        
        [GeneratedColumn]
        public string AutoCol { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
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
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }
        [Fact]
        public Task Generator_UsesDefaultTableName_WhenNoArgument()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity]
    public partial class DefaultTableEntity
    {
        public int Id { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_Enums_And_Guids()
        {
            var source = @"

namespace TestNamespace
{
    public enum Status { Active, Inactive }
    public enum SmallStatus : byte { Active, Inactive }
    public enum LongStatus : long { Active, Inactive }
    public enum ShortStatus : short { Active, Inactive }

    [SqlEntity]
    public partial class EnumGuidEntity
    {
        public int Id { get; set; }
        public Status Status { get; set; }
        public SmallStatus Small { get; set; }
        public LongStatus LongS { get; set; }
        public ShortStatus ShortS { get; set; }
        public Guid Uuid { get; set; }
        public Guid? NullableUuid { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_AllTypes()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity]
    public partial class AllTypesEntity
    {
        public int Id { get; set; }
        public long LongVal { get; set; }
        public short ShortVal { get; set; }
        public byte ByteVal { get; set; }
        public bool BoolVal { get; set; }
        public string StringVal { get; set; }
        public DateTime DateTimeVal { get; set; }
        public decimal DecimalVal { get; set; }
        public double DoubleVal { get; set; }
        public float FloatVal { get; set; }
        public char CharVal { get; set; }
        public TimeSpan TimeSpanVal { get; set; }
        public byte[] ByteArrayVal { get; set; }
        
        // Nullables
        public int? NId { get; set; }
        public long? NLongVal { get; set; }
        public short? NShortVal { get; set; }
        public byte? NByteVal { get; set; }
        public bool? NBoolVal { get; set; }
        public DateTime? NDateTimeVal { get; set; }
        public decimal? NDecimalVal { get; set; }
        public double? NDoubleVal { get; set; }
        public float? NFloatVal { get; set; }
        public char? NCharVal { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }
        
        [Fact]
        public Task Generator_Handles_CustomKeyAttributes()
        {
            var source = @"

namespace EricksonLopez.SqlBuilder.Annotations
{
    public class KeyAttribute : System.Attribute { }
    public class PrimaryKeyAttribute : System.Attribute { }
}

namespace TestNamespace
{
    [SqlEntity]
    public partial class CustomKeyEntity
    {
        [EricksonLopez.SqlBuilder.Annotations.Key]
        public int KeyId { get; set; }
        
        [EricksonLopez.SqlBuilder.Annotations.PrimaryKey]
        public int PrimaryId { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
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
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_UnresolvedAttribute()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity]
    public partial class UnresolvedAttrEntity
    {
        public int Id { get; set; }
        [NonExistentAttribute]
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<SqlEntityGenerator>(source);
        }
    }
}



