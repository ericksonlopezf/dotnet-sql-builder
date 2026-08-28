// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using static VerifyXunit.Verifier;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlCompilerSnapshotTests
{
    [Fact]
    public Task Verify_All_Queries()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var queries = new List<string>();

        void Add(IAstQuery query)
        {
            var res = compiler.Compile(query);
            queries.Add(res.Sql);
        }

        // Select
        Add(new SelectQuery<DummyEntity>().Select("Id", "Name").From("dummy_table"));
        Add(new SelectQuery<DummyEntity>().Select(x => x.Id).Distinct().From("dummy_table"));
        Add(new SelectQuery<DummyEntity>().From("dummy_table"));
        Add(new SelectQuery<DummyEntity>().AddNode(new SelectNode(Array.Empty<string>(), false)));
        
        // ExpressionSelectNode
        var expNode = new ExpressionSelectNode(Expression.Lambda(Expression.Constant(1), Expression.Parameter(typeof(DummyEntity), "x")), false);
        Add(new SelectQuery<DummyEntity>().AddNode(expNode));
        var expNodeDistinct = new ExpressionSelectNode(Expression.Lambda(Expression.Constant(1), Expression.Parameter(typeof(DummyEntity), "x")), true);
        Add(new SelectQuery<DummyEntity>().AddNode(expNodeDistinct));
        
        // RawSelectNode
        Add(new SelectQuery<DummyEntity>().AddNode(new RawSelectNode("1", null, false)));
        Add(new SelectQuery<DummyEntity>().AddNode(new RawSelectNode("2", Array.Empty<object?>(), true)));
        Add(new SelectQuery<DummyEntity>().AddNode(new RawSelectNode("3", new object[] { 1 }, false)));
        
        // WindowPageNode
        Add(new SelectQuery<DummyEntity>().WindowPage(2, 10, "id", false));
        Add(new SelectQuery<DummyEntity>().WindowPage(2, 10, "id", true));
        
        // WindowNode
        Add(new SelectQuery<DummyEntity>().Window("win1", new[] { "id" }, new[] { "id" }));
        
        // SetOperationNode
        var q1 = new SelectQuery<DummyEntity>().From("t1");
        var q2 = new SelectQuery<DummyEntity>().From("t2");
        Add(new SelectQuery<DummyEntity>().From("t1").Union(q2));
        Add(new SelectQuery<DummyEntity>().From("t1").UnionAll(q2));
        Add(new SelectQuery<DummyEntity>().From("t1").Intersect(q2));
        Add(new SelectQuery<DummyEntity>().From("t1").Except(q2));
        
        // Joins
        Add(new SelectQuery<DummyEntity>().From("t1").Join("t2", "t2", "t1.id = t2.id"));
        Add(new SelectQuery<DummyEntity>().From("t1").LeftJoin("t2", "t2", "t1.id = t2.id"));
        Add(new SelectQuery<DummyEntity>().From("t1").RightJoin("t2", "t2", "t1.id = t2.id"));
        Add(new SelectQuery<DummyEntity>().From("t1").CrossJoin("t2", "t2"));
        Add(new SelectQuery<DummyEntity>().From("t1").AddNode(new SubqueryJoinNode(JoinType.Inner, q2, "sq", "sq.id = t1.id")));
        Add(new SelectQuery<DummyEntity>().From("t1").AddNode(new RawJoinNode("NATURAL JOIN t2", null)));
        
        // Where
        Add(new SelectQuery<DummyEntity>().From("t1").Where(x => x.Id == 1));
        Add(new SelectQuery<DummyEntity>().From("t1").Where(x => x.Id == 1).Or(x => x.Id == 2));
        Add(new SelectQuery<DummyEntity>().From("t1").AddNode(new RawWhereNode("id = 1", null, false)));
        Add(new SelectQuery<DummyEntity>().From("t1").AddNode(new RawWhereNode("id = 2", null, true)));
        
        // GroupBy / Having
        Add(new SelectQuery<DummyEntity>().From("t1").GroupBy("id").Having(x => x.Id > 1));
        Add(new SelectQuery<DummyEntity>().From("t1").GroupBy("id").Having(x => x.Id > 1).OrHaving(x => x.Id > 2));
        Add(new SelectQuery<DummyEntity>().From("t1").GroupBy("id").AddNode(new RawHavingNode("id > 3", null, false)));
        Add(new SelectQuery<DummyEntity>().From("t1").GroupBy("id").AddNode(new RawHavingNode("id > 4", null, true)));
        
        // OrderBy
        Add(new SelectQuery<DummyEntity>().From("t1").OrderBy(x => (object)x.Id).OrderByDescending(x => (object)x.Name));
        Add(new SelectQuery<DummyEntity>().From("t1").AddNode(new RawOrderByNode("id ASC NULLS FIRST")));
        Add(new SelectQuery<DummyEntity>().From("t1").OrderBy(x => (object)x.Id)); // Unary

        // Unnest
        Add(new SelectQuery<DummyEntity>().AddNode(new UnnestNode(new object[] { new[] { 1, 2 } }, "unnest_alias")));

        // QueryAlias
        Add(new SelectQuery<DummyEntity>().AddNode(new QueryAliasNode("qa")));

        // CTE
        Add(new SelectQuery<DummyEntity>().CTE("cte1", q2).From("cte1"));
        Add(new SelectQuery<DummyEntity>().RecursiveCTE("cte2", q2).From("cte2"));

        // Insert
        Add(new InsertQuery<DummyEntity>().Into("t1").Values(new DummyEntity { Id = 1, Name = "A" }));
        Add(new InsertQuery<DummyEntity>().Into("t1").DefaultValues());
        Add(new InsertQuery<DummyEntity>().Into("t1").AddNode(expNode));
        Add(new InsertQuery<DummyEntity>().Into("t1").Returning("id", "name"));
        Add(new InsertQuery<DummyEntity>().Into("t1").Returning(x => x.Id));
        Add(new InsertQuery<DummyEntity>().Into("t1").Returning(x => new { x.Id, x.Name }));

        // OnConflict
        Add(new InsertQuery<DummyEntity>().Into("t1").OnConflict("id").DoNothing());
        Add(new InsertQuery<DummyEntity>().Into("t1").OnConflict(x => (object)x.Id).DoUpdate((System.FormattableString)$"Name = EXCLUDED.Name"));
        Add(new InsertQuery<DummyEntity>().Into("t1").OnConflict("id", "name").DoUpdate((System.FormattableString)$"Name = EXCLUDED.Name"));
        Add(new InsertQuery<DummyEntity>().Into("t1").AddNode(new OnConflictNode(new[] { "id" }, "UPDATE", null, null)));
        
        // Update
        Add(new UpdateQuery<DummyEntity>().Update("t1").Set(x => x.Name, "B").Where(x => x.Id == 1));
        Add(new UpdateQuery<DummyEntity>().Update("t1").AddNode(new SetNode("Name", null, "EXPR", null)));
        Add(new UpdateQuery<DummyEntity>().Update("t1").Where(x => x.Id == 1));

        // Delete
        Add((IAstQuery)new DeleteQuery<DummyEntity>().Delete("t1").Where(x => x.Id == 1));
        Add((IAstQuery)new DeleteQuery<DummyEntity>().Delete("t1").WhereAll().Returning("id"));

        // DistinctOn
        Add(new SelectQuery<DummyEntity>().AddNode(new DistinctOnNode(new[] { "id", "name" })));

        return Verify(string.Join("\n", queries));
    }
}







