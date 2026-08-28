# EricksonLopez.SqlBuilder — Executable Showcase

> **Official executable documentation of the library.**  
> Each level is a complete, compilable sample demonstrating real library APIs.

[![Build](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml/badge.svg)](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../../LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com)

---

## Level Index

| Level | Directory | Description |
|-------|-----------|-------------|
| 0 | [`Level00_Conceptual`](Level00_Conceptual/README.md) | What is it? Why does it exist? Comparison with alternatives |
| 1 | [`Level01_QuickStart`](Level01_QuickStart/) | Installation, minimal configuration, first usage |
| 2 | [`Level02_FullConfiguration`](Level02_FullConfiguration/) | Full options, ITypeHandler, serialization |
| 3 | [`Level03_RealUseCases`](Level03_RealUseCases/) | CTE, Window Functions, Merge, Pagination |
| 4 | [`Level04_AdvancedIntegration`](Level04_AdvancedIntegration/) | Joins, SubqueryJoin, CASE, InsertFrom |
| 5 | [`Level05_Processing`](Level05_Processing/) | BulkInsert, Streaming, AOT, Cancellation |
| 6 | [`Level06_ErrorHandling`](Level06_ErrorHandling/) | Concurrency, Retry, Dead-letter, Backoff |
| 7 | [`Level07_Scalability`](Level07_Scalability/) | Cursor pagination, Seek-based, WindowPage |
| 8 | [`Level08_Customization`](Level08_Customization/) | ITypeHandler, IParameterManager, ISqlFilter, ISqlCompiler |
| 9 | [`Level09_Extensions`](Level09_Extensions/README.md) | SqlResult, GetFingerprint, ProjectTo, ToResultAsync, Sql.Raw |
| 10 | [`Level10_EnterpriseArchitecture`](Level10_EnterpriseArchitecture/) | DI, Repository, CQRS, CTE, Multi-Compiler |

---

## High-Level Architecture

```mermaid
graph TB
    subgraph "Public API — Core"
        SQL["Sql.From&lt;T&gt;() / Sql.Insert / Sql.Update / Sql.Delete"]
        SQ["SelectQuery&lt;T&gt;"]
        IQ["InsertQuery&lt;T&gt;"]
        UQ["UpdateQuery&lt;T&gt;"]
        DQ["DeleteQuery&lt;T&gt;"]
        SQL --> SQ & IQ & UQ & DQ
    end

    subgraph "Compilation"
        COMP["ISqlCompiler"]
        SQLITE["SqliteCompiler"]
        PG["PostgreSqlCompiler"]
        SS["SqlServerCompiler"]
        ORA["OracleCompiler"]
        COMP --> SQLITE & PG & SS & ORA
    end

    subgraph "Execution — Dapper Integration"
        EXT["ConnectionSqlExtensions"]
        QA["QueryAsync&lt;T&gt;"]
        EA["ExecuteAsync"]
        QP["QueryPagedAsync"]
        BI["BulkInsertAsync"]
        STREAM["ToStreamAsync"]
        RESULT["ToResultAsync"]
        EXT --> QA & EA & QP & BI & STREAM & RESULT
    end

    subgraph "Source Generators — AOT"
        SG["SqlEntityGenerator"]
        FG["FilterGenerator"]
        MG["MultiMapDescriptorGenerator"]
    end

    SQ & IQ & UQ & DQ --> COMP
    COMP --> EXT
    SG & FG & MG -.->|"Generates code at build time"| SQ
```

---

## Primary Flow

```mermaid
sequenceDiagram
    participant App as Application
    participant Builder as Query Builder<br/>(SelectQuery&lt;T&gt;)
    participant Compiler as ISqlCompiler
    participant Dapper as Dapper Extensions
    participant DB as Database

    App->>Builder: Sql.From&lt;T&gt;().Where(...).OrderBy(...).Limit(10)
    Note over Builder: Builds immutable AST<br/>without executing
    App->>Builder: .Build(compiler) [optional]
    Builder->>Compiler: Compile(ast, paramManager)
    Compiler-->>App: SqlResult { Sql, Parameters }

    App->>Dapper: connection.QueryAsync&lt;T&gt;(query)
    Dapper->>Compiler: Implicitly compiles using registered compiler
    Dapper->>DB: Executes SQL + Parameters
    DB-->>Dapper: DataReader
    Dapper-->>App: IEnumerable&lt;T&gt;
```

---

## Query Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Construction: Sql.From&lt;T&gt;()
    Construction --> Composition: .Where() / .OrderBy() / .Join()
    Composition --> Composition: Immutable — every method returns a new instance
    Composition --> Compilation: .Build(compiler) or implicit via Dapper
    Compilation --> Parameterization: IParameterManager extracts values → @p0, @p1
    Parameterization --> SQL_Ready: SqlResult { Sql: "SELECT...", Parameters: {...} }
    SQL_Ready --> Execution: Dapper.QueryAsync / ExecuteAsync
    Execution --> [*]: Materialized result
```

---

## Dependency Map

```mermaid
graph LR
    CORE["EricksonLopez.SqlBuilder<br/>(Core)"]
    ABST["EricksonLopez.SqlBuilder.Abstractions"]
    DAP["EricksonLopez.SqlBuilder.Dapper"]
    SQLITE["EricksonLopez.SqlBuilder.Sqlite"]
    PG["EricksonLopez.SqlBuilder.PostgreSql"]
    SS["EricksonLopez.SqlBuilder.SqlServer"]
    ORA["EricksonLopez.SqlBuilder.Oracle"]
    AOT["EricksonLopez.SqlBuilder.Aot"]
    SG["EricksonLopez.SqlBuilder.SourceGenerators"]
    OTEL["EricksonLopez.SqlBuilder.OpenTelemetry"]
    RESIL["EricksonLopez.SqlBuilder.Dapper.Resilience"]
    UOW["EricksonLopez.SqlBuilder.Dapper.UnitOfWork"]
    MM["EricksonLopez.SqlBuilder.Dapper.MultiMap"]

    ABST --> CORE
    DAP --> CORE
    SQLITE --> CORE
    PG --> CORE
    SS --> CORE
    ORA --> CORE
    AOT --> CORE
    OTEL --> CORE
    RESIL --> DAP
    UOW --> DAP
    MM --> DAP
    SG -.->|"Source Generator (Analyzer)"| CORE
```

---

## Bulk Processing Pipeline

```mermaid
graph LR
    DATA["List&lt;T&gt;<br/>(500+ records)"]
    BULK["Sql.BulkInsert&lt;T&gt;()"]
    QUERY["InsertQuery&lt;T&gt;<br/>(multi-row INSERT)"]
    COMPILER["ISqlCompiler"]
    DB[("Database")]

    DATA --> BULK --> QUERY --> COMPILER --> DB

    subgraph "Alternative: Streaming"
        STREAM["SelectQuery&lt;T&gt;<br/>.ToStreamAsync(connection)"]
        AE["IAsyncEnumerable&lt;T&gt;"]
        PROC["Process item by item<br/>(zero in-memory buffering)"]
        STREAM --> AE --> PROC
    end
```

---

## Error Handling & Concurrency

```mermaid
graph TD
    UPDATE["UpdateQuery&lt;T&gt;<br/>.WithConcurrencyToken(v => v.Version, expectedValue)"]
    EXEC["ExecuteWithConcurrencyCheckAsync&lt;T&gt;(connection)"]
    CHECK{"Does version match?"}
    OK["rowsAffected > 0<br/>Success"]
    FAIL["DbConcurrencyException<br/>Concurrency conflict"]
    RETRY["Retry Strategy<br/>(Exponential Backoff)"]
    DL["Dead Letter / Compensation"]

    UPDATE --> EXEC --> CHECK
    CHECK -->|"Yes"| OK
    CHECK -->|"No"| FAIL
    FAIL --> RETRY
    RETRY -->|"Max attempts reached"| DL
    RETRY -->|"Retry"| UPDATE
```

---

## Pagination: Offset vs Seek

```mermaid
graph LR
    subgraph "Offset Pagination (ToPagedListAsync)"
        OP["Sql.From&lt;T&gt;()<br/>.Paginate(page, size)"]
        OC["Separate COUNT(*) query"]
        OL["IPagedList&lt;T&gt; { Page, TotalPages, TotalCount }"]
        OP --> OC --> OL
    end

    subgraph "Seek / Cursor Pagination (WindowPage)"
        SP["Sql.From&lt;T&gt;()<br/>.WindowPage&lt;TKey&gt;(cursor, size)"]
        SL["IPagedList&lt;T&gt; { HasNextPage, HasPreviousPage }"]
        SP --> SL
    end
```

---

## Running the Showcase

```bash
# From repository root
cd samples/EricksonLopez.SqlBuilder.Samples
dotnet run
```

The showcase executes all levels sequentially using SQLite in-memory. No database installation is required.

---

## Relationship with Official Documentation

| This Showcase | Documentation in `/docs/` |
|---------------|---------------------------|
| Level00_Conceptual | [`docs/getting-started.md`](../../docs/getting-started.md) |
| Level01–Level02 | [`docs/api-reference.md`](../../docs/api-reference.md) |
| Level03–Level04 | [`docs/cookbook.md`](../../docs/cookbook.md) |
| Level05–Level07 | [`docs/performance.md`](../../docs/performance.md) |
| Level08 | [`docs/architecture.md`](../../docs/architecture.md) |
| Level09 | [`docs/api-reference.md`](../../docs/api-reference.md) |
| Level10 | [`docs/architecture.md`](../../docs/architecture.md) |

---

> **Note**: This project is the **official reference implementation** of the library.  
> Every public API documented here is extracted directly from the codebase.  
> It contains zero fictional APIs or pseudo-code examples.
