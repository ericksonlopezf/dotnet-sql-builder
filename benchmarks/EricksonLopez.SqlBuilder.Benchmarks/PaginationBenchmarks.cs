// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Benchmarks
{
    /// <summary>
    /// Benchmarks for pagination queries.
    /// </summary>
    [MemoryDiagnoser]
    public class PaginationBenchmarks
    {
        private static readonly SqlServerCompiler SqlServerCompiler = new SqlServerCompiler();
        private static readonly PostgreSqlCompiler PostgreSqlCompiler = new PostgreSqlCompiler();

        /// <summary>Gets or sets the page number.</summary>
        [Params(1, 10, 100)]
        public int PageNumber { get; set; }

        /// <summary>Gets or sets the page size.</summary>
        [Params(10, 50, 100)]
        public int PageSize { get; set; }

        /// <summary>Benchmarks the SQL Server pagination queries using OFFSET/FETCH.</summary>
        [Benchmark]
        public string SqlServer_OffsetFetch()
        {
            var query = Sql.From<Customer>()
                .OrderBy(c => c.Id)
                .Offset((PageNumber - 1) * PageSize)
                .Limit(PageSize);
                
            return query.Build(SqlServerCompiler).Sql;
        }

        /// <summary>Benchmarks the PostgreSQL pagination queries using LIMIT/OFFSET.</summary>
        [Benchmark]
        public string PostgreSql_LimitOffset()
        {
            var query = Sql.From<Customer>()
                .OrderBy(c => c.Id)
                .Offset((PageNumber - 1) * PageSize)
                .Limit(PageSize);
                
            return query.Build(PostgreSqlCompiler).Sql;
        }
    }
}


