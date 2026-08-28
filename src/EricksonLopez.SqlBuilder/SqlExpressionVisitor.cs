// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides an expression visitor that traverses LINQ expression trees and translates them into SQL string representations.
/// </summary>
/// <remarks>
/// This visitor handles common evaluation scenarios required for WHERE and HAVING clauses.
/// It uses reflection-based caching internally for property access and method invocations.
/// Use <c>Sql.Raw(FormattableString)</c> for strict NativeAOT scenarios to avoid IL2026 warnings.
/// </remarks>
[RequiresDynamicCode("SQL expression compilation uses dynamic code generation. Use Sql.Raw() for NativeAOT strict paths.")]
[RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed by the linker. Use Sql.Raw() for NativeAOT strict paths.")]
public class SqlExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _sql;
    private readonly IParameterManager _parameters;
    private readonly Func<string, string>? _escapeFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExpressionVisitor"/> class.
    /// </summary>
    /// <param name="sql">The string builder where the resulting SQL will be appended.</param>
    /// <param name="parameters">The parameter manager for tracking evaluated expression variables.</param>
    /// <param name="escapeFunc">An optional function used to escape SQL identifiers.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    public SqlExpressionVisitor(StringBuilder sql, IParameterManager parameters, Func<string, string>? escapeFunc = null)
    {
        _sql = sql ?? throw new ArgumentNullException(nameof(sql));
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _escapeFunc = escapeFunc;
    }

    /// <summary>
    /// Parses the specified expression and appends its translated SQL equivalent to the string builder.
    /// </summary>
    /// <param name="expression">The LINQ expression to parse.</param>
    public void Parse(Expression expression)
    {
        Visit(expression);
    }

    /// <summary>
    /// Dispatches the expression to one of the more specialized visit methods in this class.
    /// </summary>
    /// <param name="node">The expression to visit.</param>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <exception cref="NotSupportedException">The expression type is not supported</exception>
    public override Expression? Visit(Expression? node)
    {
        if (node == null)
        {
            return null;
        }

        if (node is BinaryExpression || node is UnaryExpression)
        {
            return base.Visit(node);
        }

        switch (node.NodeType)
        {
            case ExpressionType.Lambda:
            case ExpressionType.MemberAccess:
            case ExpressionType.Constant:
            case ExpressionType.Call:
                return base.Visit(node);
            default:
                throw new NotSupportedException($"Expression of type {node.NodeType} is not supported.");
        }
    }

    /// <inheritdoc />
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Visit(node.Body);
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
        {
            Visit(node.Operand);
            return node;
        }
        if (node.NodeType == ExpressionType.Not)
        {
            _sql.Append("NOT (");
            Visit(node.Operand);
            _sql.Append(")");
            return node;
        }
        if (node.NodeType == ExpressionType.Negate || node.NodeType == ExpressionType.NegateChecked)
        {
            _sql.Append("-(");
            Visit(node.Operand);
            _sql.Append(")");
            return node;
        }
        
        throw new NotSupportedException($"Expression of type {node.NodeType} is not supported.");
    }

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
        {
            bool leftIsNull = IsNullConstantOrExpression(node.Left);
            bool rightIsNull = IsNullConstantOrExpression(node.Right);

            if (leftIsNull || rightIsNull)
            {
                var nonNullOperand = leftIsNull ? node.Right : node.Left;
                _sql.Append("(");
                Visit(nonNullOperand);
                _sql.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
                _sql.Append(")");
                return node;
            }
        }

        _sql.Append("(");
        Visit(node.Left);
        
        _sql.Append(GetSqlOperator(node.NodeType));
        
        Visit(node.Right);
        _sql.Append(")");
        
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
        {
            var snakeCase = SqlNamingHelper.ToSnakeCase(node.Member.Name);
            if (_escapeFunc != null)
            {
                _sql.Append(_escapeFunc(snakeCase));
            }
            else
            {
                _sql.Append(snakeCase);
            }

            return node;
        }
        else
        {
            var val = Evaluate(node);
            if (val == null)
            {
                _sql.Append("NULL");
            }
            else
            {
                _sql.Append(_parameters.Add(val));
            }

            return node;
        }
    }

    /// <inheritdoc />
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value == null)
        {
            _sql.Append("NULL");
        }
        else
        {
            var p = _parameters.Add(node.Value);
            _sql.Append(p);
        }
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "ILike":
                if (HandleILike(node)) return node;
                break;
            case "Any":
                if (HandleAny(node)) return node;
                break;
            case "All":
                if (HandleAll(node)) return node;
                break;
            case "Between":
                if (HandleBetween(node)) return node;
                break;
            case "Contains":
                if (HandleContains(node)) return node;
                break;
            case "StartsWith":
                if (HandleStartsWith(node)) return node;
                break;
            case "EndsWith":
                if (HandleEndsWith(node)) return node;
                break;
            case "Coalesce":
                if (HandleCoalesce(node)) return node;
                break;
            case "NullIf":
                if (HandleNullIf(node)) return node;
                break;
            case "IsDistinctFrom":
                if (HandleIsDistinctFrom(node)) return node;
                break;
            case "IsNotDistinctFrom":
                if (HandleIsNotDistinctFrom(node)) return node;
                break;
            case "Outer":
                if (HandleOuter(node)) return node;
                break;
        }

        var val = Evaluate(node);
        if (val == null)
        {
            _sql.Append("NULL");
        }
        else
        {
            _sql.Append(_parameters.Add(val));
        }

        return node;
    }

    private bool HandleILike(MethodCallExpression node)
    {
        var declType = node.Method.DeclaringType;
        if (declType == null || (declType != typeof(Sql) && declType.Name != "PgSql")) return false;
        Visit(node.Arguments[0]);
        _sql.Append(" ILIKE ");
        var val = Evaluate(node.Arguments[1]);
        _sql.Append(_parameters.Add(val));
        return true;
    }

    private bool HandleAny(MethodCallExpression node)
    {
        var declType = node.Method.DeclaringType;
        if (declType == null || (declType != typeof(Sql) && declType.Name != "PgSql")) return false;
        Visit(node.Arguments[0]);
        _sql.Append(" = ANY(");
        var val = Evaluate(node.Arguments[1]);
        _sql.Append(_parameters.Add(val)).Append(")");
        return true;
    }

    private bool HandleAll(MethodCallExpression node)
    {
        var declType = node.Method.DeclaringType;
        if (declType == null || (declType != typeof(Sql) && declType.Name != "PgSql")) return false;
        Visit(node.Arguments[0]);
        _sql.Append(" = ALL(");
        var val = Evaluate(node.Arguments[1]);
        _sql.Append(_parameters.Add(val)).Append(")");
        return true;
    }

    /// <summary>
    /// Handles <c>value.Between(from, to)</c> extension method calls.
    /// Emits: <c>column BETWEEN @p0 AND @p1</c>
    /// </summary>
    private bool HandleBetween(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Sql)) return false;
        // node.Arguments[0] = the column (value)
        // node.Arguments[1] = from
        // node.Arguments[2] = to
        Visit(node.Arguments[0]);
        _sql.Append(" BETWEEN ");
        var fromVal = Evaluate(node.Arguments[1]);
        _sql.Append(_parameters.Add(fromVal));
        _sql.Append(" AND ");
        var toVal = Evaluate(node.Arguments[2]);
        _sql.Append(_parameters.Add(toVal));
        return true;
    }

    /// <summary>
    /// Handles <c>value.Coalesce(fallback)</c> or <c>Sql.Coalesce(v1, v2, fallback)</c> calls.
    /// Emits: <c>COALESCE(c1, @p0)</c> or <c>COALESCE(c1, c2, @p0)</c>
    /// </summary>
    private bool HandleCoalesce(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Sql)) return false;
        _sql.Append("COALESCE(");
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            if (i > 0) _sql.Append(", ");
            var arg = node.Arguments[i];
            if (arg is MemberExpression or ParameterExpression)
            {
                Visit(arg);
            }
            else
            {
                var val = Evaluate(arg);
                if (val == null)
                    _sql.Append("NULL");
                else
                    _sql.Append(_parameters.Add(val));
            }
        }
        _sql.Append(")");
        return true;
    }

    private bool HandleNullIf(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Sql)) return false;
        _sql.Append("NULLIF(");
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            if (i > 0) _sql.Append(", ");
            var arg = node.Arguments[i];
            if (arg is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
            {
                arg = u.Operand;
            }
            if (arg is MemberExpression or ParameterExpression)
            {
                Visit(arg);
            }
            else
            {
                var val = Evaluate(arg);
                if (val == null)
                    _sql.Append("NULL");
                else
                    _sql.Append(_parameters.Add(val));
            }
        }
        _sql.Append(")");
        return true;
    }

    private bool HandleIsDistinctFrom(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Sql)) return false;
        Visit(node.Arguments[0]);
        _sql.Append(" IS DISTINCT FROM ");
        var arg1 = node.Arguments[1];
        if (arg1 is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
        {
            arg1 = u.Operand;
        }
        if (arg1 is MemberExpression or ParameterExpression)
        {
            Visit(arg1);
        }
        else
        {
            var val = Evaluate(arg1);
            if (val == null)
                _sql.Append("NULL");
            else
                _sql.Append(_parameters.Add(val));
        }
        return true;
    }

    private bool HandleIsNotDistinctFrom(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Sql)) return false;
        Visit(node.Arguments[0]);
        _sql.Append(" IS NOT DISTINCT FROM ");
        var arg1 = node.Arguments[1];
        if (arg1 is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
        {
            arg1 = u.Operand;
        }
        if (arg1 is MemberExpression or ParameterExpression)
        {
            Visit(arg1);
        }
        else
        {
            var val = Evaluate(arg1);
            if (val == null)
                _sql.Append("NULL");
            else
                _sql.Append(_parameters.Add(val));
        }
        return true;
    }

    private bool HandleOuter(MethodCallExpression node)
    {
        if (node.Method.DeclaringType != typeof(Sql)) return false;
        var lambda = node.Arguments[0] is UnaryExpression un 
            ? un.Operand as LambdaExpression 
            : node.Arguments[0] as LambdaExpression;

        if (lambda != null)
        {
            var member = lambda.Body is UnaryExpression ub 
                ? ub.Operand as MemberExpression 
                : lambda.Body as MemberExpression;

            if (member != null)
            {
                var colName = SqlNamingHelper.ToSnakeCase(member.Member.Name);
                var escaped = _escapeFunc != null ? _escapeFunc(colName) : $"\"{colName}\"";
                _sql.Append(escaped);
                return true;
            }

            throw new System.NotSupportedException("Sql.Outer requires a member expression selector.");
        }
        return false;
    }

    private bool HandleContains(MethodCallExpression node)
    {
        if (node.Object != null && node.Object.Type == typeof(string))
        {
            Visit(node.Object);
            _sql.Append(" LIKE ");
            var val = Evaluate(node.Arguments[0])?.ToString() ?? string.Empty;
            _sql.Append(_parameters.Add($"%{EscapeLikePattern(val)}%")).Append(@" ESCAPE '\'");
            return true;
        }
        
        if (node.Object == null && node.Arguments.Count == 2)
        {
            var arg0 = node.Arguments[0];
            var implicitCast = arg0 as MethodCallExpression;
            if (implicitCast != null && implicitCast.Method.Name == "op_Implicit" && implicitCast.Arguments.Count == 1)
            {
                arg0 = implicitCast.Arguments[0];
            }
            
            var val = Evaluate(arg0) as System.Collections.IEnumerable;
            var ps = new List<string>();
            if (val != null)
            {
                foreach(var item in val)
                {
                    ps.Add(_parameters.Add(item));
                }
            }
            
            if (ps.Count > 0)
            {
                Visit(node.Arguments[1]);
                _sql.Append(" IN (").Append(string.Join(", ", ps)).Append(")");
            }
            else
            {
                _sql.Append("1=0");
            }
            return true;
        }
        
        if (node.Object != null && node.Arguments.Count == 1)
        {
            var val = Evaluate(node.Object) as System.Collections.IEnumerable;
            var ps = new List<string>();
            if (val != null)
            {
                foreach(var item in val)
                {
                    ps.Add(_parameters.Add(item));
                }
            }
            
            if (ps.Count > 0)
            {
                Visit(node.Arguments[0]);
                _sql.Append(" IN (").Append(string.Join(", ", ps)).Append(")");
            }
            else
            {
                _sql.Append("1=0");
            }
            return true;
        }

        return false;
    }

    private bool HandleStartsWith(MethodCallExpression node)
    {
        if (node.Object != null && node.Object.Type == typeof(string))
        {
            Visit(node.Object);
            _sql.Append(" LIKE ");
            var val = Evaluate(node.Arguments[0])?.ToString() ?? string.Empty;
            _sql.Append(_parameters.Add($"{EscapeLikePattern(val)}%")).Append(@" ESCAPE '\'");
            return true;
        }
        return false;
    }

    private bool HandleEndsWith(MethodCallExpression node)
    {
        if (node.Object != null && node.Object.Type == typeof(string))
        {
            Visit(node.Object);
            _sql.Append(" LIKE ");
            var val = Evaluate(node.Arguments[0])?.ToString() ?? string.Empty;
            _sql.Append(_parameters.Add($"%{EscapeLikePattern(val)}")).Append(@" ESCAPE '\'");
            return true;
        }
        return false;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<System.Reflection.MemberInfo, System.Func<object, object?>> _memberCache = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<System.Reflection.MethodInfo, System.Func<object, object?[], object?>> _methodCache = new();

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Array.CreateInstance creates typed array matching the expression element type.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Member and method access covered by class-level RequiresUnreferencedCode.")]
    private object? Evaluate(Expression? expr)
    {
        if (expr == null)
        {
            return null;
        }

        switch (expr)
        {
            case ConstantExpression ce:
                return ce.Value;

            case UnaryExpression ue when ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked:
                return Evaluate(ue.Operand);

            case MemberExpression me:
                var target = Evaluate(me.Expression);
                var getter = _memberCache.GetValue(me.Member, CreateMemberGetter);
                return getter(target!);

            case MethodCallExpression mce:
                var instance = Evaluate(mce.Object);
                var args = new object?[mce.Arguments.Count];
                for (int i = 0; i < mce.Arguments.Count; i++)
                {
                    args[i] = Evaluate(mce.Arguments[i]);
                }
                
                var invoker = _methodCache.GetValue(mce.Method, CreateMethodInvoker);
                return invoker(instance!, args);

            case NewArrayExpression nae:
                var elemType = nae.Type.GetElementType()!;
                var array = Array.CreateInstance(elemType, nae.Expressions.Count);
                for (int i = 0; i < nae.Expressions.Count; i++)
                {
                    array.SetValue(Evaluate(nae.Expressions[i]), i);
                }
                return array;
        }

        throw new NotSupportedException(
            $"Expression of type '{expr.NodeType}' cannot be evaluated in NativeAOT context. " +
            $"Supported evaluation patterns: ConstantExpression, MemberExpression (field/property access), " +
            $"MethodCallExpression, UnaryExpression (Convert/ConvertChecked), and NewArrayExpression. " +
            $"Avoid using closures over complex types or delegates in Where() predicates.");
    }

    private bool IsNullConstantOrExpression(Expression expr)
    {
        var unwrap = expr;
        var u = unwrap as UnaryExpression;
        if (u != null && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked))
        {
            unwrap = u.Operand;
        }
        if (unwrap is ConstantExpression ce)
        {
            return ce.Value == null;
        }
        var me = unwrap as MemberExpression;
        if (me != null && me.Expression != null && me.Expression.NodeType != ExpressionType.Parameter)
        {
            return Evaluate(me) == null;
        }
        return false;
    }

    private string GetSqlOperator(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Equal => " = ",
        ExpressionType.NotEqual => " != ",
        ExpressionType.GreaterThan => " > ",
        ExpressionType.GreaterThanOrEqual => " >= ",
        ExpressionType.LessThan => " < ",
        ExpressionType.LessThanOrEqual => " <= ",
        ExpressionType.AndAlso => " AND ",
        ExpressionType.OrElse => " OR ",
        ExpressionType.And => " & ",
        ExpressionType.Or => " | ",
        ExpressionType.ExclusiveOr => " ^ ",
        ExpressionType.Add or ExpressionType.AddChecked => " + ",
        ExpressionType.Subtract or ExpressionType.SubtractChecked => " - ",
        ExpressionType.Multiply or ExpressionType.MultiplyChecked => " * ",
        ExpressionType.Divide => " / ",
        ExpressionType.Modulo => " % ",
        ExpressionType.LeftShift => " << ",
        ExpressionType.RightShift => " >> ",
        _ => throw new NotSupportedException($"Operator {nodeType} is not supported.")
    };

    [UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "Covered by class-level RequiresUnreferencedCode annotation.")]
    private static System.Func<object, object?> CreateMemberGetter(System.Reflection.MemberInfo member)
    {
        return member switch
        {
            System.Reflection.FieldInfo f => (instance) => f.GetValue(instance),
            System.Reflection.PropertyInfo pi => (instance) => pi.GetValue(instance),
            _ => throw new System.NotSupportedException($"Member type {member.GetType()} not supported")
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Covered by class-level RequiresUnreferencedCode annotation.")]
    private static System.Func<object, object?[], object?> CreateMethodInvoker(System.Reflection.MethodInfo method)
    {
        return (instance, args) => method.Invoke(instance, args);
    }

    private static string EscapeLikePattern(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
    }
}




