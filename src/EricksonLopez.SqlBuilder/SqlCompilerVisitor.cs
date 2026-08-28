// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

[RequiresDynamicCode("SQL expression visitors use dynamic code when evaluating LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
[RequiresUnreferencedCode("SQL expression visitors access member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
internal class SqlCompilerVisitor : SqlVisitorBase
{
    /// <summary>
    /// Gets the SQL compiler used by this visitor.
    /// </summary>
    public ISqlCompiler Compiler { get; }
    
    /// <summary>
    /// Gets the compilation context managing the SQL string builder and parameters.
    /// </summary>
    public CompilationContext Context { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCompilerVisitor"/> class.
    /// </summary>
    /// <param name="compiler">The SQL compiler.</param>
    /// <param name="context">The compilation context.</param>
    public SqlCompilerVisitor(ISqlCompiler compiler, CompilationContext context)
    {
        Compiler = compiler;
        Context = context;
    }

    /// <inheritdoc/>
    public override void VisitUnknown(ISqlNode node) => throw new NotSupportedException($"Unknown node type: {node.GetType().Name}");

    /// <summary>
    /// Escapes the specified SQL identifier.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier.</returns>
    protected virtual string Escape(string identifier) => Compiler.Escape(identifier);

    /// <summary>
    /// Escapes a raw SQL identifier string.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier.</returns>
    protected virtual string EscapeIdentifier(string identifier) => Compiler.EscapeIdentifier(identifier);

    /// <summary>
    /// Appends a raw SQL condition and its parameters to the output.
    /// </summary>
    /// <param name="condition">The raw SQL condition.</param>
    /// <param name="parameters">The parameters to include.</param>
    protected void AppendRaw(string condition, object?[]? parameters)
    {
        if (parameters != null)
        {
            var mapped = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                mapped[i] = Context.Parameters.Add(parameters[i]);
            }

            Context.Sql.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, condition, mapped);
        }
        else
        {
            Context.Sql.Append(condition);
        }
    }

    /// <inheritdoc />
    public override void Visit(SelectNode node)
    {
        Context.Sql.Append("SELECT ");
        if (node.IsDistinct)
        {
            Context.Sql.Append("DISTINCT ");
        }

        if (node.Columns.Length == 0)
        {
            Context.Sql.Append("*");
        }
        else
        {
            for (int i = 0; i < node.Columns.Length; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Escape(node.Columns[i]));
            }
        }
        
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(ExpressionSelectNode node)
    {
        Context.Sql.Append("SELECT ");
        if (node.IsDistinct)
        {
            Context.Sql.Append("DISTINCT ");
        }

        var lambda = node.Selector as System.Linq.Expressions.LambdaExpression;
        if (lambda != null)
        {
            var newExpr = lambda.Body as System.Linq.Expressions.NewExpression;
            var memExpr = lambda.Body as System.Linq.Expressions.MemberExpression;
            if (newExpr != null)
            {
                var cols = newExpr.Members?.Select(m => SqlNamingHelper.ToSnakeCase(m.Name)) ?? Array.Empty<string>();
                Context.Sql.Append(cols.Any() ? string.Join(", ", cols) : "*");
            }
            else if (memExpr != null)
            {
                var snake = SqlNamingHelper.ToSnakeCase(memExpr.Member.Name);
                Context.Sql.Append(snake);
            }
            else
            {
                Context.Sql.Append("*");
            }
        }
        else
        {
            Context.Sql.Append("*");
        }
    }

    /// <inheritdoc />
    public override void Visit(RawSelectNode node)
    {
        Context.Sql.Append("SELECT ");
        if (node.IsDistinct)
        {
            Context.Sql.Append("DISTINCT ");
        }

        AppendRaw(node.RawSql, node.Parameters);
    }

    /// <inheritdoc />
    public override void Visit(ScalarSubquerySelectNode node)
    {
        Context.Sql.Append("SELECT (");
        var subResult = Compiler.Compile(node.Subquery, Context.Parameters);
        Context.Sql.Append(subResult.Sql.TrimEnd());
        Context.Sql.Append(") AS ");
        Context.Sql.Append(Escape(node.Alias));
    }

    /// <inheritdoc />
    public override void Visit(FromNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"FROM {Escape(node.TableName)} ");
        if (!string.IsNullOrEmpty(node.Alias))
        {
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"AS {Escape(node.Alias!)} ");
        }
    }

    /// <inheritdoc />
    public override void Visit(JoinNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{GetJoinTypeString(node.Type)} JOIN {Escape(node.TableName)} ");
        if (!string.IsNullOrEmpty(node.Alias))
        {
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"AS {Escape(node.Alias!)} ");
        }

        if (!string.IsNullOrEmpty(node.RawCondition))
        {
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"ON {node.RawCondition} ");
        }
        else if (node.ExpressionCondition != null)
        {
            Context.Sql.Append("ON ");
            var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
            parser.Parse(node.ExpressionCondition);
            Context.Sql.Append(" ");
        }
    }

    /// <inheritdoc />
    public override void Visit(ExpressionWhereNode node)
    {
        var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
        parser.Parse(node.Expression);
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(RawWhereNode node)
    {
        AppendRaw(node.Condition, node.Parameters);
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(EricksonLopez.SqlBuilder.Abstractions.Nodes.ExistsWhereNode node)
    {
        Context.Sql.Append(node.IsNot ? "NOT EXISTS (" : "EXISTS (");
        var subResult = Compiler.Compile(node.Subquery, Context.Parameters);
        Context.Sql.Append(subResult.Sql.TrimEnd());
        Context.Sql.Append(") ");
    }

    /// <inheritdoc />
    public override void Visit(ExpressionHavingNode node)
    {
        var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
        parser.Parse(node.Expression);
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(RawHavingNode node)
    {
        AppendRaw(node.Condition, node.Parameters);
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(OrderByNode node)
    {
        var lambdaOrder = node.KeySelector as System.Linq.Expressions.LambdaExpression;
        if (lambdaOrder != null)
        {
            var member = lambdaOrder.Body is System.Linq.Expressions.UnaryExpression u 
                ? u.Operand as System.Linq.Expressions.MemberExpression 
                : lambdaOrder.Body as System.Linq.Expressions.MemberExpression;
            if (member != null)
            {
                var snakeOrder = SqlNamingHelper.ToSnakeCase(member.Member.Name);
                Context.Sql.Append(Escape(snakeOrder));
            }
        }
        if (node.IsDescending)
        {
            Context.Sql.Append(" DESC");
        }
        AppendNullsPosition(node.Nulls);
    }

    /// <summary>
    /// Appends the NULLS FIRST / NULLS LAST clause to the current SQL output.
    /// Override in dialect-specific compilers that need different behavior (e.g., SQL Server emulation).
    /// </summary>
    protected virtual void AppendNullsPosition(EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition nulls)
    {
        if (nulls == EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.First)
        {
            Context.Sql.Append(" NULLS FIRST");
        }
        else if (nulls == EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.Last)
        {
            Context.Sql.Append(" NULLS LAST");
        }
    }

    /// <inheritdoc />
    public override void Visit(RawOrderByNode node)
    {
        AppendRaw(node.Condition, node.Parameters);
        if (node.IsDescending)
        {
            Context.Sql.Append(" DESC");
        }
    }

    /// <inheritdoc />
    public override void Visit(LimitOffsetNode node)
    {
    }

    /// <inheritdoc />
    public override void Visit(InsertNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"INSERT INTO {Escape(node.TableName)} ");
        if (node.Columns.Count > 0)
        {
            Context.Sql.Append("(");
            for (int i = 0; i < node.Columns.Count; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Escape(node.Columns[i]));
            }
            Context.Sql.Append(") ");
        }
    }

    /// <inheritdoc />
    public override void Visit(ValuesNode node)
    {
        Context.Sql.Append("VALUES ");
        for (int i = 0; i < node.ValuesSets.Count; i++)
        {
            if (i > 0)
            {
                Context.Sql.Append(", ");
            }

            Context.Sql.Append("(");
            var valuesSet = node.ValuesSets[i];
            for (int j = 0; j < valuesSet.Count; j++)
            {
                if (j > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Context.Parameters.Add(valuesSet[j]));
            }
            Context.Sql.Append(")");
        }
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(DefaultValuesNode node)
    {
        Context.Sql.Append("DEFAULT VALUES ");
    }

    /// <inheritdoc />
    public override void Visit(SubqueryFromNode node)
    {
        Context.Sql.Append("FROM (");
        var subResult = Compiler.Compile(node.Query, Context.Parameters);
        Context.Sql.Append(subResult.Sql);
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $") AS {Escape(node.Alias)} ");
    }

    /// <inheritdoc />
    public override void Visit(UnnestNode node)
    {
        Context.Sql.Append("FROM UNNEST(");
        for (int i = 0; i < node.Arrays.Length; i++)
        {
            if (i > 0)
            {
                Context.Sql.Append(", ");
            }

            Context.Sql.Append(Context.Parameters.Add(node.Arrays[i]));
        }
        Context.Sql.Append(") AS ").Append(Escape(node.Alias)).Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(QueryAliasNode node)
    {
        if (!string.IsNullOrEmpty(node.Alias))
        {
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"AS {Escape(node.Alias)} ");
        }
    }

    /// <inheritdoc />
    public override void Visit(RawJoinNode node)
    {
        AppendRaw(node.JoinSql, node.Parameters);
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(SubqueryJoinNode node)
    {
        var joinType = node.Type;
        bool isApply = joinType == EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.CrossApply
                    || joinType == EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.OuterApply;

        if (isApply)
        {
            // CROSS APPLY (subquery) AS alias
            // OUTER APPLY (subquery) AS alias
            // No "JOIN" keyword — these are SQL Server APPLY operators
            string applyKeyword = joinType == EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.CrossApply
                ? "CROSS APPLY"
                : "OUTER APPLY";
            Context.Sql.Append(applyKeyword).Append(" (");
        }
        else if (node.IsLateral)
        {
            string joinStr = GetJoinTypeString(node.Type);
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{joinStr} JOIN LATERAL (");
        }
        else
        {
            string joinStr = GetJoinTypeString(node.Type);
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{joinStr} JOIN (");
        }

        var sjResult = Compiler.Compile(node.Subquery, Context.Parameters);
        Context.Sql.Append(sjResult.Sql);
        Context.Sql.Append(") AS ").Append(Escape(node.Alias)).Append(" ");
        if (!string.IsNullOrEmpty(node.OnCondition))
        {
            Context.Sql.Append("ON ").Append(node.OnCondition).Append(" ");
        }
        else if (node.ExpressionCondition != null)
        {
            Context.Sql.Append("ON ");
            var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
            parser.Parse(node.ExpressionCondition);
            Context.Sql.Append(" ");
        }
    }

    /// <inheritdoc />
    public override void Visit(OnConflictNode node)
    {
        Context.Sql.Append("ON CONFLICT ");
        if (node.TargetColumns != null && node.TargetColumns.Length > 0)
        {
            Context.Sql.Append("(");
            for (int i = 0; i < node.TargetColumns.Length; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Escape(node.TargetColumns[i]));
            }
            Context.Sql.Append(") ");
        }
        if (!string.IsNullOrEmpty(node.UpdateAction))
        {
            AppendRaw(node.UpdateAction!, node.Parameters);
            Context.Sql.Append(" ");
            if (node.UpdateExpression != null)
            {
                var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
                parser.Parse(node.UpdateExpression);
                Context.Sql.Append(" ");
            }
        }
    }

    /// <inheritdoc />
    public override void Visit(UpdateNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"UPDATE {Escape(node.TableName)} ");
    }

    /// <inheritdoc />
    public override void Visit(SetNode node)
    {
        if (!string.IsNullOrEmpty(node.RawExpression))
        {
            AppendRaw(node.RawExpression!, node.Parameters);
        }
        else
        {
            Context.Sql.Append(Escape(node.Column!)).Append(" = ").Append(Context.Parameters.Add(node.Value));
        }
    }

    /// <inheritdoc />
    public override void Visit(DeleteNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"DELETE FROM {Escape(node.TableName)} ");
    }

    /// <summary>
    /// ConcurrencyTokenNode is handled directly by <see cref="SqlCompilerBase.CompileUpdate"/> 
    /// and does not need visitor emission here. This override satisfies the interface contract.
    /// </summary>
    public override void Visit(ConcurrencyTokenNode node)
    {
        // Intentionally empty — processed by CompileUpdate in SqlCompilerBase
    }

    /// <inheritdoc />
    public override void Visit(ReturningNode node)
    {
        Context.Sql.Append("RETURNING ");
        if (node.Columns.Length == 0)
        {
            Context.Sql.Append("*");
        }
        else
        {
            for (int i = 0; i < node.Columns.Length; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Escape(node.Columns[i]));
            }
        }
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(CteNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Escape(node.Name)} AS (");
        var cteResult = Compiler.Compile(node.Query, Context.Parameters);
        Context.Sql.Append(cteResult.Sql);
        Context.Sql.Append(")");
    }

    /// <inheritdoc />
    public override void Visit(WindowNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Escape(node.Name)} AS (");
        
        if (node.PartitionBy != null && node.PartitionBy.Length > 0)
        {
            Context.Sql.Append("PARTITION BY ");
            for (int i = 0; i < node.PartitionBy.Length; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Escape(node.PartitionBy[i]));
            }
            Context.Sql.Append(" ");
        }

        if (node.OrderBy != null && node.OrderBy.Length > 0)
        {
            Context.Sql.Append("ORDER BY ");
            for (int i = 0; i < node.OrderBy.Length; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                var parts = node.OrderBy[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Context.Sql.Append(Escape(parts[0]));
                for (int j = 1; j < parts.Length; j++)
                {
                    Context.Sql.Append(" ").Append(parts[j].ToUpperInvariant());
                }
            }
        }
        
        Context.Sql.Append(")");
    }

    /// <summary>
    /// Emits: FUNC([column]) OVER (PARTITION BY col1, col2 ORDER BY col3 DESC) AS alias
    /// </summary>
    public override void Visit(WindowFunctionNode node)
    {
        // Function call: e.g. RANK() or SUM(amount) or LAG(amount, 1)
        Context.Sql.Append(node.FunctionName).Append("(");
        if (!string.IsNullOrEmpty(node.ColumnName))
        {
            // NTILE has its bucket count stored in ColumnName
            if (node.FunctionName == "NTILE")
                Context.Sql.Append(node.ColumnName);
            else
                Context.Sql.Append(Escape(node.ColumnName!));

            if (node.Offset.HasValue)
            {
                Context.Sql.Append(", ")
                           .Append(node.Offset.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (node.DefaultValue != null)
                {
                    Context.Sql.Append(", ").Append(Context.Parameters.Add(node.DefaultValue));
                }
            }
        }
        else if (node.FunctionName == "COUNT")
        {
            Context.Sql.Append("*");
        }
        Context.Sql.Append(")");

        if (node.FilterExpression != null)
        {
            Context.Sql.Append(" FILTER (WHERE ");
            var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
            parser.Parse(node.FilterExpression);
            Context.Sql.Append(")");
        }
        else if (!string.IsNullOrEmpty(node.FilterRaw))
        {
            Context.Sql.Append(" FILTER (WHERE ");
            AppendRaw(node.FilterRaw, node.FilterRawArgs);
            Context.Sql.Append(")");
        }

        Context.Sql.Append(" OVER (");

        // PARTITION BY
        if (node.PartitionByColumns.Length > 0)
        {
            Context.Sql.Append("PARTITION BY ");
            for (int i = 0; i < node.PartitionByColumns.Length; i++)
            {
                if (i > 0) Context.Sql.Append(", ");
                Context.Sql.Append(Escape(node.PartitionByColumns[i]));
            }
            if (node.OrderByColumns.Length > 0) Context.Sql.Append(" ");
        }

        // ORDER BY
        if (node.OrderByColumns.Length > 0)
        {
            Context.Sql.Append("ORDER BY ");
            for (int i = 0; i < node.OrderByColumns.Length; i++)
            {
                if (i > 0) Context.Sql.Append(", ");
                Context.Sql.Append(Escape(node.OrderByColumns[i]));
                if (node.OrderByDescending[i]) Context.Sql.Append(" DESC");
            }
        }

        Context.Sql.Append(") AS ").Append(Escape(node.Alias));
    }

    /// <inheritdoc />
    public override void Visit(SetOperationNode node)
    {
        Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{node.Operation} ");
        var opResult = Compiler.Compile(node.Query, Context.Parameters);
        Context.Sql.Append(opResult.Sql).Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(ThenByNode node) { Visit((OrderByNode)node); }
    
    /// <inheritdoc />
    public override void Visit(GroupByNode node) 
    {
        switch (node.Type)
        {
            case EricksonLopez.SqlBuilder.Abstractions.Nodes.GroupByType.Rollup:
                Context.Sql.Append("ROLLUP(");
                AppendGroupByColumns(node.Columns);
                Context.Sql.Append(")");
                break;
            case EricksonLopez.SqlBuilder.Abstractions.Nodes.GroupByType.Cube:
                Context.Sql.Append("CUBE(");
                AppendGroupByColumns(node.Columns);
                Context.Sql.Append(")");
                break;
            case EricksonLopez.SqlBuilder.Abstractions.Nodes.GroupByType.GroupingSets:
                Context.Sql.Append("GROUPING SETS (");
                if (node.Sets != null)
                {
                    for (int s = 0; s < node.Sets.Count; s++)
                    {
                        if (s > 0) Context.Sql.Append(", ");
                        Context.Sql.Append("(");
                        AppendGroupByColumns(node.Sets[s]);
                        Context.Sql.Append(")");
                    }
                }
                Context.Sql.Append(")");
                break;
            default:
                AppendGroupByColumns(node.Columns);
                break;
        }
    }

    private void AppendGroupByColumns(IReadOnlyList<string>? cols)
    {
        if (cols != null)
        {
            for (int i = 0; i < cols.Count; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append(Escape(cols[i]));
            }
        }
    }

    private static string GetJoinTypeString(EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType type) => type switch
    {
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Inner => "INNER",
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Left => "LEFT",
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Right => "RIGHT",
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Cross => "CROSS",
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Full => "FULL",
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.CrossApply => "CROSS APPLY",
        EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.OuterApply => "OUTER APPLY",
        _ => type.ToString().ToUpperInvariant()
    };

    /// <inheritdoc />
    public override void Visit(CaseNode node)
    {
        Context.Sql.Append("CASE");

        for (int i = 0; i < node.Branches.Length; i++)
        {
            var branch = node.Branches[i];
            Context.Sql.Append(" WHEN ");
            AppendRaw(branch.WhenSql, branch.WhenParameters);
            Context.Sql.Append(" THEN ");
            AppendRaw(branch.ThenSql, branch.ThenParameters);
        }

        if (node.ElseSql != null)
        {
            Context.Sql.Append(" ELSE ");
            AppendRaw(node.ElseSql, node.ElseParameters);
        }

        Context.Sql.Append(" END");

        if (!string.IsNullOrEmpty(node.Alias))
        {
            Context.Sql.Append(" AS ").Append(Escape(node.Alias));
        }
    }

    /// <inheritdoc />
    public override void Visit(InsertSelectNode node)
    {
        Context.Sql.Append("INSERT INTO ").Append(Escape(node.TableName)).Append(" ");
        if (node.Columns != null && node.Columns.Length > 0)
        {
            Context.Sql.Append("(");
            for (int i = 0; i < node.Columns.Length; i++)
            {
                if (i > 0) Context.Sql.Append(", ");
                Context.Sql.Append(Escape(node.Columns[i]));
            }
            Context.Sql.Append(") ");
        }
        var subResult = Compiler.Compile(node.SelectQuery, Context.Parameters);
        Context.Sql.Append(subResult.Sql);
    }

    /// <inheritdoc />
    public override void Visit(CompositeCursorNode node)
    {
        if (node.Keys == null || node.Keys.Length == 0) return;

        // Generate nested cursor predicate:
        // For 2 ascending keys: (col1 > @p0 OR (col1 = @p0 AND col2 > @p1))
        // For descending keys: replace > with <
        // For IsAfter=false (backward): reverse comparisons
        bool isAfter = node.IsAfter;

        Context.Sql.Append("(");
        AppendCursorPredicate(node.Keys, 0, isAfter);
        Context.Sql.Append(")");
    }

    private void AppendCursorPredicate(CursorKey[] keys, int startIndex, bool isAfter)
    {
        // Stryker disable once Equality,Statement : Guard clause
        if (startIndex >= keys.Length) return;

        var key = keys[startIndex];
        var colEscaped = Escape(key.ColumnName);
        var pName = Context.Parameters.Add(key.Value);

        // Determine comparison operator:
        // Ascending + isAfter → > (greater than anchor)
        // Ascending + !isAfter → < (less than anchor)
        // Descending + isAfter → < (smaller key = later in DESC order)
        // Descending + !isAfter → > (larger key = earlier in DESC order)
        string cmpOp = (key.IsDescending == isAfter) ? " < " : " > ";

        bool hasMore = startIndex + 1 < keys.Length;

        if (hasMore)
        {
            // col > @p0 OR (col = @p0 AND <recursive>)
            Context.Sql.Append(colEscaped).Append(cmpOp).Append(pName);
            Context.Sql.Append(" OR (");
            Context.Sql.Append(colEscaped).Append(" = ").Append(pName);
            Context.Sql.Append(" AND ");
            AppendCursorPredicate(keys, startIndex + 1, isAfter);
            Context.Sql.Append(")");
        }
        else
        {
            // Last key: just the comparison
            Context.Sql.Append(colEscaped).Append(cmpOp).Append(pName);
        }
    }
}









