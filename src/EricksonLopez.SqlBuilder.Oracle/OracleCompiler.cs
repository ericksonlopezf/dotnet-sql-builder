// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.Oracle;

/// <summary>
/// Provides Oracle Database specific compilation and dialect rules.
/// </summary>
[RequiresDynamicCode("Oracle dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("Oracle dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public class OracleCompiler : SqlCompilerBase
{
    /// <inheritdoc />
    public override IParameterManager CreateParameterManager() => new OracleParameterManager();
    /// <inheritdoc />
    public override string EscapeIdentifier(string identifier) => $"\"{identifier.ToUpperInvariant()}\"";
    
    /// <inheritdoc />
    public override void EscapeIdentifier(StringBuilder sb, ReadOnlySpan<char> identifier)
    {
        sb.Append('"');
        foreach (char c in identifier)
        {
            sb.Append(char.ToUpperInvariant(c));
        }
        sb.Append('"');
    }

    private readonly ISqlRenderer _aotRenderer;

    /// <summary>
    /// Gets the Oracle Database dialect version targeted by this compiler instance.
    /// </summary>
    public OracleDialectVersion DialectVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleCompiler"/> class.
    /// </summary>
    /// <param name="version">The target Oracle Database dialect version.</param>
    public OracleCompiler(OracleDialectVersion version = OracleDialectVersion.Oracle12cPlus)
    {
        DialectVersion = version;
        _aotRenderer = CreateAotRenderer();
    }

    /// <summary>
    /// Creates the AOT renderer for this compiler instance.
    /// Override in derived dialect compilers to provide a custom renderer.
    /// </summary>
    /// <remarks>
    /// Called once in the constructor after the instance is fully constructed.
    /// </remarks>
    /// <returns>A new <see cref="ISqlRenderer"/> instance for Oracle.</returns>
    protected virtual ISqlRenderer CreateAotRenderer() => new OracleRenderer(this);

    /// <inheritdoc />
    protected override ISqlRenderer AotRenderer => _aotRenderer;

    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new OracleVisitor(this, context);

    internal override void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var limitNode = partition.LimitNode;

        if (DialectVersion == OracleDialectVersion.Oracle11g && limitNode != null)
        {
            var innerNodes = nodes.Where(n => !(n is LimitOffsetNode)).ToList();
            var innerContext = new CompilationContext(context.Parameters);
            var innerVisitor = CreateVisitor(innerContext);
            base.CompileSelect(innerNodes, innerVisitor, innerContext);
            var innerSql = innerContext.Sql.ToString().TrimEnd();

            var limit = limitNode.Limit;
            var offset = limitNode.Offset;

            if (offset.HasValue && limit.HasValue)
            {
                int maxRow = offset.Value + limit.Value;
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture,
                    $"SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM ({innerSql}) a_ WHERE ROWNUM <= {maxRow}) WHERE rnum_ > {offset.Value}");
            }
            else if (limit.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture,
                    $"SELECT * FROM ({innerSql}) WHERE ROWNUM <= {limit.Value}");
            }
            else if (offset.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture,
                    $"SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM ({innerSql}) a_) WHERE rnum_ > {offset.Value}");
            }
        }
        else
        {
            base.CompileSelect(nodes, visitor, context);
        }
    }

    internal override void CompileInsert(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var insertSelectNode = nodes.OfType<InsertSelectNode>().LastOrDefault();
        if (insertSelectNode != null)
        {
            insertSelectNode.Accept(visitor);
        }
        else
        {
            var insertNode = nodes.OfType<InsertNode>().LastOrDefault();
            var valuesNode = nodes.OfType<ValuesNode>().LastOrDefault();
            var returningNode = nodes.OfType<ReturningNode>().LastOrDefault();
            var defaultValuesNode = nodes.OfType<DefaultValuesNode>().LastOrDefault();

            var conflictNode = nodes.OfType<OnConflictNode>().LastOrDefault();
            if (conflictNode != null)
            {
                throw new NotSupportedException(
                    "Oracle does not support ON CONFLICT syntax. " +
                    "Use Sql.Raw() with a MERGE INTO statement instead.");
            }

            if (valuesNode != null && valuesNode.ValuesSets.Count > 1)
            {
                CompileOracleInsertAll(insertNode, valuesNode, context);
                if (returningNode != null)
                {
                    throw new NotSupportedException(
                        "Oracle does not support RETURNING with multi-row INSERT ALL. " +
                        "Insert rows individually to use RETURNING.");
                }

                return;
            }

            if (insertNode != null)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"INSERT INTO {Escape(insertNode.TableName)} ");
                if (insertNode.Columns.Count > 0)
                {
                    context.Sql.Append("(");
                    context.Sql.Append(string.Join(", ", insertNode.Columns.Select(c => Escape(c))));
                    context.Sql.Append(") ");
                }
            }

            if (defaultValuesNode != null)
            {
                context.Sql.Append("/* Oracle: specify explicit DEFAULT values per column via VALUES () */ ");
            }
            else if (valuesNode != null)
            {
                valuesNode.Accept(visitor);
            }

            returningNode?.Accept(visitor);
        }
    }

    private void CompileOracleInsertAll(InsertNode? insertNode, ValuesNode valuesNode, CompilationContext context)
    {
        if (insertNode == null)
        {
            return;
        }

        context.Sql.Append("BEGIN ");

        var cols = insertNode.Columns.Count > 0
            ? "(" + string.Join(", ", insertNode.Columns.Select(c => Escape(c))) + ") "
            : "";

        foreach (var valueSet in valuesNode.ValuesSets)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"INSERT INTO {Escape(insertNode.TableName)} {cols}VALUES (");
            var paramNames = new List<string>();
            foreach (var val in valueSet)
            {
                paramNames.Add(context.Parameters.Add(val));
            }

            context.Sql.Append(string.Join(", ", paramNames));
            context.Sql.Append("); ");
        }

        context.Sql.Append("END; ");
    }

    internal override void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var updateNode = partition.UpdateNodes.OfType<UpdateNode>().LastOrDefault();
        if (updateNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"UPDATE {Escape(updateNode.TableName)} ");
        }

        var setNodes = partition.SetNodes;
        var tokenNodes = partition.ConcurrencyTokenNodes;

        if (setNodes.Count > 0 || tokenNodes.Count > 0)
        {
            context.Sql.Append("SET ");
            bool firstSet = true;
            for (int i = 0; i < setNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                setNodes[i].Accept(visitor);
                firstSet = false;
            }

            for (int i = 0; i < tokenNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                var token = tokenNodes[i];
                if (token.AutoIncrement && token.NewValue == null)
                {
                    context.Sql.Append(Escape(token.ColumnName))
                               .Append(" = ")
                               .Append(Escape(token.ColumnName))
                               .Append(" + 1");
                }
                else
                {
                    var newParamName = context.Parameters.Add(token.NewValue);
                    context.Sql.Append(Escape(token.ColumnName))
                               .Append(" = ")
                               .Append(newParamName);
                }
                firstSet = false;
            }
            context.Sql.Append(" ");
        }

        CompileWheres(partition.WhereNodes, visitor, context);

        bool hasWhere = partition.WhereNodes.Count > 0;
        for (int i = 0; i < tokenNodes.Count; i++)
        {
            var token = tokenNodes[i];
            context.Sql.Append(hasWhere ? "AND " : "WHERE ");
            hasWhere = true;
            var paramName = context.Parameters.Add(token.ExpectedValue);
            context.Sql.Append(Escape(token.ColumnName))
                       .Append(" = ")
                       .Append(paramName)
                       .Append(" ");
        }

        partition.ReturningNode?.Accept(visitor);
    }

    internal override void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var deleteNode = partition.DeleteNode;
        if (deleteNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"DELETE FROM {Escape(deleteNode.TableName)} ");
        }

        CompileWheres(partition.WhereNodes, visitor, context);
        partition.ReturningNode?.Accept(visitor);
    }

    internal override void CompileLimitOffset(LimitOffsetNode? limitNode, ISqlVisitor visitor, CompilationContext context)
    {
        if (limitNode == null || DialectVersion == OracleDialectVersion.Oracle11g)
        {
            return;
        }

        var limit = limitNode.Limit;
        var offset = limitNode.Offset;

        if (offset.HasValue)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"OFFSET {offset.Value} ROWS ");
        }

        if (limit.HasValue)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"FETCH NEXT {limit.Value} ROWS ONLY ");
        }
    }
}
