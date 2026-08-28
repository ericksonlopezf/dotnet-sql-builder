// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// A mock implementation of <see cref="ISqlCompiler"/> for testing purposes.
/// </summary>
public class MockSqlCompiler : ISqlCompiler
{
    /// <inheritdoc />
    public bool SupportsCapability(ProviderCapability capability) => true;

    private sealed class MockVisitor : SqlVisitorBase
    {
        private readonly System.Text.StringBuilder _sql;
        public MockVisitor(System.Text.StringBuilder sql)
        {
            _sql = sql;
        }

        public override void VisitUnknown(ISqlNode node)
        {
            _sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"[{node.GetType().Name}] ");
        }

        public override void Visit(EricksonLopez.SqlBuilder.Abstractions.Nodes.FromNode node) => VisitUnknown(node);
        public override void Visit(EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionWhereNode node) => VisitUnknown(node);
    }
    
    /// <inheritdoc />
    public SqlResult Compile(ISqlQuery query) => Compile(query, null);

    /// <inheritdoc />
    public SqlResult Compile(ISqlQuery query, IParameterManager? existingParameters)
    {
        var parameters = existingParameters ?? new ParameterManager();
        using var context = new CompilationContext(parameters);
        var visitor = new MockVisitor(context.Sql);
        
        if (query is IAstQuery astQuery)
        {
            foreach (var node in astQuery.Nodes)
            {
                node.Accept(visitor);
            }
        }
        else if (query is RawQuery raw)
        {
            context.Sql.Append(raw.RawSql);
        }
        return new SqlResult(context.Sql.ToString().Trim(), context.Parameters.GetParameters());
    }

    /// <inheritdoc />
    public string Escape(string identifier) => identifier;
    /// <inheritdoc />
    public string EscapeIdentifier(string identifier) => identifier;
    
    /// <inheritdoc />
    public void EscapeIdentifier(System.Text.StringBuilder sb, System.ReadOnlySpan<char> identifier)
    {
        sb.Append(identifier);
    }

    /// <inheritdoc />
    public void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
    /// <inheritdoc />
    public void CompileInsert(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
    /// <inheritdoc />
    public void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}
    /// <inheritdoc />
    public void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor) {}

    /// <inheritdoc />
    public IParameterManager CreateParameterManager() => new ParameterManager();
}





