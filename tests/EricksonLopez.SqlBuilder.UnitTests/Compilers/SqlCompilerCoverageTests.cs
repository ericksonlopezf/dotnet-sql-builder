// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.UnitTests.Infrastructure;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests
{
    [Collection("SqlBuilderDiagnosticsCollection")]
    public class SqlCompilerCoverageTests : IDisposable
    {
        private class FakeQuery : EricksonLopez.SqlBuilder.Abstractions.IAstQuery
        {
        public string? Tag => null;
            private readonly EricksonLopez.SqlBuilder.Abstractions.ISqlNode[] _nodes;
            public FakeQuery(params EricksonLopez.SqlBuilder.Abstractions.ISqlNode[] nodes) => _nodes = nodes;
            IReadOnlyList<EricksonLopez.SqlBuilder.Abstractions.ISqlNode> IAstQuery.Nodes => _nodes;
            public EricksonLopez.SqlBuilder.Abstractions.SqlResult Build(EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler compiler) => compiler.Compile(this);
        }

        private readonly System.Diagnostics.ActivityListener _listener;

        public SqlCompilerCoverageTests()
        {
            _listener = new System.Diagnostics.ActivityListener
            {
                ShouldListenTo = s => s.Name == "EricksonLopez.SqlBuilder",
                Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref System.Diagnostics.ActivityCreationOptions<string> _) => System.Diagnostics.ActivitySamplingResult.AllData
            };
            System.Diagnostics.ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
            GC.SuppressFinalize(this);
        }

        private readonly List<ISqlCompiler> _compilers = new()
        {
            new SqliteCompiler(),
            new SqlServerCompiler(),
            new MySqlCompiler(),
            new OracleCompiler(),
            new PostgreSqlCompiler()
        };

        [Fact]
        public void Compile_RawQuery_CompilesForAll()
        {
            var rawQuery = new RawQuery("SELECT * FROM Dummy WHERE Id = @p0", new Dictionary<string, object?> { { "p0", 1 } });
            
            foreach (var compiler in _compilers)
            {
                var result = compiler.Compile(rawQuery);
                result.Sql.Trim().Should().Be("SELECT * FROM Dummy WHERE Id = @p0");
                result.Parameters.Should().ContainKey("p0").WhoseValue.Should().Be(1);
            }
        }

        [Fact]
        public void Compile_SelectQuery_CompilesForAll()
        {
            var query = new SelectQuery<DummyEntity>()
                .Select(x => new { x.Id, x.Name })
                .Where(x => x.Id == 1)
                .OrderByDescending(x => x.Name)
                .Offset(10).Limit(5);

            foreach (var compiler in _compilers)
            {
                var result = compiler.Compile(query);
                result.Sql.Should().NotBeEmpty();
                result.Parameters.Count.Should().Be(1);
            }
        }

        [Fact]
        public void Compile_InsertQuery_CompilesForAll()
        {
            var query = new InsertQuery<DummyEntity>()
                .Values(new DummyEntity { Id = 1, Name = "Test" });

            foreach (var compiler in _compilers)
            {
                var result = compiler.Compile(query);
                result.Sql.Should().NotBeEmpty();
                // Ensure values are compiled
                result.Parameters.Count.Should().Be(2); 
            }
        }

        [Fact]
        public void Compile_UpdateQuery_CompilesForAll()
        {
            var query = new UpdateQuery<DummyEntity>()
                .Set(x => x.Name, "Updated")
                .Where(x => x.Id == 1);

            foreach (var compiler in _compilers)
            {
                var result = compiler.Compile(query);
                result.Parameters.Should().HaveCount(2, $"Compiler {compiler.GetType().Name} failed! SQL: {result.Sql}");
            }
        }

        [Fact]
        public void Compile_DeleteQuery_CompilesForAll()
        {
            var query = new DeleteQuery<DummyEntity>()
                .Where(x => x.Id == 1);

            foreach (var compiler in _compilers)
            {
                var result = compiler.Compile(query);
                result.Sql.Should().NotBeEmpty();
                result.Parameters.Count.Should().Be(1);
            }
        }

        [Fact]
        public void Compile_AllNodes_CompilesForAll()
        {
            var query = new SelectQuery<DummyEntity>()
                .RawSelect((FormattableString)$"MAX(id) + {1}")
                .Select(x => x.Id)
                .Select("rt.id")
                .From(new SelectQuery<DummyEntity>().Select(x => x.Id), "sub")
                .RawJoin((FormattableString)$"INNER JOIN raw_table rt ON rt.id = sub.id AND rt.val = {42}")
                .LeftJoin("left_table", "lt", "lt.id = rt.id")
                .RightJoin("right_table", "rt2", "rt2.id = lt.id")
                .CrossJoin("cross_table", "ct")
                .FullJoin("full_table", "ft", "ft.id = ct.id")
                .Where((FormattableString)$"rt.id = {1}")
                .Where((FormattableString)$"table.column > {0}")
                .GroupBy("name")
                .Having(x => x.Id > 1)
                .Having((FormattableString)$"MAX(id) > {2}")
                .OrderBy((FormattableString)$"name ASC")
                .OrderByDescending((FormattableString)$"name DESC");

            foreach (var compiler in _compilers)
            {
                var result = compiler.Compile(query);
                result.Sql.Should().NotBeEmpty();
            }
        }

        private class DefaultCompiler : SqlCompilerBase
        {
            protected override ISqlRenderer AotRenderer => null!;
            public void ClearProperties() { }
        }

        [Fact]
        public void Compile_BaseImplementations_SelectNodes()
        {
            var compiler = new DefaultCompiler();
            var select = new SelectQuery<DummyEntity>().Select("id").Distinct();
            var selectExp = new SelectQuery<DummyEntity>().Select(x => x.Id).Distinct();
            var rawSelectNullParams = new SelectQuery<DummyEntity>().AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.RawSelectNode("1", null, false));
            var rawSelectEmptyParams = new SelectQuery<DummyEntity>().AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.RawSelectNode("1", System.Array.Empty<object?>(), false));
            var rawSelectDistinct = new SelectQuery<DummyEntity>().AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.RawSelectNode("1", null, true));

            compiler.Compile(select);
            compiler.Compile(selectExp);
            compiler.Compile(rawSelectNullParams);
            compiler.Compile(rawSelectEmptyParams);
            compiler.Compile(rawSelectDistinct);
        }

        [Fact]
        public void Compile_BaseImplementations_WhereAndWindowNodes()
        {
            var compiler = new DefaultCompiler();
            var windowPageAsc = new SelectQuery<DummyEntity>().WindowPage(2, 10, "id", false);
            var windowPageDesc = new SelectQuery<DummyEntity>().WindowPage(2, 10, "id", true);
            var windowPageAscNoSelect = new SelectQuery<DummyEntity>().Select(System.Array.Empty<string>()).WindowPage(2, 10, "id", false);
            var windowPageDescNoSelect = new SelectQuery<DummyEntity>().Select(System.Array.Empty<string>()).WindowPage(2, 10, "id", true);
            var multipleWindows = new SelectQuery<DummyEntity>().Window("w1", new[] { "id" }, System.Array.Empty<string>()).Window("w2", new[] { "name" }, System.Array.Empty<string>());
            
            var expTrue = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(true));
            var whereCombos = new SelectQuery<DummyEntity>()
                .Where(x => x.Id == 1) // index 0
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionWhereNode(expTrue, true))
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionWhereNode(expTrue, false))
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.RawWhereNode("1=1", null, true))
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.RawWhereNode("1=1", null, false));

            compiler.Compile(windowPageAsc);
            compiler.Compile(windowPageDesc);
            compiler.Compile(windowPageAscNoSelect);
            compiler.Compile(windowPageDescNoSelect);
            compiler.Compile(multipleWindows);
            compiler.Compile(whereCombos);
        }

        [Fact]
        public void Compile_BaseImplementations_InsertUpdateDeleteNodes()
        {
            var compiler = new DefaultCompiler();
            var expNode = new EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionSelectNode(System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(1), System.Linq.Expressions.Expression.Parameter(typeof(DummyEntity), "x")), false);
            var expNodeDistinct = new EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionSelectNode(System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Constant(1), System.Linq.Expressions.Expression.Parameter(typeof(DummyEntity), "x")), true);
            
            var insert = new InsertQuery<DummyEntity>().Into("dummy_table").Values(new DummyEntity()).AddNode(expNode).OnConflict("id").DoNothing();
            var insert2 = new InsertQuery<DummyEntity>().Into("dummy_table").Values(new DummyEntity()).OnConflict("id").DoUpdate(x => 1);
            var insert3 = new InsertQuery<DummyEntity>().Into("dummy_table").Values(new DummyEntity()).OnConflict("id").DoUpdate((System.FormattableString)$"name = 't'");
            var update = ((UpdateQuery<DummyEntity>)new UpdateQuery<DummyEntity>().Update("dummy_table").Set(x => x.Name, "test").WhereAll().Returning("id")).AddNode(expNodeDistinct);
            var delete = ((DeleteQuery<DummyEntity>)new DeleteQuery<DummyEntity>().Delete("dummy_table").Where(x => x.Id == 1)).AddNode(expNode);

            compiler.Compile(insert);
            compiler.Compile(insert2);
            compiler.Compile(insert3);
            compiler.Compile(new InsertQuery<DummyEntity>());
            var insertConflictOnly = new InsertQuery<DummyEntity>().Into("dummy_table").Values(new DummyEntity()).OnConflict("id");
            compiler.Compile(insertConflictOnly);
            
            compiler.ClearProperties();
            compiler.Compile(update);
            compiler.Compile(new UpdateQuery<DummyEntity>());
            compiler.Compile(delete);
        }

        [Fact]
        public void Compile_BaseImplementations_EdgeSelectNodes()
        {
            var compiler = new DefaultCompiler();
            var selectNew = new SelectQuery<DummyEntity>().Select(x => new { x.Id });
            var selectNewEmpty = new SelectQuery<DummyEntity>().Select(x => new { });
            var selectConstant = new SelectQuery<DummyEntity>().Select(x => 1);
            var selectLimitOnly = new SelectQuery<DummyEntity>().Limit(10);
            var selectOffsetOnly = new SelectQuery<DummyEntity>().Offset(10);
            var joinExpr = new SelectQuery<DummyEntity>().AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinNode(EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Inner, "table2", "t2", null, System.Linq.Expressions.Expression.Constant(true)));
            var joinRaw = new SelectQuery<DummyEntity>().InnerJoin("table2", "t2", "t1.id = table2.id");
            var orderByConst = new SelectQuery<DummyEntity>().OrderBy(x => 1);
            var orderByUnary = new SelectQuery<DummyEntity>().OrderBy(x => (object)x.Id);
            var selectConstantNoLambda = new SelectQuery<DummyEntity>().AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionSelectNode(System.Linq.Expressions.Expression.Constant(1), false));
            var selectUnnest = new SelectQuery<DummyEntity>().AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.UnnestNode(new object[] { new[] { 1, 2, 3 } }, "unnest_alias"));

            compiler.Compile(selectNew).Sql.Should().Be("SELECT id");
            compiler.Compile(selectNewEmpty).Sql.Should().Be("SELECT *");
            compiler.Compile(selectConstant).Sql.Should().Be("SELECT *");
            compiler.Compile(selectConstantNoLambda).Sql.Should().Be("SELECT *");
            compiler.Compile(selectLimitOnly).Sql.Should().Be("SELECT * LIMIT 10");
            compiler.Compile(selectOffsetOnly).Sql.Should().Be("SELECT * OFFSET 10");
            compiler.Compile(joinExpr).Sql.Should().Be("SELECT * INNER JOIN \"table2\" AS \"t2\" ON @p0");
            compiler.Compile(joinRaw).Sql.Should().Be("SELECT * INNER JOIN \"table2\" AS \"t2\" ON t1.id = table2.id");
            compiler.Compile(orderByConst).Sql.Should().Be("SELECT * ORDER BY");
            compiler.Compile(orderByUnary).Sql.Should().Be("SELECT * ORDER BY \"id\"");
            compiler.Compile(new SelectQuery<DummyEntity>().OrderByDescending(x => x.Id)).Sql.Should().Be("SELECT * ORDER BY \"id\" DESC");
            compiler.Compile(new SelectQuery<DummyEntity>().OrderByDescending(x => (object)x.Id)).Sql.Should().Be("SELECT * ORDER BY \"id\" DESC");
            compiler.Compile(selectUnnest).Sql.Should().Be("SELECT * FROM UNNEST(@p0) AS \"unnest_alias\"");
        }

        [Fact]
        public void Compile_BaseCrudImplementations_CoversAllNodes()
        {
            var defaultCompiler = new DefaultCompiler();
            
            // Testing base CompileInsert, CompileUpdate, CompileDelete directly on SqlCompilerBase
            var insert = new InsertQuery<DummyEntity>()
                .Into("dummy_table")
                .Values(new DummyEntity { Id = 1, Name = "Test" })
                .Returning("id");
            var resInsert = defaultCompiler.Compile(insert);
            resInsert.Sql.Should().Contain("INSERT INTO");

            var insertDefault = new InsertQuery<DummyEntity>()
                .Into("dummy_table")
                .DefaultValues();
            var resInsertDefault = defaultCompiler.Compile(insertDefault);
            resInsertDefault.Sql.Should().Contain("DEFAULT VALUES");

            var update = new UpdateQuery<DummyEntity>()
                .Update("dummy_table")
                .Set(x => x.Name, "Test")
                .Where(x => x.Id == 1)
                .Returning("id");
            var resUpdate = defaultCompiler.Compile(update);
            resUpdate.Sql.Should().Contain("UPDATE");

            var delete = new DeleteQuery<DummyEntity>()
                .Delete("dummy_table")
                .Where(x => x.Id == 1)
                .Returning("id");
            var resDelete = defaultCompiler.Compile(delete);
            resDelete.Sql.Should().Contain("DELETE");

            var subquery = new SelectQuery<DummyEntity>().Select(x => x.Id);
            var insertSelect = Sql.InsertFrom<DummyEntity>(subquery, "Id", "Name");
            var resInsertSelect = defaultCompiler.Compile(insertSelect);
            resInsertSelect.Sql.Should().Contain("INSERT INTO");

            // Empty partition tests
            var emptyNodes = new FakeQuery(Array.Empty<ISqlNode>());
            defaultCompiler.Compile(emptyNodes);

            var deleteEmpty = new FakeQuery(new DeleteNode("t"));
            defaultCompiler.Compile(deleteEmpty);
        }

        [Fact]
        public void Compile_AllNodes_CompilesForAll_Queries()
        {
            var insert = new InsertQuery<DummyEntity>()
                .Into("dummy_table")
                .Values(new DummyEntity())
                .OnConflict(x => x.Id)
                .DoUpdate((FormattableString)$"count = count + {1}")
                .Returning("id");

            var insertDefault = new InsertQuery<DummyEntity>()
                .Into("dummy_table")
                .DefaultValues();

            var update = new UpdateQuery<DummyEntity>()
                .Update("dummy_table")
                .Set(x => x.Name, "test")
                .Set((FormattableString)$"count = count + {1}")
                .Where(x => x.Id == 1)
                .Returning(x => x.Id);

            var delete = new DeleteQuery<DummyEntity>()
                .Delete("dummy_table")
                .Where(x => x.Id == 1)
                .Returning("id");
                
            var cte = new SelectQuery<DummyEntity>()
                .CTE("cte1", new SelectQuery<DummyEntity>().Select(x => x.Id))
                .CTE("cte2", new SelectQuery<DummyEntity>().Select(x => x.Id))
                .Select(x => x.Id);
                
            var cteRecursive = new SelectQuery<DummyEntity>()
                .RecursiveCTE("cte1", new SelectQuery<DummyEntity>().Select(x => x.Id))
                .Select(x => x.Id);
                
            var window = new SelectQuery<DummyEntity>()
                .Window("w1", new[] { "id" }, new[] { "name DESC" })
                .Select(x => x.Id);
                
            var windowPage = new SelectQuery<DummyEntity>()
                .RecursiveCTE("cte1", new SelectQuery<DummyEntity>().Select(x => x.Id))
                .CTE("cte2", new SelectQuery<DummyEntity>().Select(x => x.Id))
                .WindowPage(1, 10, "Id", true)
                .Select(x => x.Id);
                
            var windowPageNoSelect = new SelectQuery<DummyEntity>()
                .WindowPage(1, 10, "Id", false);
                
            var union = new SelectQuery<DummyEntity>()
                .Select(x => x.Id)
                .Union(new SelectQuery<DummyEntity>().Select(x => x.Id));

            foreach (var compiler in _compilers)
            {
                // Explicit dialect capability checks:
                if (compiler is OracleCompiler)
                {
                    Action actInsert = () => compiler.Compile(insert);
                    actInsert.Should().Throw<NotSupportedException>().WithMessage("*Oracle does not support ON CONFLICT*");
                }
                else if (compiler is MySqlCompiler)
                {
                    Action actInsert = () => compiler.Compile(insert);
                    actInsert.Should().Throw<NotSupportedException>().WithMessage("*RETURNING clause is not natively supported in MySQL*");
                }
                else if (compiler is SqlServerCompiler)
                {
                    Action actInsert = () => compiler.Compile(insert);
                    actInsert.Should().Throw<NotSupportedException>().WithMessage("*SQL Server does not support ON CONFLICT syntax*");
                }
                else
                {
                    var resInsert = compiler.Compile(insert);
                    resInsert.Sql.Should().Contain("INSERT INTO");
                }

                compiler.Compile(insertDefault).Sql.Should().Contain("INSERT INTO");
                compiler.Compile(update).Sql.Should().Contain("UPDATE");
                compiler.Compile(delete).Sql.Should().Contain("DELETE");
                compiler.Compile(cte).Sql.Should().Contain("WITH");
                compiler.Compile(cteRecursive).Sql.Should().Contain("WITH");
                compiler.Compile(window).Sql.Should().Contain("WINDOW");
                compiler.Compile(windowPage).Sql.Should().Contain("ROW_NUMBER()");
                compiler.Compile(windowPageNoSelect).Sql.Should().Contain("ROW_NUMBER()");
                compiler.Compile(union).Sql.Should().Contain("UNION");
                
                // Test exception throwing to hit catch block and activity error
                Action act = () => compiler.Compile(new ExceptionQuery());
                act.Should().Throw<Exception>();
            }
        }
        
        [Fact]
        public void Compile_EdgeCases_CoversAllBranches()
        {
            var insertExprReturn = new InsertQuery<DummyEntity>()
                .Into("dummy_table")
                .Values(new DummyEntity())
                .Returning(x => x.Id);

            var deleteExprReturn = new DeleteQuery<DummyEntity>()
                .Delete("dummy_table")
                .WhereAll()
                .Returning(x => x.Id);

            var updateStringReturnAndFrom = new UpdateQuery<DummyEntity>()
                .Update("dummy_table")
                .Set(x => x.Name, "test")
                .From("other_table")
                .Join("join_table", "jt", "jt.id = other_table.id")
                .WhereAll()
                .Returning("id");

            var selectEmpty = new SelectQuery<DummyEntity>().Select(System.Array.Empty<string>());
            
            var selectMethod = new SelectQuery<DummyEntity>().Select(x => x.ToString());
            
            var selectNullString = new SelectQuery<DummyEntity>().Select((string)null!);

            var selectDistinct = new SelectQuery<DummyEntity>().Select(x => x.Id).Distinct();
            
            var rawSelectDistinct = new SelectQuery<DummyEntity>().RawSelect((System.FormattableString)$"1").Distinct();

            var selectFromAlias = new SelectQuery<DummyEntity>().From("table", "alias");

            var havingOr = new SelectQuery<DummyEntity>()
                .Having(x => x.Id > 1)
                .OrHaving(x => x.Id < 5)
                .OrHaving((FormattableString)$"MAX(id) < {10}");

            var subquery = new SelectQuery<DummyEntity>().Select("id");
            var subqueryJoinNoCondition = new SelectQuery<DummyEntity>()
                .From("table")
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryJoinNode(EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Cross, subquery, "AliasC", null));
            var subqueryJoinWithCondition = new SelectQuery<DummyEntity>()
                .From("table")
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryJoinNode(EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Inner, subquery, "AliasC", "table.id = AliasC.id"));

            var queryAliasNode = new SelectQuery<DummyEntity>().Select("id").Alias("query_alias");

            var orderByConstant = new SelectQuery<DummyEntity>().OrderBy(x => 1);
            var orderByObjectConstant = new SelectQuery<DummyEntity>().OrderBy(x => (object)1);
            var orderByNull = new SelectQuery<DummyEntity>().OrderBy((System.Linq.Expressions.Expression<System.Func<DummyEntity, object>>)null!);

            var returningEmpty = new InsertQuery<DummyEntity>().Into("dummy_table").Values(new DummyEntity()).Returning(System.Array.Empty<string>());
            
            var onConflictEmptyTargets = new InsertQuery<DummyEntity>().Into("table").Values(new DummyEntity()).OnConflict(System.Array.Empty<string>());
            var onConflictNullTargets = new InsertQuery<DummyEntity>().Into("table").Values(new DummyEntity()).OnConflict((string[])null!);

            var defaultCompiler = new DefaultCompiler();

            // Explicit assertions without swallowing exceptions
            _compilers[0].Compile(insertExprReturn).Sql.Should().Contain("INSERT INTO");
            _compilers[0].Compile(deleteExprReturn).Sql.Should().Contain("DELETE");
            _compilers[0].Compile(updateStringReturnAndFrom).Sql.Should().Contain("UPDATE");
            _compilers[0].Compile(selectEmpty).Sql.Should().Contain("SELECT *");
            _compilers[0].Compile(selectMethod).Sql.Should().Contain("SELECT");
            _compilers[0].Compile(selectNullString).Sql.Should().Be("SELECT");
            _compilers[0].Compile(selectDistinct).Sql.Should().Contain("DISTINCT");
            _compilers[0].Compile(rawSelectDistinct).Sql.Should().Contain("DISTINCT");
            _compilers[0].Compile(selectFromAlias).Sql.Should().Contain("FROM \"table\" AS \"alias\"");
            _compilers[0].Compile(havingOr).Sql.Should().Match("*HAVING*OR*OR*");
            _compilers[0].Compile(subqueryJoinNoCondition).Sql.Should().Be("SELECT * FROM \"table\" CROSS JOIN (SELECT \"id\") AS \"AliasC\"");
            _compilers[0].Compile(subqueryJoinWithCondition).Sql.Should().Be("SELECT * FROM \"table\" INNER JOIN (SELECT \"id\") AS \"AliasC\" ON table.id = AliasC.id");
            _compilers[0].Compile(queryAliasNode).Sql.Should().Be("SELECT \"id\" AS \"query_alias\"");
            defaultCompiler.Compile(orderByConstant).Sql.Should().Match("*ORDER BY*");
            defaultCompiler.Compile(orderByObjectConstant).Sql.Should().Match("*ORDER BY*");
            defaultCompiler.Compile(orderByNull).Sql.Should().Contain("SELECT *");
            defaultCompiler.Compile(returningEmpty).Sql.Should().Contain("INSERT INTO");
            defaultCompiler.Compile(onConflictEmptyTargets).Sql.Should().Contain("ON CONFLICT");
            defaultCompiler.Compile(onConflictNullTargets).Sql.Should().Contain("ON CONFLICT");
        }
        
        [Fact]
        public void CompileSelect_ExactStrings_KillsFormattingMutations()
        {
            var defaultCompiler = new DefaultCompiler();
            
            // Explicit SelectNode
            var selectExplicit = new SelectQuery<DummyEntity>().Select("Id").From("dummy_table");
            defaultCompiler.Compile(selectExplicit).Sql.Should().Be("SELECT \"Id\" FROM \"dummy_table\"");

            // Implicit SelectNode
            var selectImplicit = new SelectQuery<DummyEntity>().From("dummy_table");
            defaultCompiler.Compile(selectImplicit).Sql.Should().Be("SELECT * FROM \"dummy_table\"");
            
            // CTE Recursive Exact
            var cteRecursive = new SelectQuery<DummyEntity>().RecursiveCTE("cte_name", selectImplicit).From("cte_name");
            defaultCompiler.Compile(cteRecursive).Sql.Should().Be("WITH RECURSIVE \"cte_name\" AS (SELECT * FROM \"dummy_table\") SELECT * FROM \"cte_name\"");
            
            // CTE Exact
            var cte = new SelectQuery<DummyEntity>().CTE("cte_name", selectImplicit).From("cte_name");
            defaultCompiler.Compile(cte).Sql.Should().Be("WITH \"cte_name\" AS (SELECT * FROM \"dummy_table\") SELECT * FROM \"cte_name\"");
            
            // Window Page with Select
            var windowPage = new SelectQuery<DummyEntity>().Select("Id").From("dummy_table").WindowPage(2, 20, "Id", false);
            defaultCompiler.Compile(windowPage).Sql.Should().Be("WITH __wp AS (SELECT \"Id\", ROW_NUMBER() OVER(ORDER BY \"Id\" ASC) AS __row_num FROM \"dummy_table\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 21 AND 40");
            
            // Window Page without Select
            var windowPageNoSelect = new SelectQuery<DummyEntity>().From("dummy_table").WindowPage(2, 20, "Id", true);
            defaultCompiler.Compile(windowPageNoSelect).Sql.Should().Be("WITH __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"Id\" DESC) AS __row_num FROM \"dummy_table\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 21 AND 40");
            
            // Distinct
            var selectDistinct = new SelectQuery<DummyEntity>().Select("Id").Distinct().From("dummy_table");
            defaultCompiler.Compile(selectDistinct).Sql.Should().Be("SELECT DISTINCT \"Id\" FROM \"dummy_table\"");
            
            // GroupBy and Having
            var groupByHaving = new SelectQuery<DummyEntity>().Select("Id").From("dummy_table").GroupBy("Id").Having(x => x.Id > 1);
            defaultCompiler.Compile(groupByHaving).Sql.Should().Be("SELECT \"Id\" FROM \"dummy_table\" GROUP BY \"Id\" HAVING (id > @p0)");
        }
        
        [Fact]
        public void Compile_MultipleNodes_UsesLast()
        {
            var defaultCompiler = new DefaultCompiler();
            
            var insertQuery = new InsertQuery<DummyEntity>()
                .Into("table1")
                .AddNode(new InsertNode("table2", System.Array.Empty<string>())) // This one is the last one
                .Values(new DummyEntity());
            
            var updateQuery = new UpdateQuery<DummyEntity>()
                .Update("table1")
                .AddNode(new UpdateNode("table2"))
                .Set(x => x.Name, "test")
                .WhereAll();

            var deleteQuery = ((DeleteQuery<DummyEntity>)new DeleteQuery<DummyEntity>()
                .Delete("table1"))
                .AddNode(new DeleteNode("table2"))
                .WhereAll();

            var insertRes = defaultCompiler.Compile(insertQuery);
            insertRes.Sql.Should().Contain("table2");
            insertRes.Sql.Should().NotContain("table1");

            var updateRes = defaultCompiler.Compile(updateQuery);
            updateRes.Sql.Should().Contain("table2");
            updateRes.Sql.Should().NotContain("table1");

            var deleteRes = defaultCompiler.Compile(deleteQuery);
            deleteRes.Sql.Should().Contain("table2");
            deleteRes.Sql.Should().NotContain("table1");
        }
        
        private class InspectingQuery : IAstQuery
        {
            public string? Tag => null;
            private readonly IAstQuery _inner;
            public SqlResult? Result { get; private set; }

            public InspectingQuery(IAstQuery inner) => _inner = inner;

            public IReadOnlyList<ISqlNode> Nodes
            {
                get
                {
                    Result = new DefaultCompiler().Compile(_inner);
                    return _inner.Nodes;
                }
            }

            public SqlResult Build(ISqlCompiler compiler)
            {
                Result = _inner.Build(compiler);
                return Result;
            }
        }

        [Fact]
        public void Compile_EdgeCases_ThreadSqlIsIsolatedForSubqueries()
        {
            var subquery = new SelectQuery<DummyEntity>().Select("id");
            var inspectingSubquery = new InspectingQuery(subquery);

            var query = new SelectQuery<DummyEntity>()
                .From("table")
                .AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryJoinNode(EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType.Cross, inspectingSubquery, "AliasC", null));

            var compiler = new DefaultCompiler();
            compiler.Compile(query);

            inspectingSubquery.Result.Should().NotBeNull();
            inspectingSubquery.Result!.Sql.Should().Be("SELECT \"id\"", "Subquery should not contain the outer query's SQL prefix");
        }

        private class ExceptionQuery : IAstQuery
        {
        public string? Tag => null;
            IReadOnlyList<ISqlNode> IAstQuery.Nodes => throw new Exception("Test Exception");
            public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);
        }

        private class UnsupportedNode : ISqlNode
        {
            public void Accept(ISqlVisitor visitor) => visitor.VisitUnknown(this);
        }
    [Fact]
    public void Compile_DeleteWithReturning_BuildsCorrectSql()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.Delete<DummyEntity>().Where(u => u.Id == 1).Returning(u => u.Id);
        var result = compiler.Compile(query);
        result.Sql.Should().Contain("RETURNING \"id\"");
    }

    [Fact]
    public void Compile_UpdateWithReturning_BuildsCorrectSql()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.Update<DummyEntity>().Set(u => u.Name, "Test").Where(u => u.Id == 1).Returning(u => u.Id);
        var result = compiler.Compile(query);
        result.Sql.Should().Contain("RETURNING \"id\"");
    }

    [Fact]
    public void Compile_UnknownNode_Throws()
    {
        var compiler = new PostgreSqlCompiler();
        var query = new FakeQuery(new UnsupportedNode());
        Action act = () => compiler.Compile(query);
        act.Should().Throw<System.NotSupportedException>();
    }

    private class NoOpNode : ISqlNode
    {
        public void Accept(ISqlVisitor visitor) { }
    }

    [Fact]
    public void Compile_NoOpNode_DoesNotThrow()
    {
        var compiler = new PostgreSqlCompiler();
        var query = new FakeQuery(new NoOpNode());
        Action act = () => compiler.Compile(query);
        act.Should().NotThrow();
    }

}
}




