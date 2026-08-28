// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Benchmarks
{
    /// <summary>
    /// Benchmarks for bulk operations (Insert, Update, Delete).
    /// </summary>
    [MemoryDiagnoser]
    public class BulkOperationBenchmarks
    {
        private static readonly SqlServerCompiler Compiler = new SqlServerCompiler();
        private List<Customer> _customers = new();

        /// <summary>Gets or sets the number of items in the batch.</summary>
        [Params(10, 100, 1000)]
        public int BatchSize { get; set; }

        /// <summary>Sets up the benchmark state.</summary>
        [GlobalSetup]
        public void Setup()
        {
            _customers = Enumerable.Range(1, BatchSize).Select(i => new Customer
            {
                Id = i,
                Name = $"Customer {i}",
                Email = $"customer{i}@example.com",
                IsActive = i % 2 == 0
            }).ToList();
        }

        /// <summary>Benchmarks the bulk insert query generation.</summary>
        [Benchmark]
        public string SqlBuilder_BulkInsert()
        {
            var query = Sql.Insert(_customers.First());
            return query.Build(Compiler).Sql;
        }

        /// <summary>Benchmarks the bulk update query generation.</summary>
        [Benchmark]
        public string SqlBuilder_BulkUpdate()
        {
            var ids = _customers.Select(c => c.Id).ToList();
            var query = Sql.Update<Customer>()
                .Set(c => c.IsActive, true)
                .Where(c => ids.Contains(c.Id));
            return query.Build(Compiler).Sql;
        }

        /// <summary>Benchmarks the bulk delete query generation.</summary>
        [Benchmark]
        public string SqlBuilder_BulkDelete()
        {
            var ids = _customers.Select(c => c.Id).ToList();
            var query = Sql.Delete<Customer>().Where(c => ids.Contains(c.Id));
            return query.Build(Compiler).Sql;
        }
    }
}

