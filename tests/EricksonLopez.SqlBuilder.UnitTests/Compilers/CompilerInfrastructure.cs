// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.UnitTests.Compilers;

internal sealed class UserEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? Age { get; set; }

    public string GetTableName() => "users";
    public string[] GetColumnNames() => new[] { "id", "name", "age" };
    public object?[] GetValues() => new object?[] { Id, Name, Age };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Id", "id" }, { "Name", "name" }, { "Age", "age" }
    };
    public string[] GetIndexedColumns() => Array.Empty<string>();
}

internal sealed record CustomExtensionNode : SqlExtensionNode
{
    public override void Accept(ISqlVisitor visitor) => visitor.VisitExtension(this);
}

internal static class CustomHelper
{
    public static string? NullIf(string a, string b) => a == b ? null : a;
    public static bool IsDistinctFrom(int a, int b) => a != b;
    public static bool IsNotDistinctFrom(int a, int b) => a == b;
    public static string Outer(string val) => "custom";
}

internal class TestExtensionVisitor : SqlCompilerVisitor
{
    public TestExtensionVisitor(ISqlCompiler compiler, CompilationContext context) : base(compiler, context) { }
    public override void VisitExtension(SqlExtensionNode node)
    {
        Context.Sql.Append("/* ext */");
    }
    public string TestEscapeIdentifier(string id) => EscapeIdentifier(id);
}

internal class TestNonTrailingSpaceVisitor : SqlCompilerVisitor
{
    public TestNonTrailingSpaceVisitor(ISqlCompiler compiler, CompilationContext context) : base(compiler, context) { }
    public override void Visit(UpdateNode node) => Context.Sql.Append("UPDATE custom");
    public override void Visit(DeleteNode node) => Context.Sql.Append("DELETE custom");
}

internal class TestNoSpaceCompiler : SqlCompilerBase
{
    protected override ISqlRenderer AotRenderer => new SqlServerRenderer(this);
    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new TestNonTrailingSpaceVisitor(this, context);
    public override string EscapeIdentifier(string identifier) => $"[{identifier}]";
}

internal class TestDefaultCompiler : SqlCompilerBase
{
    protected override ISqlRenderer AotRenderer => new SqlServerRenderer(this);
    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new TestExtensionVisitor(this, context);
    public override string EscapeIdentifier(string identifier) => $"[{identifier}]";
    public override void EscapeIdentifier(System.Text.StringBuilder sb, ReadOnlySpan<char> identifier)
    {
        sb.Append('[').Append(identifier).Append(']');
    }
}

internal class TestCustomDistinctCompiler : TestDefaultCompiler
{
    internal override void CompileDistinct(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context)
    {
        if (partition.DistinctOnNode != null)
        {
            context.Sql.Append("DISTINCT ON (custom) ");
        }
    }
}
