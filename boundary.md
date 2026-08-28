# Architectural Boundary Specification: EricksonLopez.SqlBuilder.Abstractions

## 1. Purpose
`EricksonLopez.SqlBuilder.Abstractions` defines the AST contracts, query builder interfaces, and dialect abstractions for safe, parameterized, zero-allocation SQL query construction in high-performance .NET applications.

## 2. Owns
- `ISqlBuilder`, `ISqlQuery`, `ISqlCommand`.
- `ISqlDialect` interface.
- AST node interfaces (`IExpressionNode`, `ISelectClause`, `IWhereClause`, `IJoinClause`).
- Parameterized SQL representation (`SqlTemplate`, `SqlParameterCollection`).

## 3. Does Not Own
- Concrete SQL compilation engine (`EricksonLopez.SqlBuilder`).
- Provider-specific SQL dialect implementations (`EricksonLopez.SqlBuilder.PostgreSql`, `SqlServer`, `MySql`, `Oracle`, `Sqlite`, `MariaDb`).
- Dapper execution integration (`EricksonLopez.SqlBuilder.Dapper`).
- AOT source generator (`EricksonLopez.SqlBuilder.SourceGenerators`).

## 4. Allowed Dependencies
- **.NET BCL only**.
- Peer L0 `EricksonLopez.Result` where formalized via ADR-002.

## 5. Forbidden Dependencies
- Database driver SDKs (`Npgsql`, `Microsoft.Data.SqlClient`, `MySqlConnector`, `Oracle.ManagedDataAccess`).
- `Dapper`, `Microsoft.EntityFrameworkCore`.

## 6. Who Can Depend On It
- `EricksonLopez.SqlBuilder` (L4).
- `EricksonLopez.SqlBuilder.*` (dialect packages).
- `EricksonLopez.Specification.Sql` (L2).
- `EricksonLopez.DapperExtensions` (L4).

## 7. Public API Rules
- All SQL fragments must be fully parameterized; string concatenation of raw values is strictly prohibited.

## 8. AOT Expectations
- `IsAotCompatible=true`.

## 9. Trimming Expectations
- `IsTrimmable=true`.

## 10. Provider Isolation
- 100% database-agnostic. Dialect-specific keywords and syntax rules are confined to provider packages.

## 11. Testing Isolation
- AST verification doubles live in `EricksonLopez.SqlBuilder.Testing`.
