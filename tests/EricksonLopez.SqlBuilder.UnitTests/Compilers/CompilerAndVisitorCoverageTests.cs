// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.UnitTests.Compilers;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class CompilerAndVisitorCoverageTests
{
    [Fact]
    public void BaseCompiler_DefaultCapabilitiesAndRendererDelegation()
    {
        var compiler = new TestDefaultCompiler();
        compiler.SupportsCapability(ProviderCapability.WindowFunctions).Should().BeFalse();

        var entity = new TestEntity { Id = 1, Name = "Test", Age = 25 };
        Span<bool> mask = stackalloc bool[3] { true, true, false };
        var insertResult = compiler.RenderInsert(entity, mask);
        insertResult.Sql.Should().Contain("INSERT INTO");

        Span<bool> setMask = stackalloc bool[3] { false, true, false };
        Span<bool> whereMask = stackalloc bool[3] { true, false, false };
        var updateResult = compiler.RenderUpdate(entity, setMask, whereMask);
        updateResult.Sql.Should().Contain("UPDATE");
    }

    [Fact]
    public void BaseCompiler_MultipleGroupByAndOrderBysWithForeignNodes()
    {
        var compiler = new TestDefaultCompiler();
        Expression<Func<UserEntity, int>> idExpr = u => u.Id;
        Expression<Func<UserEntity, string>> nameExpr = u => u.Name;

        var query = Sql.From<UserEntity>()
            .AddNode(new GroupByNode(new[] { "dept" }, GroupByType.Standard))
            .AddNode(new GroupByNode(new[] { "job" }, GroupByType.Standard))
            .AddNode(new OrderByNode(idExpr, false))
            .AddNode(new CustomExtensionNode()) // non-order node inside OrderByNodes partition
            .AddNode(new ThenByNode(nameExpr, true));

        var result = compiler.Compile(query);
        result.Sql.Should().Contain("GROUP BY [dept], [job]");
        result.Sql.Should().Contain("ORDER BY [id], [name] DESC");
        result.Sql.Should().Contain("/* ext */");
    }

    [Fact]
    public void BaseCompiler_InsertUpdateDeleteMerge_WithExtensionNodesAndReturning()
    {
        var compiler = new TestDefaultCompiler();

        // Insert with extension node
        var insertQ = new InsertQuery<UserEntity>()
            .AddNode(new InsertNode("users", new[] { "id", "name" }))
            .AddNode(new ValuesNode(new[] { new object?[] { 1, "Alice" } }))
            .AddNode(new CustomExtensionNode())
            .AddNode(new ReturningNode(new[] { "id" }));
        var insertRes = compiler.Compile(insertQ);
        insertRes.Sql.Should().Contain("RETURNING [id]");
        insertRes.Sql.Should().Contain("/* ext */");

        // Update with extension node and returning
        var updateQ = new UpdateQuery<UserEntity>()
            .AddNode(new UpdateNode("users"))
            .AddNode(new SetNode("name", "Bob"))
            .AddNode(new CustomExtensionNode())
            .AddNode(new ReturningNode(new[] { "id" }));
        var updateRes = compiler.Compile(updateQ);
        updateRes.Sql.Should().Contain("RETURNING [id]");
        updateRes.Sql.Should().Contain("/* ext */");

        // Delete with extension node and returning
        var deleteQ = new DeleteQuery<UserEntity>()
            .AddNode(new DeleteNode("users"))
            .AddNode(new RawWhereNode("id = {0}", new object?[] { 1 }))
            .AddNode(new CustomExtensionNode())
            .AddNode(new ReturningNode(new[] { "id" }));
        var deleteRes = compiler.Compile(deleteQ);
        deleteRes.Sql.Should().Contain("RETURNING [id]");
        deleteRes.Sql.Should().Contain("/* ext */");
    }

    [Fact]
    public void SqlCompilerVisitor_LimitOffsetAndConcurrencyToken_VisitsWithoutError()
    {
        var compiler = new TestDefaultCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var limitNode = new LimitOffsetNode(10, 20);
        visitor.Visit(limitNode); // no-op

        var tokenNode = new ConcurrencyTokenNode("ver", 1, 2, false);
        visitor.Visit(tokenNode); // no-op

        Expression<Func<UserEntity, int>> idExpr = u => u.Id;
        var thenBy = new ThenByNode(idExpr, true);
        visitor.Visit(thenBy);
        context.Sql.ToString().Should().Contain("[id] DESC");
    }

    [Fact]
    public void SqlCompilerVisitor_SubqueryJoin_LateralAndApplyAndExpressionCondition()
    {
        var compiler = new TestDefaultCompiler();

        // Lateral left join with expression condition
        Expression<Func<UserEntity, UserEntity, bool>> expr = (u, sub) => u.Id == sub.Id;
        var subquery = Sql.From<UserEntity>().Where(u => u.Age > 20);

        var nodeLateralLeft = new SubqueryJoinNode(JoinType.Left, subquery, "sub1", null, IsLateral: true, ExpressionCondition: expr);
        var q1 = Sql.From<UserEntity>().AddNode(nodeLateralLeft);
        var res1 = compiler.Compile(q1);
        res1.Sql.Should().Contain("LEFT JOIN LATERAL (SELECT * FROM [users] WHERE (age > @p0)) AS [sub1] ON (id = id)");

        // Cross apply and outer apply subquery join types
        var nodeCrossApply = new SubqueryJoinNode(JoinType.CrossApply, subquery, "sub2", null, IsLateral: false);
        var q2 = Sql.From<UserEntity>().AddNode(nodeCrossApply);
        var res2 = compiler.Compile(q2);
        res2.Sql.Should().Contain("CROSS APPLY (SELECT * FROM [users] WHERE (age > @p0)) AS [sub2]");

        var nodeOuterApply = new SubqueryJoinNode(JoinType.OuterApply, subquery, "sub3", null, IsLateral: false);
        var q3 = Sql.From<UserEntity>().AddNode(nodeOuterApply);
        var res3 = compiler.Compile(q3);
        res3.Sql.Should().Contain("OUTER APPLY (SELECT * FROM [users] WHERE (age > @p0)) AS [sub3]");
    }

    [Fact]
    public void SqlCompilerVisitor_WindowNode_MultiplePartitionsAndOrders()
    {
        var compiler = new TestDefaultCompiler();
        var windowNode = new WindowNode("w", new[] { "dept_id", "team_id" }, new[] { "salary DESC", "hire_date ASC" });
        var q = Sql.From<UserEntity>().AddNode(windowNode);

        var res = compiler.Compile(q);
        res.Sql.Should().Contain("WINDOW [w] AS (PARTITION BY [dept_id], [team_id] ORDER BY [salary] DESC, [hire_date] ASC)");
    }

    [Fact]
    public void SqlExpressionVisitor_NullIf_IsDistinctFrom_IsNotDistinctFrom_Outer()
    {
        var compiler = new TestDefaultCompiler();

        // NullIf with null arg, constant arg, member arg
        var qNullIf = Sql.From<UserEntity>()
            .Where(u => Sql.NullIf(u.Name, (string?)null) == "test" &&
                        Sql.NullIf(u.Name, "default") == "test" &&
                        Sql.NullIf(u.Name, u.Name) == "test");
        var resNullIf = compiler.Compile(qNullIf);
        resNullIf.Sql.Should().Contain("NULLIF(name, NULL)");
        resNullIf.Sql.Should().Contain("NULLIF(name, @p");
        resNullIf.Sql.Should().Contain("NULLIF(name, name)");

        // IsDistinctFrom with null, constant, member
        var qDistinct = Sql.From<UserEntity>()
            .Where(u => Sql.IsDistinctFrom(u.Age, (int?)null) &&
                        Sql.IsDistinctFrom(u.Age, 30) &&
                        Sql.IsDistinctFrom(u.Age, u.Id));
        var resDistinct = compiler.Compile(qDistinct);
        resDistinct.Sql.Should().Contain("age IS DISTINCT FROM NULL");
        resDistinct.Sql.Should().Contain("age IS DISTINCT FROM @p");
        resDistinct.Sql.Should().Contain("age IS DISTINCT FROM id");

        // IsNotDistinctFrom with null, constant, member
        var qNotDistinct = Sql.From<UserEntity>()
            .Where(u => Sql.IsNotDistinctFrom(u.Age, (int?)null) &&
                        Sql.IsNotDistinctFrom(u.Age, 30) &&
                        Sql.IsNotDistinctFrom(u.Age, u.Id));
        var resNotDistinct = compiler.Compile(qNotDistinct);
        resNotDistinct.Sql.Should().Contain("age IS NOT DISTINCT FROM NULL");
        resNotDistinct.Sql.Should().Contain("age IS NOT DISTINCT FROM @p");
        resNotDistinct.Sql.Should().Contain("age IS NOT DISTINCT FROM id");

        // Outer with direct member and unary convert
        var qOuter = Sql.From<UserEntity>()
            .Where(u => u.Id == Sql.Outer<UserEntity, int>(x => x.Id) &&
                        u.Name == Sql.Outer<UserEntity, string>(x => x.Name) &&
                        (object)u.Id == Sql.Outer<UserEntity, object>(x => (object)x.Id));
        var resOuter = compiler.Compile(qOuter);
        resOuter.Sql.Should().Contain("\"id\"");
        resOuter.Sql.Should().Contain("\"name\"");

        // Coalesce with null and Contains with empty string
        var qCoalesceAndContains = Sql.From<UserEntity>()
            .Where(u => Sql.Coalesce(u.Name, (string?)null) == "test" &&
                        u.Name.Contains(""));
        var resCoalesceAndContains = compiler.Compile(qCoalesceAndContains);
        resCoalesceAndContains.Sql.Should().Contain("COALESCE(name, NULL)");
    }

    [Fact]
    public void SqlCompilerBase_DeleteAndUpdatingReturningWithoutTrailingSpace()
    {
        var compiler = new TestDefaultCompiler();

        // Update without set nodes followed by returning
        var updateQ = new UpdateQuery<UserEntity>()
            .AddNode(new UpdateNode("users"))
            .AddNode(new ReturningNode(new[] { "id" }));
        var updateRes = compiler.Compile(updateQ);
        updateRes.Sql.Should().Contain("UPDATE [users] RETURNING [id]");

        // Delete without where nodes followed by returning
        var deleteQ = new DeleteQuery<UserEntity>()
            .AddNode(new DeleteNode("users"))
            .AddNode(new ReturningNode(new[] { "id" }));
        var deleteRes = compiler.Compile(deleteQ);
        deleteRes.Sql.Should().Contain("DELETE FROM [users] RETURNING [id]");

        // Non-trailing space compiler test
        var noSpaceCompiler = new TestNoSpaceCompiler();
        var noSpaceUpdate = noSpaceCompiler.Compile(updateQ);
        noSpaceUpdate.Sql.Should().Contain("UPDATE custom RETURNING [id]");

        var noSpaceDelete = noSpaceCompiler.Compile(deleteQ);
        noSpaceDelete.Sql.Should().Contain("DELETE custom RETURNING [id]");
    }

    [Fact]
    public void SqlCompilerVisitor_EscapeIdentifier_And_CustomMethodsInExpression()
    {
        var compiler = new TestDefaultCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new TestExtensionVisitor(compiler, context);

        visitor.TestEscapeIdentifier("my_column").Should().Be("[my_column]");

        // Custom methods (not declared on Sql) in LINQ expressions
        var q = Sql.From<UserEntity>()
            .Where(u => u.Name == CustomHelper.NullIf("a", "b") &&
                        CustomHelper.IsDistinctFrom(1, 2) &&
                        CustomHelper.IsNotDistinctFrom(1, 1) &&
                        u.Name == CustomHelper.Outer("test"));

        var res = compiler.Compile(q);
        res.Sql.Should().Contain("WHERE");

        // NullIf with Convert unwrap
        var qNullIfConvert = Sql.From<UserEntity>()
            .Where(u => Sql.NullIf<int?>(u.Age, u.Id) == 10);
        var resNullIfConvert = compiler.Compile(qNullIfConvert);
        resNullIfConvert.Sql.Should().Contain("NULLIF(age, id)");

        // JoinNode with CrossApply / OuterApply
        var joinCrossApply = new JoinNode(JoinType.CrossApply, "other_table", "ot", "ot.id = u.id");
        var qJoinCross = Sql.From<UserEntity>().AddNode(joinCrossApply);
        var resJoinCross = compiler.Compile(qJoinCross);
        resJoinCross.Sql.Should().Contain("CROSS APPLY JOIN [other_table] AS [ot] ON ot.id = u.id");

        var joinOuterApply = new JoinNode(JoinType.OuterApply, "other_table", "ot", "ot.id = u.id");
        var qJoinOuter = Sql.From<UserEntity>().AddNode(joinOuterApply);
        var resJoinOuter = compiler.Compile(qJoinOuter);
        resJoinOuter.Sql.Should().Contain("OUTER APPLY JOIN [other_table] AS [ot] ON ot.id = u.id");
    }

    [Fact]
    public void SqlCompilerBase_CompileOrderBys_WithForeignNode_SkipsSafely()
    {
        var compiler = new TestDefaultCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new TestExtensionVisitor(compiler, context);

        var nodes = new ISqlNode[] { new CustomExtensionNode() };
        compiler.CompileOrderBys(nodes, visitor, context);
        context.Sql.ToString().Should().BeEmpty();
    }

    [Fact]
    public void SqlExpressionVisitor_InternalReflectiveEdgeCases()
    {
        // HandleOuter fallback returning false when lambda is null or body is not a member
        var handleOuter = typeof(SqlExpressionVisitor).GetMethod("HandleOuter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var visitor = new SqlExpressionVisitor(new System.Text.StringBuilder(), new ParameterManager(), null);
        var methodCall = Expression.Call(typeof(Sql).GetMethod("Outer")!.MakeGenericMethod(typeof(UserEntity), typeof(int)), Expression.Constant(null, typeof(Expression<Func<UserEntity, int>>)));
        var res = (bool)handleOuter!.Invoke(visitor, new object[] { methodCall })!;
        res.Should().BeFalse();

        Expression<Func<UserEntity, int>> nonMemberLambda = x => 10;
        var methodCallNonMember = Expression.Call(typeof(Sql).GetMethod("Outer")!.MakeGenericMethod(typeof(UserEntity), typeof(int)), Expression.Constant(nonMemberLambda));
        var resNonMember = (bool)handleOuter.Invoke(visitor, new object[] { methodCallNonMember })!;
        resNonMember.Should().BeFalse();

        // DeclaringType != typeof(Sql) returns false
        var nonSqlCall = Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })!, Expression.Constant("a"), Expression.Constant("b"));
        var handleNullIf = typeof(SqlExpressionVisitor).GetMethod("HandleNullIf", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((bool)handleNullIf!.Invoke(visitor, new object[] { nonSqlCall })!).Should().BeFalse();

        var handleDistinct = typeof(SqlExpressionVisitor).GetMethod("HandleIsDistinctFrom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((bool)handleDistinct!.Invoke(visitor, new object[] { nonSqlCall })!).Should().BeFalse();

        var handleNotDistinct = typeof(SqlExpressionVisitor).GetMethod("HandleIsNotDistinctFrom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((bool)handleNotDistinct!.Invoke(visitor, new object[] { nonSqlCall })!).Should().BeFalse();

        ((bool)handleOuter.Invoke(visitor, new object[] { nonSqlCall })!).Should().BeFalse();

        // HandleOuter with custom escape function and direct MemberExpression
        var sbCustom = new System.Text.StringBuilder();
        var visitorCustom = new SqlExpressionVisitor(sbCustom, new ParameterManager(), col => $"[{col}]");
        Expression<Func<UserEntity, int>> memberLambda = x => x.Id;
        var methodCallMember = Expression.Call(typeof(Sql).GetMethod("Outer")!.MakeGenericMethod(typeof(UserEntity), typeof(int)), Expression.Quote(memberLambda));
        var resMember = (bool)handleOuter.Invoke(visitorCustom, new object[] { methodCallMember })!;
        resMember.Should().BeTrue();
        sbCustom.ToString().Should().Be("[id]");

        // EscapeLikePattern on null and empty
        var escapeLike = typeof(SqlExpressionVisitor).GetMethod("EscapeLikePattern", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        escapeLike!.Invoke(null, new object?[] { null }).Should().BeNull();
        escapeLike.Invoke(null, new object[] { "" }).Should().Be("");

        // CreateMemberGetter throwing NotSupportedException for unsupported MemberInfo types (e.g. MethodInfo)
        var createGetter = typeof(SqlExpressionVisitor).GetMethod("CreateMemberGetter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var act = () => createGetter!.Invoke(null, new object[] { typeof(UserEntity).GetMethods()[0] });
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<NotSupportedException>()
            .WithMessage("*not supported*");
    }

    [Fact]
    public void SqlCompilerVisitor_ExhaustiveMutantKillers()
    {
        var compiler = new TestDefaultCompiler();

        // 1. ExistsWhereNode - trailing space and NOT EXISTS
        var qExists = Sql.From<UserEntity>().WhereExists(Sql.From<UserEntity>().Where(u => u.Id == 1));
        var resExists = compiler.Compile(qExists);
        resExists.Sql.Should().Contain("EXISTS (SELECT * FROM [users] WHERE (id = @p0))");

        var qNotExists = Sql.From<UserEntity>().WhereNotExists(Sql.From<UserEntity>().Where(u => u.Id == 1));
        var resNotExists = compiler.Compile(qNotExists);
        resNotExists.Sql.Should().Contain("NOT EXISTS (SELECT * FROM [users] WHERE (id = @p0))");

        // 2. OrderByNode - NullsFirst and NullsLast
        var qNullsFirst = Sql.From<UserEntity>().OrderBy(u => u.Age, NullsPosition.First);
        var resNullsFirst = compiler.Compile(qNullsFirst);
        resNullsFirst.Sql.Should().Contain("ORDER BY [age] NULLS FIRST");

        var qNullsLast = Sql.From<UserEntity>().OrderBy(u => u.Age, NullsPosition.Last);
        var resNullsLast = compiler.Compile(qNullsLast);
        resNullsLast.Sql.Should().Contain("ORDER BY [age] NULLS LAST");

        // 3. SubqueryJoinNode with ON condition string vs expression condition
        var subOnStr = new SubqueryJoinNode(JoinType.Left, Sql.From<UserEntity>(), "sub_str", "sub_str.id = u.id");
        var qSubStr = Sql.From<UserEntity>().AddNode(subOnStr);
        var resSubStr = compiler.Compile(qSubStr);
        resSubStr.Sql.Should().Contain("LEFT JOIN (SELECT * FROM [users]) AS [sub_str] ON sub_str.id = u.id");

        // 4. WindowFunctionNode with multiple partitions and multiple order columns with directions
        var winNode = new WindowFunctionNode(
            "ROW_NUMBER",
            null,
            null,
            null,
            new[] { "dept", "team" },
            new[] { "salary", "age" },
            new[] { true, false },
            "rn"
        );
        var qWin = Sql.From<UserEntity>().Select("id").AddNode(winNode);
        var resWin = compiler.Compile(qWin);
        resWin.Sql.Should().Contain("ROW_NUMBER() OVER (PARTITION BY [dept], [team] ORDER BY [salary] DESC, [age]) AS [rn]");

        // 5. GroupByNode with GroupingSets having multiple sets and empty columns
        var groupSets = new GroupByNode(
            new[] { "dummy" },
            GroupByType.GroupingSets,
            new List<IReadOnlyList<string>> { new[] { "colA", "colB" }, new[] { "colC" } }
        );
        var qGroup = Sql.From<UserEntity>().AddNode(groupSets);
        var resGroup = compiler.Compile(qGroup);
        resGroup.Sql.Should().Contain("GROUPING SETS (([colA], [colB]), ([colC]))");

        // GroupBy with empty columns
        var groupEmpty = new GroupByNode(Array.Empty<string>(), GroupByType.Standard);
        var qGroupEmpty = Sql.From<UserEntity>().AddNode(groupEmpty);
        var resGroupEmpty = compiler.Compile(qGroupEmpty);
        resGroupEmpty.Sql.Should().Be("SELECT * FROM [users] GROUP BY");

        // 6. InsertSelectNode with multiple columns
        var insertSelect = new InsertSelectNode("target_table", new[] { "col1", "col2", "col3" }, Sql.From<UserEntity>());
        var qInsertSelect = new InsertQuery<UserEntity>().AddNode(insertSelect);
        var resInsertSelect = compiler.Compile(qInsertSelect);
        resInsertSelect.Sql.Should().Contain("INSERT INTO [target_table] ([col1], [col2], [col3]) SELECT * FROM [users]");

        // 7. CompositeCursorNode with 2 ascending/descending keys
        var cursorKeys = new[]
        {
            new CursorKey("created_at", DateTime.UtcNow, false),
            new CursorKey("id", 100, true)
        };
        var cursorNode = new CompositeCursorNode(cursorKeys, true);
        var qCursor = Sql.From<UserEntity>().AddNode(cursorNode);
        var resCursor = compiler.Compile(qCursor);
        resCursor.Sql.Should().Contain("([created_at] > @p0 OR ([created_at] = @p0 AND [id] < @p1))");

        // 8. OnConflictNode with multiple target columns and update expression
        var conflictNode = new OnConflictNode(
            new[] { "id", "tenant_id" },
            "DO UPDATE SET name = EXCLUDED.name",
            null,
            null
        );
        var qConflict = new InsertQuery<UserEntity>().AddNode(new InsertNode("users", new[] { "id" })).AddNode(conflictNode);
        var resConflict = compiler.Compile(qConflict);
        resConflict.Sql.Should().Contain("ON CONFLICT ([id], [tenant_id]) DO UPDATE SET name = EXCLUDED.name");

        // 9. CaseNode with Else clause and Alias
        var caseNode = new CaseNode(
            new[] { new CaseWhenBranch("id = 1", null, "'One'", null) },
            "'Zero'",
            null,
            "category"
        );
        var qCase = Sql.From<UserEntity>().Select("id").AddNode(caseNode);
        var resCase = compiler.Compile(qCase);
        resCase.Sql.Should().Contain("CASE WHEN id = 1 THEN 'One' ELSE 'Zero' END AS [category]");

        // 10. JoinNode with ExpressionCondition followed by WHERE
        var paramU1 = Expression.Parameter(typeof(UserEntity), "u1");
        var paramU2 = Expression.Parameter(typeof(UserEntity), "u2");
        var joinExpr = Expression.Equal(Expression.Property(paramU1, "Id"), Expression.Property(paramU2, "Id"));
        var qJoinExprWhere = Sql.From<UserEntity>()
            .AddNode(new JoinNode(JoinType.Inner, "other", null, null, joinExpr))
            .Where(u => u.Age > 20);
        var resJoinExprWhere = compiler.Compile(qJoinExprWhere);
        resJoinExprWhere.Sql.Should().Contain("INNER JOIN [other] ON (id = id) WHERE (age > @p0)");

        // 11. OrderByNode with non-unary string member
        var qOrderString = Sql.From<UserEntity>().OrderBy(u => u.Name);
        var resOrderString = compiler.Compile(qOrderString);
        resOrderString.Sql.Should().Contain("ORDER BY [name]");

        // 12. SubqueryJoinNode with Full Join and ExpressionCondition followed by WHERE
        var subExpr = Expression.Equal(Expression.Property(paramU1, "Id"), Expression.Constant(1));
        var subFull = new SubqueryJoinNode(JoinType.Full, Sql.From<UserEntity>(), "sub_full", null, false, subExpr);
        var qSubFull = Sql.From<UserEntity>().AddNode(subFull).Where(u => u.Age > 20);
        var resSubFull = compiler.Compile(qSubFull);
        resSubFull.Sql.Should().Contain("FULL JOIN (SELECT * FROM [users]) AS [sub_full] ON (id = @p0) WHERE (age > @p1)");

        // 13. ScalarSubquerySelectNode
        var scalarSub = new ScalarSubquerySelectNode(Sql.From<UserEntity>().Select("id"), "cnt");
        var qScalarSub = Sql.From<UserEntity>().AddNode(scalarSub);
        var resScalarSub = compiler.Compile(qScalarSub);
        resScalarSub.Sql.Should().Contain("(SELECT [id] FROM [users]) AS [cnt]");

        // 14. GroupByNode with empty sets
        var groupEmptySets = new GroupByNode(new[] { "dummy" }, GroupByType.GroupingSets, new List<IReadOnlyList<string>>());
        var qGroupEmptySets = Sql.From<UserEntity>().AddNode(groupEmptySets);
        compiler.Compile(qGroupEmptySets).Sql.Should().Contain("GROUPING SETS ()");

        // 15. InsertSelectNode with empty columns
        var insertSelectZeroCols = new InsertSelectNode("target_table", Array.Empty<string>(), Sql.From<UserEntity>());
        var qInsertSelectZero = new InsertQuery<UserEntity>().AddNode(insertSelectZeroCols);
        compiler.Compile(qInsertSelectZero).Sql.Should().Be("INSERT INTO [target_table] SELECT * FROM [users]");

        // 16. CompositeCursorNode with 3 keys
        var cursor3Keys = new[]
        {
            new CursorKey("tenant_id", 1, false),
            new CursorKey("created_at", DateTime.UtcNow, false),
            new CursorKey("id", 100, true)
        };
        var qCursor3 = Sql.From<UserEntity>().AddNode(new CompositeCursorNode(cursor3Keys, true));
        var resCursor3 = compiler.Compile(qCursor3);
        resCursor3.Sql.Should().Contain("([tenant_id] > @p0 OR ([tenant_id] = @p0 AND [created_at] > @p1 OR ([created_at] = @p1 AND [id] < @p2)))");

        // 17. OnConflictNode with both UpdateAction and UpdateExpression
        var conflictBoth = new OnConflictNode(
            new[] { "id" },
            "DO UPDATE SET",
            (Expression<Func<UserEntity, bool>>)(u => u.Name == "New"),
            null
        );
        var qConflictBoth = new InsertQuery<UserEntity>()
            .AddNode(new InsertNode("users", new[] { "id" }))
            .AddNode(conflictBoth)
            .AddNode(new CustomExtensionNode());
        var resConflictBoth = compiler.Compile(qConflictBoth);
        resConflictBoth.Sql.Should().Contain("ON CONFLICT ([id]) DO UPDATE SET (name = @p0) /* ext */");

        // 18. WindowFunctionNode with NTILE (raw bucket count not escaped)
        var winNtile = new WindowFunctionNode("NTILE", "4", null, null, Array.Empty<string>(), new[] { "id" }, new[] { false }, "quartile");
        var qNtile = Sql.From<UserEntity>().Select("id").AddNode(winNtile);
        var resNtile = compiler.Compile(qNtile);
        resNtile.Sql.Should().Contain("NTILE(4) OVER (ORDER BY [id]) AS [quartile]");

        // 19. GroupByNode with null Sets and null Columns
        var groupNullSets = new GroupByNode(new[] { "id" }, GroupByType.GroupingSets, null);
        var qGroupNullSets = Sql.From<UserEntity>().AddNode(groupNullSets);
        compiler.Compile(qGroupNullSets).Sql.Should().Contain("GROUPING SETS ()");

        var groupNullCols = new GroupByNode(null, GroupByType.Standard);
        var qGroupNullCols = Sql.From<UserEntity>().AddNode(groupNullCols);
        compiler.Compile(qGroupNullCols).Sql.Should().Be("SELECT * FROM [users] GROUP BY");

        // 20. RawJoinNode followed by Where
        var qRawJoinWhere = Sql.From<UserEntity>()
            .AddNode(new RawJoinNode("LEFT JOIN audit ON audit.user_id = users.id"))
            .Where(u => u.Id == 1);
        var resRawJoinWhere = compiler.Compile(qRawJoinWhere);
        resRawJoinWhere.Sql.Should().Be("SELECT * FROM [users] LEFT JOIN audit ON audit.user_id = users.id WHERE (id = @p0)");

        // 21. SubqueryJoinNode with OnCondition followed by Where
        var subOnCond = new SubqueryJoinNode(JoinType.Left, Sql.From<UserEntity>(), "s", "s.id = users.id");
        var qSubOnCond = Sql.From<UserEntity>().AddNode(subOnCond).Where(u => u.Id == 1);
        var resSubOnCond = compiler.Compile(qSubOnCond);
        resSubOnCond.Sql.Should().Be("SELECT * FROM [users] LEFT JOIN (SELECT * FROM [users]) AS [s] ON s.id = users.id WHERE (id = @p0)");

        // 22. Delete query and ReturningNode followed by CustomExtensionNode

        var deleteWithExt = new DeleteQuery<UserEntity>()
            .AddNode(new DeleteNode("users"))
            .AddNode(new ReturningNode(new[] { "id" }))
            .AddNode(new CustomExtensionNode());
        var resDeleteExt = compiler.Compile(deleteWithExt);
        resDeleteExt.Sql.Should().Be("DELETE FROM [users] RETURNING [id] /* ext */");
    }



    [Fact]
    public void SqlCompilerBase_ExhaustiveMutantKillers()
    {
        var compiler = new TestDefaultCompiler();

        // 1. Raw query with trailing whitespace & whitespace only
        var rawWithSpaces = new RawQuery("SELECT * FROM users   \t \r\n");
        var resRaw = compiler.Compile(rawWithSpaces);
        resRaw.Sql.Should().Be("SELECT * FROM users");

        var rawSingleChar = new RawQuery("a   \t  ");
        var resSingleChar = compiler.Compile(rawSingleChar);
        resSingleChar.Sql.Should().Be("a");

        // 2. Escape identifiers (single, dotted 2 parts, dotted 3 parts, leading dot)
        compiler.Escape("users").Should().Be("[users]");
        compiler.Escape("dbo.users").Should().Be("[dbo].[users]");
        compiler.Escape("server.dbo.users").Should().Be("[server].[dbo].[users]");
        compiler.Escape(".table").Should().Be("[].[table]");

        // 3. CompileDistinct with RawSelectNode (starts with SELECT ) and Custom Distinct Compiler
        var qDistinctSelect = Sql.From<UserEntity>().AddNode(new RawSelectNode("id", null, true));
        var resDistinct = compiler.Compile(qDistinctSelect);
        resDistinct.Sql.Should().Be("SELECT DISTINCT id FROM [users]");

        var customDistinctComp = new TestCustomDistinctCompiler();
        var qCustomDistinct = Sql.From<UserEntity>().Select("id").AddNode(new DistinctOnNode(new[] { "id" }));
        var resCustomDistinct = customDistinctComp.Compile(qCustomDistinct);
        resCustomDistinct.Sql.Should().Be("SELECT DISTINCT ON (custom) [id] FROM [users]");

        // 4. Multiple CompositeCursors without existing Where
        var cursor1 = new CompositeCursorNode(new[] { new CursorKey("id", 1, false) }, true);
        var cursor2 = new CompositeCursorNode(new[] { new CursorKey("tenant_id", 2, false) }, true);
        var qDoubleCursor = Sql.From<UserEntity>().AddNode(cursor1).AddNode(cursor2);
        var resDoubleCursor = compiler.Compile(qDoubleCursor);
        resDoubleCursor.Sql.Should().Be("SELECT * FROM [users] WHERE ([id] > @p0) AND ([tenant_id] > @p1)");

        // 5. CompositeCursor with existing Where
        var qCursorWithWhere = Sql.From<UserEntity>().Where(u => u.Age > 18).AddNode(cursor1);
        var resCursorWithWhere = compiler.Compile(qCursorWithWhere);
        resCursorWithWhere.Sql.Should().Be("SELECT * FROM [users] WHERE (age > @p0) AND ([id] > @p1)");

        // 6. Concurrency tokens: AutoIncrement vs Explicit Value, null new value without autoincrement, and multiple tokens without where
        var updateWithTokens = new UpdateQuery<UserEntity>()
            .AddNode(new UpdateNode("users"))
            .AddNode(new SetNode("name", "NewName"))
            .AddNode(new ConcurrencyTokenNode("version", 1, 2, false))
            .AddNode(new ConcurrencyTokenNode("sub_version", 10, null, true));
        var resTokens = compiler.Compile(updateWithTokens);
        resTokens.Sql.Should().Be("UPDATE [users] SET [name] = @p0, [version] = @p1, [sub_version] = [sub_version] + 1 WHERE [version] = @p2 AND [sub_version] = @p3");

        var updateWithNullToken = new UpdateQuery<UserEntity>()
            .AddNode(new UpdateNode("users"))
            .AddNode(new ConcurrencyTokenNode("opt_lock", 1, null, false));
        var resNullToken = compiler.Compile(updateWithNullToken);
        resNullToken.Sql.Should().Be("UPDATE [users] SET [opt_lock] = @p0 WHERE [opt_lock] = @p1");

        // 7. Non-root compilation produces empty parameters dictionary in SqlResult
        var subquery = Sql.From<UserEntity>().Where(u => u.Id == 10);
        var subRes = compiler.Compile(subquery, new ParameterManager());
        subRes.Parameters.Should().BeEmpty();

        // 8. ReturningNode on Insert, Update, and Delete
        var insertReturning = new InsertQuery<UserEntity>()
            .AddNode(new InsertNode("users", new[] { "name" }))
            .AddNode(new ValuesNode(new List<IReadOnlyList<object?>> { new List<object?> { "Alice" } }))
            .AddNode(new ReturningNode(new[] { "id" }));
        var resInsertReturning = compiler.Compile(insertReturning);
        resInsertReturning.Sql.Should().Be("INSERT INTO [users] ([name]) VALUES (@p0) RETURNING [id]");

        using var ctxInsert = new CompilationContext(new ParameterManager());
        compiler.CompileInsert(new ISqlNode[] { new ReturningNode(new[] { "id" }) }, compiler.CreateVisitor(ctxInsert), ctxInsert);
        ctxInsert.Sql.ToString().TrimEnd().Should().Be("RETURNING [id]");

        var insertSelectAndValues = new InsertQuery<UserEntity>()
            .AddNode(new InsertSelectNode("users", new[] { "id" }, new RawQuery("SELECT 1")))
            .AddNode(new ValuesNode(new List<IReadOnlyList<object?>> { new List<object?> { 1 } }));
        var resInsertSelectValues = compiler.Compile(insertSelectAndValues);
        resInsertSelectValues.Sql.Should().Be("INSERT INTO [users] ([id]) SELECT 1");

        var paramU = Expression.Parameter(typeof(UserEntity), "u");
        var updateReturning = new UpdateQuery<UserEntity>()
            .AddNode(new UpdateNode("users"))
            .AddNode(new SetNode("name", "Bob"))
            .AddNode(new ExpressionWhereNode(Expression.Equal(Expression.Property(paramU, "Id"), Expression.Constant(1)), false))
            .AddNode(new ReturningNode(new[] { "id" }));
        var resUpdateReturning = compiler.Compile(updateReturning);
        resUpdateReturning.Sql.Should().Be("UPDATE [users] SET [name] = @p0 WHERE (id = @p1) RETURNING [id]");

        using var ctxUpdate = new CompilationContext(new ParameterManager());
        compiler.CompileUpdate(new ISqlNode[] { new ReturningNode(new[] { "id" }) }, compiler.CreateVisitor(ctxUpdate), ctxUpdate);
        ctxUpdate.Sql.ToString().TrimEnd().Should().Be("RETURNING [id]");

        var deleteReturning = new DeleteQuery<UserEntity>()
            .AddNode(new DeleteNode("users"))
            .AddNode(new ExpressionWhereNode(Expression.Equal(Expression.Property(paramU, "Id"), Expression.Constant(1)), false))
            .AddNode(new ReturningNode(new[] { "id" }));
        var resDeleteReturning = compiler.Compile(deleteReturning);
        resDeleteReturning.Sql.Should().Be("DELETE FROM [users] WHERE (id = @p0) RETURNING [id]");

        using var ctxDelete = new CompilationContext(new ParameterManager());
        compiler.CompileDelete(new ISqlNode[] { new ReturningNode(new[] { "id" }) }, compiler.CreateVisitor(ctxDelete), ctxDelete);
        ctxDelete.Sql.ToString().TrimEnd().Should().Be("RETURNING [id]");
    }
}




