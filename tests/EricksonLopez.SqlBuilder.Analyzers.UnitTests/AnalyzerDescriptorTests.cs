// Copyright © Erickson Lopez. MIT License.
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class AnalyzerDescriptorTests
    {
        [Fact]
        public void BatchSizeExceedsMaxAnalyzer_Descriptors()
        {
            var analyzer = new BatchSizeExceedsMaxAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ELSB006", rule.Id);
            Assert.Equal("Batch size exceeds provider parameter limit", rule.Title.ToString());
            Assert.Equal("The batch size of {0} may exceed the parameter limit for common database providers (SQL Server: 2100 params, SQLite: 999 params). Consider reducing the batch size or using a native bulk strategy (SqlBulkCopyStrategy, NpgsqlCopyStrategy, MySqlBatchStrategy).", rule.MessageFormat.ToString());
            Assert.Equal("Performance", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("SQL providers have a maximum number of parameters per statement. If the batch size multiplied by the number of columns exceeds this limit, the query will fail at runtime. For SQL Server the limit is 2100 parameters. Use a native bulk strategy that bypasses the parameter limit for large datasets.", rule.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ELSB006.md", rule.HelpLinkUri);
        }

        [Fact]
        public void CartesianJoinAnalyzer_Descriptors()
        {
            var analyzer = new CartesianJoinAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL024", rule.Id);
            Assert.Equal("Potential Cartesian Join (Missing ON Condition)", rule.Title.ToString());
            Assert.Equal("The Join clause '{0}' appears to be missing an ON condition, which could lead to a Cartesian Join", rule.MessageFormat.ToString());
            Assert.Equal("Correctness", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Ensure that JOIN clauses include an ON condition to avoid Cartesian products.", rule.Description.ToString());
        }

        [Fact]
        public void DapperCompilerAnalyzer_Descriptors()
        {
            var analyzer = new DapperCompilerAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL005", rule.Id);
            Assert.Equal("Call to Dapper extensions without compiler", rule.Title.ToString());
            Assert.Equal("Ensure you have registered the compiler with DapperExtensions.RegisterCompiler", rule.MessageFormat.ToString());
            Assert.Equal("Usage", rule.Category);
            Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("To use SqlBuilder Dapper extensions you must register your DB compiler.", rule.Description.ToString());
        }

        [Fact]
        public void DeleteWithoutWhereAnalyzer_Descriptors()
        {
            var analyzer = new DeleteWithoutWhereAnalyzer();
            Assert.Equal(2, analyzer.SupportedDiagnostics.Length);

            var r1 = analyzer.SupportedDiagnostics.First(r => r.Id == "ESQL001");
            Assert.Equal("DELETE without WHERE clause", r1.Title.ToString());
            Assert.Equal("DELETE will affect the entire table because no WHERE, WhereAll, WhereExists, or WhereNotExists filter was applied", r1.MessageFormat.ToString());
            Assert.Equal("Usage", r1.Category);
            Assert.Equal(DiagnosticSeverity.Error, r1.DefaultSeverity);
            Assert.True(r1.IsEnabledByDefault);
            Assert.Equal("Avoid accidentally deleting all rows. Add a WHERE clause, or call .WhereAll() to explicitly express intent.", r1.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL001.md", r1.HelpLinkUri);

            var r2 = analyzer.SupportedDiagnostics.First(r => r.Id == "ESQL003");
            Assert.Equal("UPDATE without WHERE clause", r2.Title.ToString());
            Assert.Equal("UPDATE will affect the entire table because no WHERE, WhereAll, WhereExists, or WhereNotExists filter was applied", r2.MessageFormat.ToString());
            Assert.Equal("Usage", r2.Category);
            Assert.Equal(DiagnosticSeverity.Error, r2.DefaultSeverity);
            Assert.True(r2.IsEnabledByDefault);
            Assert.Equal("Avoid accidentally updating all rows. Add a WHERE clause, or call .WhereAll() to explicitly express intent.", r2.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL003.md", r2.HelpLinkUri);
        }

        [Fact]
        public void DialectSpecificOverloadAnalyzer_Descriptors()
        {
            var analyzer = new DialectSpecificOverloadAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL020", rule.Id);
            Assert.Equal("Capability requirement might not be met", rule.Title.ToString());
            Assert.Equal("Method '{0}' requires capability '{1}' which might not be supported by the intended compiler", rule.MessageFormat.ToString());
            Assert.Equal("Correctness", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Checks if methods decorated with [RequiresCapability] are used safely.", rule.Description.ToString());
        }
        [Fact]
        public void DynamicIdentifierAnalyzer_Descriptors()
        {
            var analyzer = new DynamicIdentifierAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ELSB004", rule.Id);
            Assert.Equal("Dynamic SQL identifier without allowlist", rule.Title.ToString());
            Assert.Equal("The SQL identifier '{0}' is built dynamically without an allowlist check. Validate against a known-safe list before passing to SQL APIs to prevent injection.", rule.MessageFormat.ToString());
            Assert.Equal("Security", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Passing dynamically-built table names, column names, or schema names into SQL APIs without an explicit allowlist creates SQL injection risk. Use strongly-typed entity models (Sql.From<T>()) or validate against a compile-time or runtime allowlist.", rule.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ELSB004.md", rule.HelpLinkUri);
        }

        [Fact]
        public void JoinConditionAnalyzer_Descriptors()
        {
            var analyzer = new JoinConditionAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL006", rule.Id);
            Assert.Equal("Incompatible types in Join", rule.Title.ToString());
            Assert.Equal("The types of the properties compared in the JOIN do not match ({0} vs {1}). This can cause execution errors or performance issues.", rule.MessageFormat.ToString());
            Assert.Equal("Correctness", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Ensure that the columns compared in a JOIN are of the same type.", rule.Description.ToString());
        }

        [Fact]
        public void LargeOffsetAnalyzer_Descriptors()
        {
            var analyzer = new LargeOffsetAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL008", rule.Id);
            Assert.Equal("Large Offset detected", rule.Title.ToString());
            Assert.Equal("The Offset {0} is greater than 10,000. Consider using keyset pagination (Seek) for better performance.", rule.MessageFormat.ToString());
            Assert.Equal("Performance", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Avoid using very large OFFSETs, as the database must process and discard rows.", rule.Description.ToString());
        }

        [Fact]
        public void LikeWildcardAnalyzer_Descriptors()
        {
            var analyzer = new LikeWildcardAnalyzer();
            Assert.Equal(2, analyzer.SupportedDiagnostics.Length);

            var r1 = analyzer.SupportedDiagnostics.First(r => r.Id == "ESQL009");
            Assert.Equal("Use of LIKE without wildcards", r1.Title.ToString());
            Assert.Equal("The string '{0}' does not contain wildcards ('%' or '_'). Use '=' for exact searches.", r1.MessageFormat.ToString());
            Assert.Equal("Performance", r1.Category);
            Assert.Equal(DiagnosticSeverity.Warning, r1.DefaultSeverity);
            Assert.True(r1.IsEnabledByDefault);
            Assert.Equal("Avoid using LIKE if you are not searching for patterns with wildcards.", r1.Description.ToString());

            var r2 = analyzer.SupportedDiagnostics.First(r => r.Id == "ESQL010");
            Assert.Equal("Use of LIKE with leading wildcard", r2.Title.ToString());
            Assert.Equal("The string '{0}' starts with a '%' wildcard. This prevents the use of B-Tree indexes and causes full table scans.", r2.MessageFormat.ToString());
            Assert.Equal("Performance", r2.Category);
            Assert.Equal(DiagnosticSeverity.Warning, r2.DefaultSeverity);
            Assert.True(r2.IsEnabledByDefault);
            Assert.Equal("Consider using Full-Text Search or trigram indexes if you require suffix or leading wildcard searches.", r2.Description.ToString());
        }

        [Fact]
        public void MergeQueryAnalyzer_Descriptors()
        {
            var analyzer = new MergeQueryAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL026", rule.Id);
            Assert.Equal("Generic Sql.Merge<T>() is removed in v2.0", rule.Title.ToString());
            Assert.Equal("Sql.Merge<T>() has been removed in v2.0. Use dialect-native .OnConflict() (PostgreSQL, MySQL, SQLite) or Sql.Raw() (SQL Server, Oracle) instead.", rule.MessageFormat.ToString());
            Assert.Equal("Design", rule.Category);
            Assert.Equal(DiagnosticSeverity.Error, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Generic cross-dialect MERGE statements suffer from major semantic differences and subtle concurrency bugs across providers. Use dialect-specific OnConflict APIs for PostgreSQL, MySQL, and SQLite, or Sql.Raw() for SQL Server and Oracle.", rule.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/ESQL026.md", rule.HelpLinkUri);
        }

        [Fact]
        public void MissingColumnAnalyzer_Descriptors()
        {
            var analyzer = new MissingColumnAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("SQL0009", rule.Id);
            Assert.Equal("Non-existent column in entity", rule.Title.ToString());
            Assert.Equal("The column '{0}' does not exist as a property in entity '{1}'", rule.MessageFormat.ToString());
            Assert.Equal("Usage", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Columns specified in Select, OrderBy or GroupBy must correspond to properties of the mapped entity.", rule.Description.ToString());
        }

        [Fact]
        public void MissingIndexAnalyzer_Descriptors()
        {
            var analyzer = new MissingIndexAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL007", rule.Id);
            Assert.Equal("OrderBy on unindexed column", rule.Title.ToString());
            Assert.Equal("The property '{0}' does not have the [Indexed] attribute. Sorting by this column can cause a full table scan and affect performance.", rule.MessageFormat.ToString());
            Assert.Equal("Performance", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Avoid sorting by columns that are not indexed in the database.", rule.Description.ToString());
        }

        [Fact]
        public void MissingSourceGeneratorAnalyzer_Descriptors()
        {
            var analyzer = new MissingSourceGeneratorAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL021", rule.Id);
            Assert.Equal("Source Generator package not referenced", rule.Title.ToString());
            Assert.Equal("Type '{0}' has [SqlEntity] but the Source Generator package is not referenced. Add EricksonLopez.SqlBuilder.SourceGenerators to restore AOT-safe code generation and eliminate runtime reflection.", rule.MessageFormat.ToString());
            Assert.Equal("Usage", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Without EricksonLopez.SqlBuilder.SourceGenerators, entity metadata is resolved at runtime via reflection. This is incompatible with NativeAOT and may cause incorrect behaviour in trimmed builds. Add the package and mark the class as 'partial'.", rule.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers.md#ESQL021", rule.HelpLinkUri);
        }

        [Fact]
        public void QueryPerformanceAnalyzer_Descriptors()
        {
            var analyzer = new QueryPerformanceAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL004", rule.Id);
            Assert.Equal("Use of ToString() or similar in SQL Expressions", rule.Title.ToString());
            Assert.Equal("The method {0} cannot be translated to SQL natively or affects performance", rule.MessageFormat.ToString());
            Assert.Equal("Performance", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Avoid using ToString(), ToUpper(), ToLower() inside Where or Having lambda expressions.", rule.Description.ToString());
        }

        [Fact]
        public void RawStringOverloadAnalyzer_Descriptors()
        {
            var analyzer = new RawStringOverloadAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL011", rule.Id);
            Assert.Equal("Unsafe Sql.Raw(string) overload", rule.Title.ToString());
            Assert.Equal("Use Sql.Raw(FormattableString) instead of Sql.Raw(string) to prevent SQL injection. The string overload does not parameterize interpolated values.", rule.MessageFormat.ToString());
            Assert.Equal("Security", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("The Sql.Raw(string, object?) overload is marked deprecated and unsafe. Replace with Sql.Raw($\"...\") (FormattableString) so that all interpolated values become named parameters automatically.", rule.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/esql011.md", rule.HelpLinkUri);
        }

        [Fact]
        public void RedundantWhereAnalyzer_Descriptors()
        {
            var analyzer = new RedundantWhereAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("SQL0004", rule.Id);
            Assert.Equal("Redundant Where clause", rule.Title.ToString());
            Assert.Equal("The condition '{0}' in the Where clause appears to be tautological or redundant", rule.MessageFormat.ToString());
            Assert.Equal("Maintainability", rule.Category);
            Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Avoid statically defined WHERE clauses like '1=1'.", rule.Description.ToString());
        }

        [Fact]
        public void RetryInsideTransactionAnalyzer_Descriptors()
        {
            var analyzer = new RetryInsideTransactionAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL012", rule.Id);
            Assert.Equal("Retry pipeline wraps transaction commit", rule.Title.ToString());
            Assert.Equal("The resilience pipeline ExecuteAsync lambda contains a CommitAsync call. This pattern can cause data corruption. Wrap the entire transaction (BeginUnitOfWork → Execute → Commit) inside the retry lambda instead.", rule.MessageFormat.ToString());
            Assert.Equal("Usage", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("A Polly retry pipeline must never wrap only the commit of a transaction. On retry, the transaction would be re-attempted from mid-state, causing duplicate inserts or corrupted data. Place the entire transactional unit (begin, execute, commit) inside the pipeline.ExecuteAsync lambda. See ADR-016.", rule.Description.ToString());
            Assert.Equal("https://github.com/ericksonlopezf/dotnet-sql-builder/blob/main/docs/analyzers/esql012.md", rule.HelpLinkUri);
        }

        [Fact]
        public void SelectStarAnalyzer_Descriptors()
        {
            var analyzer = new SelectStarAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("SQL0003", rule.Id);
            Assert.Equal("Avoid explicit SELECT *", rule.Title.ToString());
            Assert.Equal("The use of '*' in RawSelect or Select(\"*\") is not recommended for performance and maintainability reasons", rule.MessageFormat.ToString());
            Assert.Equal("Performance", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Explicitly specify the desired columns instead of using '*'.", rule.Description.ToString());
        }

        [Fact]
        public void SqlKataMigrationAnalyzer_Descriptors()
        {
            var analyzer = new SqlKataMigrationAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL025", rule.Id);
            Assert.Equal("Migrate SqlKata Query to SqlBuilder", rule.Title.ToString());
            Assert.Equal("Replace 'new Query(...)' with 'Sql.From(...)'", rule.MessageFormat.ToString());
            Assert.Equal("Migration", rule.Category);
            Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Automatically migrate legacy SqlKata Query instantiations to EricksonLopez.SqlBuilder.", rule.Description.ToString());
        }

        [Fact]
        public void SyncOnUiThreadAnalyzer_Descriptors()
        {
            var analyzer = new SyncOnUiThreadAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL023", rule.Id);
            Assert.Equal("Synchronous execution on UI thread", rule.Title.ToString());
            Assert.Equal("Avoid synchronous Dapper execution (like ToResult) on UI threads. Use ToResultAsync instead.", rule.MessageFormat.ToString());
            Assert.Equal("Performance", rule.Category);
            Assert.Equal(DiagnosticSeverity.Warning, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Synchronous database queries can block the UI thread and degrade application responsiveness.", rule.Description.ToString());
        }

        [Fact]
        public void TypeMapRegistrationAnalyzer_Descriptors()
        {
            var analyzer = new TypeMapRegistrationAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL022", rule.Id);
            Assert.Equal("Verify Dapper Type Maps registration on startup", rule.Title.ToString());
            Assert.Equal("Ensure you have registered required Dapper Type Maps for your custom value types", rule.MessageFormat.ToString());
            Assert.Equal("Usage", rule.Category);
            Assert.Equal(DiagnosticSeverity.Info, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Missing Type Maps can lead to runtime mapping errors.", rule.Description.ToString());
        }

        [Fact]
        public void UnsafeStringConcatenationAnalyzer_Descriptors()
        {
            var analyzer = new UnsafeStringConcatenationAnalyzer();
            Assert.Single(analyzer.SupportedDiagnostics);
            var rule = analyzer.SupportedDiagnostics[0];
            Assert.Equal("ESQL002", rule.Id);
            Assert.Equal("Unsafe string concatenation in SQL", rule.Title.ToString());
            Assert.Equal("Use of '+' concatenation instead of safe '$' interpolation in SQL method", rule.MessageFormat.ToString());
            Assert.Equal("Security", rule.Category);
            Assert.Equal(DiagnosticSeverity.Error, rule.DefaultSeverity);
            Assert.True(rule.IsEnabledByDefault);
            Assert.Equal("Detects the use of string concatenation in Raw methods which could lead to SQL Injection.", rule.Description.ToString());
        }
    }
}
