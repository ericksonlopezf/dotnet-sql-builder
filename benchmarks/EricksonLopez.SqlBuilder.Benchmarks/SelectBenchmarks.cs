// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Dapper;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Benchmarks
{
    /// <summary>
    /// Benchmarks for SELECT queries.
    /// </summary>
    [MemoryDiagnoser]
    public class SelectBenchmarks
    {
        private static readonly SqlServerCompiler Compiler = new SqlServerCompiler();
        
        /// <summary>Benchmarks simple SELECT query generation.</summary>
        [Benchmark]
        public string SqlBuilder_SimpleSelect()
        {
            var query = Sql.From<Customer>().Where(c => c.IsActive == true);
            return query.Build(Compiler).Sql;
        }

        /// <summary>Benchmarks complex SELECT query generation.</summary>
        [Benchmark]
        public string SqlBuilder_ComplexSelect()
        {
            var query = Sql.From<Order>()
                .InnerJoin("Customer", "c", "c.Id = Order.CustomerId")
                .Where(o => o.TotalAmount > 1000m)
                .OrderByDescending(o => o.OrderDate);
            
            return query.Build(Compiler).Sql;
        }
        
        /// <summary>Benchmarks SELECT queries with GROUP BY.</summary>
        [Benchmark]
        public string SqlBuilder_GroupBy()
        {
            var query = Sql.From<Order>()
                .GroupBy("CustomerId")
                .Having(o => o.TotalAmount > 5000m);
                
            return query.Build(Compiler).Sql;
        }
    }
}

