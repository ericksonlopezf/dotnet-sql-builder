// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.Metadata;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides the base implementation for a SQL compiler that translates AST nodes into executable SQL queries.
/// </summary>
/// <remarks>
/// LINQ-expression-based query methods (e.g., <c>.Where(x =&gt; ...)</c>) use reflection-based evaluation
/// and are not compatible with NativeAOT strict trimming. Use <c>Sql.Raw()</c> or raw string overloads for AOT paths.
/// </remarks>
[RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
[RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
public abstract class SqlCompilerBase : ISqlCompiler, ISqlRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlCompilerBase"/> class.
    /// </summary>
    protected SqlCompilerBase() { }

    /// <inheritdoc/>
    public virtual bool SupportsCapability(ProviderCapability capability) => false;

    /// <summary>
    /// Creates a new parameter manager for storing SQL parameters during compilation.
    /// </summary>
    /// <returns>An instance of <see cref="IParameterManager"/>.</returns>
    public virtual IParameterManager CreateParameterManager() => new ParameterManager();
    internal virtual SqlVisitorBase CreateVisitor(CompilationContext context) => new SqlCompilerVisitor(this, context);

    /// <summary>
    /// Compiles the specified query AST into an executable SQL string and parameters.
    /// </summary>
    /// <param name="query">The query to compile.</param>
    /// <returns>The compiled <see cref="SqlResult"/> containing the SQL text and parameters.</returns>
    public virtual SqlResult Compile(ISqlQuery query) => Compile(query, null);

    /// <summary>
    /// Compiles the specified query AST using an optional existing parameter manager.
    /// </summary>
    /// <param name="query">The query to compile.</param>
    /// <param name="existingParameters">An optional existing parameter manager to append parameters to.</param>
    /// <returns>The compiled <see cref="SqlResult"/> containing the SQL text and parameters.</returns>
    public virtual SqlResult Compile(ISqlQuery query, IParameterManager? existingParameters)
    {
        using var activity = SqlBuilderDiagnostics.ActivitySource.StartActivity("SqlCompiler.Compile");
        
        var isRoot = existingParameters == null;
        var parameters = existingParameters ?? CreateParameterManager();
        
        using var context = new CompilationContext(parameters);

        try
        {
            if (query is IAstQuery astQuery)
            {
                var nodes = astQuery.Nodes;
                bool isInsert = false, isUpdate = false, isDelete = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    var n = nodes[i];
                    if (n is InsertNode || n is InsertSelectNode)
                    {
                        isInsert = true;
                    }
                    else if (n is UpdateNode)
                    {
                        isUpdate = true;
                    }
                    else if (n is DeleteNode)
                    {
                        isDelete = true;
                    }
                }

                var visitor = CreateVisitor(context);

                if (isInsert)
                {
                    CompileInsert(nodes, visitor, context);
                }
                else if (isUpdate)
                {
                    CompileUpdate(nodes, visitor, context);
                }
                else if (isDelete)
                {
                    CompileDelete(nodes, visitor, context);
                }
                else
                {
                    CompileSelect(nodes, visitor, context);
                }

                activity?.SetTag("sqlbuilder.query_type", isInsert ? "INSERT" : isUpdate ? "UPDATE" : isDelete ? "DELETE" : "SELECT");
            }
            else if (query is RawQuery rawQuery)
            {
                context.Sql.Append(rawQuery.RawSql);
                if (rawQuery.Parameters != null)
                {
                    foreach(var kv in (IEnumerable<KeyValuePair<string, object?>>)rawQuery.Parameters)
                    {
                        parameters.AddNamed(kv.Key.TrimStart('@'), kv.Value);
                    }
                }
                activity?.SetTag("sqlbuilder.query_type", "RAW");
            }

            int len = context.Sql.Length;
            for (int i = len - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(context.Sql[i]))
                {
                    break;
                }
                len--;
            }
            var sqlStr = context.Sql.ToString(0, len);
            var result = new SqlResult(sqlStr, isRoot ? parameters.GetParameters() : new Dictionary<string, object?>());
            
            activity?.SetTag("db.statement", sqlStr);
            activity?.SetTag("sqlbuilder.parameter_count", result.Parameters.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new System.Diagnostics.ActivityEvent("Exception", tags: new System.Diagnostics.ActivityTagsCollection { { "exception.message", ex.Message }, { "exception.stacktrace", ex.StackTrace } }));
            throw;
        }
    }
    
    /// <summary>
    /// Escapes a SQL identifier, safely handling qualified names containing dots (e.g., 'schema.table').
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier string.</returns>
    public virtual string Escape(string identifier)
    {
        if (string.IsNullOrEmpty(identifier) || identifier == "*")
        {
            return identifier;
        }

        int firstDot = identifier.IndexOf('.');
        if (firstDot < 0)
        {
            return EscapeIdentifier(identifier);
        }

        var span = identifier.AsSpan();
        // Stryker disable once Arithmetic : Buffer initial capacity calculation
        var sb = new System.Text.StringBuilder(identifier.Length + 4);
        while (true)
        {
            int dotIndex = span.IndexOf('.');
            if (dotIndex < 0)
            {
                EscapeIdentifier(sb, span);
                break;
            }

            EscapeIdentifier(sb, span.Slice(0, dotIndex));
            sb.Append('.');
            span = span.Slice(dotIndex + 1);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a raw identifier span and appends it directly to the provided <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="sb">The string builder to append the escaped identifier to.</param>
    /// <param name="identifier">The raw identifier span.</param>
    public virtual void EscapeIdentifier(System.Text.StringBuilder sb, System.ReadOnlySpan<char> identifier)
    {
        sb.Append('"');
        sb.Append(identifier);
        sb.Append('"');
    }

    /// <summary>
    /// Escapes a raw SQL identifier string.
    /// </summary>
    /// <param name="identifier">The raw identifier to escape.</param>
    /// <returns>The escaped identifier string.</returns>
    public virtual string EscapeIdentifier(string identifier)
    {
        return "\"" + identifier + "\"";
    }

    /// <summary>
    /// Gets the AOT-compatible SQL renderer for generating bulk queries and non-AST based queries.
    /// </summary>
    protected abstract ISqlRenderer AotRenderer { get; }
    
    internal virtual void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var cteNodes = partition.CteNodes;
        var windowPageNode = partition.WindowPageNode;
        if (CompileBeforeSelect(partition, visitor, context))
        {
            return;
        }

        if (windowPageNode != null)
        {
            context.Sql.Append("WITH ");
            if (cteNodes.Any(c => c.IsRecursive))
            {
                context.Sql.Append("RECURSIVE ");
            }

            for (int i = 0; i < cteNodes.Count; i++)
            {
                if (i > 0)
                {
                    context.Sql.Append(", ");
                }

                cteNodes[i].Accept(visitor);
            }
            if (cteNodes.Count > 0)
            {
                context.Sql.Append(", ");
            }

            context.Sql.Append("__wp AS (");
        }
        else if (cteNodes.Count > 0)
        {
            context.Sql.Append("WITH ");
            if (cteNodes.Any(c => c.IsRecursive))
            {
                context.Sql.Append("RECURSIVE ");
            }

            for (int i = 0; i < cteNodes.Count; i++)
            {
                if (i > 0)
                {
                    context.Sql.Append(", ");
                }

                cteNodes[i].Accept(visitor);
            }
            context.Sql.Append(" ");
        }
        
        var selectNodes = partition.SelectNodes;
        if (selectNodes.Count > 0)
        {
            var subContext = new CompilationContext(context.Parameters);
            try
            {
                var subVisitor = CreateVisitor(subContext);
                selectNodes[selectNodes.Count - 1].Accept(subVisitor);
                
                if (subContext.Sql.Length >= 7 && subContext.Sql.ToString(0, 7) == "SELECT ")
                {
                    context.Sql.Append("SELECT ");
                    CompileDistinct(partition, visitor, context);
                    subContext.Sql.Remove(0, 7);
                }
                
                if (subContext.Sql.Length > 0 && subContext.Sql[subContext.Sql.Length - 1] == ' ') 
                {
                    subContext.Sql.Length--;
                }
                
                if (windowPageNode != null)
                {
                    subContext.Sql.Append(", ROW_NUMBER() OVER(ORDER BY ").Append(Escape(windowPageNode.OrderByColumn)).Append(windowPageNode.Descending ? " DESC) AS __row_num " : " ASC) AS __row_num ");
                }
                else
                {
                    subContext.Sql.Append(" ");
                }
                
                context.Sql.Append(subContext.Sql);
            }
            // Stryker disable once Block : SubContext resource disposal is unobservable functionally
            finally
            {
                // Stryker disable once Statement : SubContext resource disposal is unobservable functionally
                subContext.Dispose();
            }
        }
        else
        {
            context.Sql.Append("SELECT ");
            CompileDistinct(partition, visitor, context);
            if (windowPageNode != null)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"*, ROW_NUMBER() OVER(ORDER BY {Escape(windowPageNode.OrderByColumn)} {(windowPageNode.Descending ? "DESC" : "ASC")}) AS __row_num ");
            }
            else
            {
                context.Sql.Append("* ");
            }
        }
        
        CompileFrom(partition, visitor, context);
        
        foreach (var join in partition.JoinNodes)
        {
            join.Accept(visitor);
        }

        CompileWheres(partition.WhereNodes, visitor, context);
        CompileCompositeCursors(partition.CompositeCursorNodes, visitor, context, partition.WhereNodes.Count > 0);
        
        var groupByNodes = partition.GroupByNodes;
        if (groupByNodes.Count > 0)
        {
            context.Sql.Append("GROUP BY ");
            for (int i = 0; i < groupByNodes.Count; i++)
            {
                if (i > 0)
                {
                    context.Sql.Append(", ");
                }
                groupByNodes[i].Accept(visitor);
            }
            context.Sql.Append(" ");
        }
        
        var havingNodes = partition.HavingNodes;
        for (int i = 0; i < havingNodes.Count; i++)
        {
            if (i == 0)
            {
                context.Sql.Append("HAVING ");
            }
            else
            {
                var rh = havingNodes[i] as RawHavingNode;
                if (rh != null && rh.IsOr)
                {
                    context.Sql.Append("OR ");
                }
                else
                {
                    var eh = havingNodes[i] as ExpressionHavingNode;
                    if (eh != null && eh.IsOr)
                    {
                        context.Sql.Append("OR ");
                    }
                    else
                    {
                        context.Sql.Append("AND ");
                    }
                }
            }
            havingNodes[i].Accept(visitor);
        }
        
        var windowNodes = partition.WindowNodes;
        if (windowNodes.Count > 0)
        {
            context.Sql.Append("WINDOW ");
            for (int i = 0; i < windowNodes.Count; i++)
            {
                if (i > 0)
                {
                    context.Sql.Append(", ");
                }

                windowNodes[i].Accept(visitor);
            }
        }
        
        var setOpNodes = partition.SetOpNodes;
        foreach (var setOp in setOpNodes)
        {
            setOp.Accept(visitor);
        }

        CompileOrderBys(partition.OrderNodes, visitor, context);
        CompileLimitOffset(partition.LimitNode, visitor, context);
        
        if (windowPageNode != null)
        {
            context.Sql.Append(")"); // Close the __wp CTE
            int start = ((windowPageNode.PageNumber - 1) * windowPageNode.PageSize) + 1;
            int end = windowPageNode.PageNumber * windowPageNode.PageSize;
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $" SELECT * FROM __wp WHERE __row_num BETWEEN {start} AND {end}");
        }
        
        if (partition.QueryAliasNode != null)
        {
            partition.QueryAliasNode.Accept(visitor);
        }

        foreach (var node in partition.ExtensionNodes)
        {
            node.Accept(visitor);
        }
    }
    
    internal virtual bool CompileBeforeSelect(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context) => false;
    internal virtual void CompileDistinct(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context) { }
    internal virtual void CompileFrom(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context)
    {
        if (partition.FromNode != null)
        {
            partition.FromNode.Accept(visitor);
        }
    }
    
    /// <inheritdoc />
    public virtual SqlResult RenderInsert<T>(T entity, Span<bool> insertMask) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>
        => AotRenderer.RenderInsert(entity, insertMask);
    
    internal virtual void CompileInsert(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);

        // INSERT INTO ... SELECT ... takes priority over regular VALUES-based insert
        if (partition.InsertSelectNode != null)
        {
            partition.InsertSelectNode.Accept(visitor);
            return;
        }

        if (partition.InsertNode != null)
        {
            partition.InsertNode.Accept(visitor);
        }

        if (partition.DefaultValuesNode != null)
        {
            partition.DefaultValuesNode.Accept(visitor);
        }
        else if (partition.ValuesNode != null)
        {
            partition.ValuesNode.Accept(visitor);
        }

        if (partition.OnConflictNode != null)
        {
            partition.OnConflictNode.Accept(visitor);
        }

        if (partition.ReturningNode != null) 
        {
            if (context.Sql.Length > 0 && context.Sql[context.Sql.Length - 1] != ' ')
            {
                context.Sql.Append(" ");
            }

            partition.ReturningNode.Accept(visitor);
        }

        foreach (var node in partition.ExtensionNodes)
        {
            node.Accept(visitor);
        }
    }

    
    /// <inheritdoc />
    public virtual SqlResult RenderUpdate<T>(T entity, Span<bool> setMask, Span<bool> whereMask) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>
        => AotRenderer.RenderUpdate(entity, setMask, whereMask);
    internal virtual void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var cteNodes = partition.CteNodes;

        if (cteNodes.Count > 0)
        {
            context.Sql.Append("WITH ");
            if (cteNodes.Any(c => c.IsRecursive))
            {
                context.Sql.Append("RECURSIVE ");
            }

            for (int i = 0; i < cteNodes.Count; i++)
            {
                if (i > 0)
                {
                    context.Sql.Append(", ");
                }

                cteNodes[i].Accept(visitor);
            }
            context.Sql.Append(" ");
        }

        if (partition.UpdateNodes.Count > 0)
        {
            partition.UpdateNodes[partition.UpdateNodes.Count - 1].Accept(visitor);
        }

        var setNodes = partition.SetNodes;
        var tokenNodes = partition.ConcurrencyTokenNodes;

        var hasSetNodes = setNodes.Count > 0 || tokenNodes.Count > 0;
        if (hasSetNodes)
        {
            context.Sql.Append("SET ");
            bool firstSet = true;
            for (int i = 0; i < setNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                setNodes[i].Accept(visitor);
                firstSet = false;
            }
            // Concurrency tokens: add SET column = column + 1 (auto-increment) or SET column = @newValue
            for (int i = 0; i < tokenNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                var token = tokenNodes[i];
                if (token.AutoIncrement && token.NewValue == null)
                {
                    context.Sql.Append(EscapeIdentifier(token.ColumnName))
                               .Append(" = ")
                               .Append(EscapeIdentifier(token.ColumnName))
                               .Append(" + 1");
                }
                else
                {
                    var newParamName = context.Parameters.Add(token.NewValue);
                    context.Sql.Append(EscapeIdentifier(token.ColumnName))
                               .Append(" = ")
                               .Append(newParamName);
                }
                firstSet = false;
            }
            context.Sql.Append(" ");
        }
        
        if (partition.FromNode != null)
        {
            partition.FromNode.Accept(visitor);
        }

        foreach (var join in partition.JoinNodes)
        {
            join.Accept(visitor);
        }

        CompileWheres(partition.WhereNodes, visitor, context);

        // Concurrency tokens: AND column = @expectedValue (appended after regular WHERE)
        // Stryker disable once Equality : Guard clause
        if (tokenNodes.Count > 0)
        {
            bool hasExistingWhere = partition.WhereNodes.Count > 0;
            for (int i = 0; i < tokenNodes.Count; i++)
            {
                var token = tokenNodes[i];
                if (!hasExistingWhere)
                {
                    context.Sql.Append("WHERE ");
                    hasExistingWhere = true;
                }
                else
                {
                    context.Sql.Append("AND ");
                }
                var paramName = context.Parameters.Add(token.ExpectedValue);
                context.Sql.Append(EscapeIdentifier(token.ColumnName))
                           .Append(" = ")
                           .Append(paramName)
                           .Append(" ");
            }
        }

        if (partition.ReturningNode != null) 
        {
            if (context.Sql.Length > 0 && context.Sql[context.Sql.Length - 1] != ' ')
            {
                context.Sql.Append(" ");
            }

            partition.ReturningNode.Accept(visitor);
        }

        foreach (var node in partition.ExtensionNodes)
        {
            node.Accept(visitor);
        }
    }
    
    internal virtual void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        if (partition.DeleteNode != null)
        {
            partition.DeleteNode.Accept(visitor);
        }

        CompileWheres(partition.WhereNodes, visitor, context);
        if (partition.ReturningNode != null) 
        {
            if (context.Sql.Length > 0 && context.Sql[context.Sql.Length - 1] != ' ')
            {
                context.Sql.Append(" ");
            }

            partition.ReturningNode.Accept(visitor);
        }

        foreach (var node in partition.ExtensionNodes)
        {
            node.Accept(visitor);
        }
    }
    
    internal virtual void CompileWheres(List<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        CompileWheres((IReadOnlyList<ISqlNode>)nodes, visitor, context);
    }

    internal virtual void CompileWheres(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        bool isFirst = true;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (!(node is RawWhereNode || node is ExpressionWhereNode || node is EricksonLopez.SqlBuilder.Abstractions.Nodes.ExistsWhereNode))
            {
                continue;
            }

            if (isFirst)
            {
                context.Sql.Append("WHERE ");
                isFirst = false;
            }
            else
            {
                if (node is RawWhereNode rw && rw.IsOr)
                {
                    context.Sql.Append("OR ");
                }
                else if (node is ExpressionWhereNode ew && ew.IsOr)
                {
                    context.Sql.Append("OR ");
                }
                else if (node is EricksonLopez.SqlBuilder.Abstractions.Nodes.ExistsWhereNode exw && exw.IsOr)
                {
                    context.Sql.Append("OR ");
                }
                else
                {
                    context.Sql.Append("AND ");
                }
            }

            node.Accept(visitor);
        }
    }

    internal virtual void CompileCompositeCursors(IReadOnlyList<EricksonLopez.SqlBuilder.Abstractions.Nodes.CompositeCursorNode> cursors, ISqlVisitor visitor, CompilationContext context, bool hasExistingWhere)
    {
        for (int i = 0; i < cursors.Count; i++)
        {
            if (!hasExistingWhere)
            {
                context.Sql.Append("WHERE ");
                hasExistingWhere = true;
            }
            else
            {
                context.Sql.Append("AND ");
            }
            cursors[i].Accept(visitor);
            context.Sql.Append(" ");
        }
    }
    
    internal virtual void CompileOrderBys(List<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        CompileOrderBys((IReadOnlyList<ISqlNode>)nodes, visitor, context);
    }

    internal virtual void CompileOrderBys(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        bool isFirst = true;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (!(node is OrderByNode || node is ThenByNode || node is RawOrderByNode))
            {
                continue;
            }

            if (isFirst)
            {
                context.Sql.Append("ORDER BY ");
                isFirst = false;
            }
            else
            {
                context.Sql.Append(", ");
            }
            node.Accept(visitor);
        }

        if (!isFirst)
        {
            context.Sql.Append(" ");
        }
    }

    internal virtual void CompileLimitOffset(LimitOffsetNode? limitNode, ISqlVisitor visitor, CompilationContext context)
    {
        if (limitNode != null)
        {
            if (limitNode.Limit.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"LIMIT {limitNode.Limit.Value} ");
            }

            if (limitNode.Offset.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"OFFSET {limitNode.Offset.Value} ");
            }
        }
    }

    /// <inheritdoc />
    public virtual EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T> => AotRenderer.RenderBulkInsert(entities, rules, batchSize);

    /// <inheritdoc />
    public virtual EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T> => AotRenderer.RenderBulkUpdate(entities, rules, batchSize);

    /// <inheritdoc />
    public virtual EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T> => AotRenderer.RenderBulkMerge(entities, rules, batchSize);

    /// <inheritdoc />
    public virtual EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T> => AotRenderer.RenderBulkUpsert(entities, rules, batchSize);

    /// <inheritdoc />
    public virtual EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T> => AotRenderer.RenderBulkInsertIgnore(entities, rules, batchSize);
}













