# Testing Strategy & Quality Assurance Guide — EricksonLopez.SqlBuilder

> **Document Version**: 2.0.0  
> **Target Frameworks**: .NET 8.0, .NET 9.0, .NET 10.0  
> **Quality Gate Thresholds**: Stryker Mutation Score ≥ 95%, Line Coverage ≥ 95%, Zero Flaky Tests.

---

## 1. Executive Summary & Philosophy

`EricksonLopez.SqlBuilder` is an enterprise-grade, high-performance SQL query construction and execution engine with NativeAOT compatibility. Testing in this repository is governed by three non-negotiable principles:

1. **Pragmatism > Pure Dogma**: Tests verify observable behavior, mathematical invariants, and generated SQL syntax rather than implementation details.
2. **Zero False Positives / Zero False Negatives**: Flaky tests, swallowed exceptions (`catch (NotSupportedException) { }`), and overly permissive assertions (`MatchRegex(@"(MERGE|INSERT)")` or `NotBeEmpty()`) are strictly prohibited.
3. **Deterministic Parallelism**: All test suites run concurrently by default. Any tests accessing process-wide state (OpenTelemetry `ActivityListener`, diagnostics meters) are strictly isolated via dedicated xUnit test collections.

---

## 2. Test Architecture & Topology

The solution contains 21 specialized test projects structured across distinct architectural layers:

```
tests/
├── Unit Testing Layer (In-Memory, Zero I/O, Parallel)
│   ├── EricksonLopez.SqlBuilder.UnitTests/              # Core query builder, AST nodes, entity metadata cache
│   ├── EricksonLopez.SqlBuilder.Abstractions.UnitTests/  # Interfaces, column selection, token structs
│   ├── EricksonLopez.SqlBuilder.SqlServer.UnitTests/     # SQL Server dialect compiler & bulk merge strategy
│   ├── EricksonLopez.SqlBuilder.PostgreSql.UnitTests/   # PostgreSQL dialect, jsonb, unnest, lateral joins
│   ├── EricksonLopez.SqlBuilder.MySql.UnitTests/        # MySQL dialect compiler
│   ├── EricksonLopez.SqlBuilder.MariaDb.UnitTests/      # MariaDb dialect compiler
│   ├── EricksonLopez.SqlBuilder.Sqlite.UnitTests/       # SQLite dialect compiler
│   ├── EricksonLopez.SqlBuilder.Oracle.UnitTests/       # Oracle dialect compiler & sequences
│   ├── EricksonLopez.SqlBuilder.Dapper.UnitTests/       # Dapper parameter bridge & type handlers
│   ├── EricksonLopez.SqlBuilder.Aot.UnitTests/          # NativeAOT static metadata renderers
│   └── EricksonLopez.SqlBuilder.SourceGenerators.UnitTests/ # Roslyn incremental generator snapshot tests
│
├── Integration Testing Layer (Testcontainers, Real Databases)
│   ├── EricksonLopez.SqlBuilder.IntegrationTests/       # Multi-engine integration tests
│   ├── EricksonLopez.SqlBuilder.SqlServer.IntegrationTests/ # Real MSSQL 2022 instance in container
│   ├── EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests/ # Real PostgreSQL 16 instance in container
│   ├── EricksonLopez.SqlBuilder.MySql.IntegrationTests/     # Real MySQL 8.4 instance in container
│   ├── EricksonLopez.SqlBuilder.MariaDb.IntegrationTests/   # Real MariaDB 11 instance in container
│   ├── EricksonLopez.SqlBuilder.Sqlite.IntegrationTests/    # Real SQLite engine
│   └── EricksonLopez.SqlBuilder.Oracle.IntegrationTests/    # Real Oracle Free instance in container
│
├── Architecture & Governance Layer
│   └── EricksonLopez.SqlBuilder.ArchitectureTests/     # ArchUnitNET & NetArchTest rules (clean architecture, zero reflection)
│
└── Shared Testing Infrastructure
    └── EricksonLopez.SqlBuilder.Testing/                # ObjectMother, Fluent Builders, Mock Compilers, Domain Entities
```

---

## 3. Testing Pyramid Guidelines

```
          / \
         /   \       Architecture Tests (ArchUnitNET) -> Enforce layer boundaries, zero reflection
        /     \      Integration Tests (Testcontainers) -> Real engine query execution, transaction integrity
       /       \     Property-Based Tests (FsCheck) -> Invariant & algebraic testing across random inputs
      /         \    Snapshot Tests (Verify.Xunit) -> Complex AST compilation & Roslyn generator output
     /           \   Unit Tests (xUnit + FluentAssertions) -> Fast AST verification, compiler dialect generation
    /_____________\
```

### When to Write What:

| Test Type | Target Scope | Execution Environment | Max Duration |
| :--- | :--- | :--- | :--- |
| **Unit Test** | Individual AST nodes, Query builder methods, Compiler dialect generation, Renderers. | In-Memory | < 50ms |
| **Snapshot Test** | Complete multi-dialect compilation matrices, Source Generator output strings. | In-Memory (Verify.Xunit) | < 100ms |
| **Property Test** | Invariants (e.g. Limit/Offset math, parameter deduplication, AST immutability). | In-Memory (FsCheck) | < 200ms |
| **Integration Test** | End-to-end execution, bulk operations, dialect-specific execution quirks. | Docker Containers | < 2s |
| **Architecture Test** | Solution dependencies, Public API surface, immutability conventions. | In-Memory | < 500ms |

---

## 4. Unified Naming Convention (H11)

All test methods must adhere strictly to the three-part naming standard:

$$\text{MethodName}\_\text{Scenario}\_\text{ExpectedResult}$$

### Examples:
- ✅ `Compile_WhenInsertWithOnConflict_ShouldGenerateReturningSql()`
- ✅ `SqlEntityCache_WhenEntityIsUnannotatedPoco_ShouldThrowTypeInitializationException()`
- ✅ `WithConcurrencyToken_WhenNoExistingWhere_ShouldGenerateWhereClause()`
- ❌ `TestInsert()` *(Non-descriptive)*
- ❌ `VerifyBranches()` *(Monolithic; bundles multiple tests)*
- ❌ `CheckQueryWorks()` *(Missing scenario and expected result)*

---

## 5. Assertion Standards & Anti-Patterns (H2, H3, H4, H5)

### Rule 1: No Swallowed Exceptions
Never catch exceptions in tests unless explicitly asserting expected failure:
```csharp
// ❌ ANTI-PATTERN: Swallowing exceptions hides compiler defects
try { compiler.Compile(insert); } catch (NotSupportedException) { }

// ✅ ENTERPRISE-GRADE: Declarative dialect capability testing
if (compiler is OracleCompiler)
{
    Action act = () => compiler.Compile(insert);
    act.Should().Throw<NotSupportedException>().WithMessage("*Oracle does not support ON CONFLICT*");
}
else
{
    var result = compiler.Compile(insert);
    result.Sql.Should().Contain("INSERT INTO");
}
```

### Rule 2: Strong Semantic Assertions
Avoid weak `.Nodes.Should().NotBeEmpty()` assertions. Assert concrete AST node types and internal properties:
```csharp
// ❌ ANTI-PATTERN: Weak assertion
((IAstQuery)q.Where(x => x.Id == 1)).Nodes.Should().NotBeEmpty();

// ✅ ENTERPRISE-GRADE: Semantic node assertion
((IAstQuery)new DeleteQuery<User>().Where(x => x.Id == 1))
    .Nodes.Should().ContainSingle()
    .Which.Should().BeOfType<ExpressionWhereNode>()
    .Which.IsOr.Should().BeFalse();
```

### Rule 3: Structural Dialect Verification
When verifying complex SQL clauses (e.g., SQL Server `MERGE` or Postgres `LATERAL`), verify full clause structure rather than simple regex matches:
```csharp
// ❌ ANTI-PATTERN: Permissive regex
result.Sql.Should().MatchRegex(@"(MERGE|INSERT)");

// ✅ ENTERPRISE-GRADE: Complete structural assertions
sql.Should().Contain("MERGE INTO [customers] AS target");
sql.Should().Contain("USING #staging_customers AS source");
sql.Should().Contain("ON (target.[Id] = source.[Id])");
sql.Should().Contain("WHEN MATCHED THEN UPDATE SET");
sql.Should().Contain("WHEN NOT MATCHED BY TARGET THEN INSERT (");
```

---

## 6. Concurrency, Diagnostic State & Parallelism (H1, H10)

Process-wide resources in .NET (such as `System.Diagnostics.ActivitySource`, `ActivityListener`, `SqlBuilderDiagnostics.LogParameters`) must be isolated to prevent cross-test contamination in concurrent CI runners.

### Usage:
Any test class inspecting or modifying OpenTelemetry diagnostics or activity listeners must be marked with `[Collection("SqlBuilderDiagnosticsCollection")]` and use `SqlBuilderDiagnosticsFixture`:

```csharp
[Collection("SqlBuilderDiagnosticsCollection")]
public class SqlBuilderDiagnosticsTests
{
    private readonly SqlBuilderDiagnosticsFixture _fixture;

    public SqlBuilderDiagnosticsTests(SqlBuilderDiagnosticsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SqlCompiler_WhenCompiling_EmitsActivity()
    {
        var activities = new List<Activity>();
        using var _ = _fixture.CaptureActivities(activity => activities.Add(activity));

        var query = Sql.From<User>().Where(u => u.Username == "Test");
        var result = query.Build(new PostgreSqlCompiler());

        var activity = activities.Last(a => (string?)a.GetTagItem("db.statement") == result.Sql);
        activity.OperationName.Should().Be("SqlCompiler.Compile");
        activity.GetTagItem("sqlbuilder.query_type").Should().Be("SELECT");
    }
}
```

---

## 7. Test Data Builders & ObjectMother (H12)

Test data creation is centralized in `EricksonLopez.SqlBuilder.Testing.DataBuilders`.

### 1. ObjectMother (Static Pre-configured Entities):
- `ObjectMother.CreateUser(id, name, isActive)`
- `ObjectMother.CreateProduct(id, name, price, stock, categoryId)`
- `ObjectMother.CreateOrder(id, customerId, totalAmount, status)`
- `ObjectMother.CreateOrderItem(id, orderId, productId, quantity, unitPrice)`
- `ObjectMother.CreateCustomer(id, name, email, isActive)`
- `ObjectMother.CreateInvoice(id, orderId, amount)`
- `ObjectMother.CreatePayment(id, invoiceId, amount)`
- `ObjectMother.CreateAuditLog(id, entityName, action)`

### 2. Composable Fluent Builders:
For tests requiring custom entity mutations:
```csharp
var customUser = UserBuilder.Create()
    .WithId(42)
    .WithUsername("john_doe")
    .WithActive(true)
    .WithFailedLoginAttempts(3)
    .Build();

var customOrder = OrderBuilder.Create()
    .WithId(101)
    .WithCustomerId(42)
    .WithStatus("shipped")
    .WithTotalAmount(250.00m)
    .Build();
```

---

## 8. Mutation Testing with Stryker.NET (Deferred Quality Gate)

Mutation testing validates that tests fail when code mutations (faults) are introduced. In `EricksonLopez.SqlBuilder`, mutation testing is architected as an **asynchronous deferred quality gate for `main` and releases**, rather than a blocking Pull Request gate.

### Architecture & Execution Strategy:
1. **Pull Requests (Fast Path)**: PR CI runs restore, build, fast unit/property/snapshot tests, AOT validation, and code coverage (< 3 minutes). Stryker is **NOT executed on PRs** to eliminate merge bottlenecks.
2. **`main` Branch (Asynchronous Quality Signal)**: Upon push to `main`, `mutation-testing.yml` executes the full 15-package mutation testing matrix in parallel (`timeout-minutes: 480`), generates HTML/JSON reports, and posts the commit status `mutation-testing/stryker`.
3. **Weekly Scheduled Execution**: Runs every Monday at 04:00 UTC to catch regressions early.
4. **Manual Trigger (`workflow_dispatch`)**: Maintainers can trigger mutation testing with explicit `Basic`, `Standard`, or `Advanced` mutation levels.
5. **Release Gate (`publish.yml`)**: Before publishing packages to NuGet.org, the release workflow verifies the latest valid mutation testing result on `main` without re-running the 1-hour+ Stryker suite:
   - **Score $\ge 95\%$**: Release permitted.
   - **Score $< 95\%$**: Release blocked.
   - **Report TTL**: Maximum 7 days.
   - **Code Drift Check**: Zero modifications in `src/` between evaluated commit and release target.

### Stryker Thresholds (Single Source of Truth in `stryker-*.json`):
- **High**: `100%` (✅ **HIGH**)
- **Low**: `98%` (🟡 **LOW**)
- **Break**: `95%` (❌ **FAILED** — sole threshold that fails the build)
- **Warning Zone** ($[95\%, 98\%)$): 🟠 **WARNING** (Passes gate, highlights upcoming risk)

### Running Stryker locally:
```pwsh
dotnet tool restore
dotnet stryker --config-file stryker-config.json
```

---

## 9. CI/CD Matrix & Test Filtering

| Filter Expression | Target Stage | Description |
| :--- | :--- | :--- |
| `dotnet test --filter "Category!=Integration"` | PR Fast Gate | Executes all unit, property, snapshot, and architecture tests (< 2 min). |
| `dotnet test --filter "Category=Integration"` | Nightly / Merge Gate | Executes all Testcontainers integration tests against real databases. |
| `dotnet test --filter "FullyQualifiedName~ArchitectureTests"` | PR Architectural Gate | Enforces architecture invariants and zero reflection. |

---

## 10. Developer Onboarding (< 30 Minutes)

1. **Clone & Build**:
   ```pwsh
   git clone https://github.com/ericksonlopezf/dotnet-sql-builder.git
   cd dotnet-sql-builder
   dotnet build
   ```
2. **Run Unit Tests**:
   ```pwsh
   dotnet test --filter "Category!=Integration"
   ```
3. **Adding a New Compiler Feature**:
   - Add the AST node in `EricksonLopez.SqlBuilder.Abstractions/Nodes/`.
   - Implement the visitor method in each dialect compiler (`SqlServer`, `PostgreSql`, `MySql`, `MariaDb`, `Sqlite`, `Oracle`).
   - Add unit tests in `tests/EricksonLopez.SqlBuilder.UnitTests/Compilers/` asserting exact SQL output per dialect.
   - Run snapshot tests and verify generated outputs:
     ```pwsh
     dotnet test --filter "FullyQualifiedName~Snapshot"
     ```
