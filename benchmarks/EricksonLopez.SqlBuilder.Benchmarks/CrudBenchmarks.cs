// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Dapper;
using EricksonLopez.SqlBuilder.SqlServer;

namespace EricksonLopez.SqlBuilder.Benchmarks
{
    /// <summary>
    /// Benchmarks for basic CRUD operations.
    /// </summary>
    [MemoryDiagnoser]
    public class CrudBenchmarks
    {
        private static readonly SqlServerCompiler Compiler = new SqlServerCompiler();
        private static readonly Customer SampleCustomer = new Customer
        {
            Id = 1,
            Name = "John Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        /// <summary>Benchmarks the insert query generation.</summary>
        [Benchmark]
        public string SqlBuilder_Insert()
        {
            var query = Sql.Insert(SampleCustomer);
            return query.Build(Compiler).Sql;
        }

        /// <summary>Benchmarks the update query generation.</summary>
        [Benchmark]
        public string SqlBuilder_Update()
        {
            var query = Sql.Update<Customer>()
                .Set(c => c.Name, SampleCustomer.Name)
                .Set(c => c.Email, SampleCustomer.Email)
                .Set(c => c.IsActive, SampleCustomer.IsActive)
                .Where(c => c.Id == SampleCustomer.Id);
            return query.Build(Compiler).Sql;
        }

        /// <summary>Benchmarks the delete query generation.</summary>
        [Benchmark]
        public string SqlBuilder_Delete()
        {
            var query = Sql.Delete<Customer>().Where(c => c.Id == SampleCustomer.Id);
            return query.Build(Compiler).Sql;
        }
    }
}


