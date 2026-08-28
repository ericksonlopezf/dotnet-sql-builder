# ADR-041: Institutionalization of Osherove Test Naming Pattern and IDE1006 Local Suppression

## Status

Accepted

## Date

2026-08-18

## Context

Clear, readable, and structured naming for automated test suites is critical for developer experience, continuous integration (CI) failure triage, and maintaining tests as living documentation.

Roy Osherove's testing pattern (`[UnitOfWork]_[Scenario]_[ExpectedBehavior]` or `Method_Scenario_Result`) provides an established, unambiguous syntax that formats tests as declarative specifications of system invariants.

In standard C# style guidelines, Roslyn analyzer rule `IDE1006` enforces PascalCase naming conventions for methods without underscores.

## Problem

Applying production naming conventions (strict PascalCase without underscores) to test methods makes test names compressed, dense, and difficult to parse quickly (e.g., `CompileWhenQualifiedTableHasMultipleDotsShouldEscapeEachSegmentSeparately` vs `Compile_WhenQualifiedTableHasMultipleDots_ShouldEscapeEachSegmentSeparately`).

Test methods are not API entrypoints consumed by external callers; they are execution targets discovered reflectively by test runners (xUnit) whose names are emitted directly into CI consoles, test explorer trees, and artifact failure reports. Enforcing `IDE1006` on test suites creates artificial friction and encourages cryptic or overly terse method names.

## Decision

1. **Standardize on the Osherove Naming Pattern across all test suites**:
   - All test methods across unit, integration, and architecture test projects must follow the three-part format:
     `[UnitOfWork]_[Scenario]_[ExpectedBehavior]`
   - **UnitOfWork**: The method, property, or component under test (e.g., `Compile`, `GetMetadata`, `BulkInsertAsync`).
   - **Scenario**: The pre-condition, input state, or context (e.g., `WhenWhereClauseContainsNullConstant`, `WithMultipleDotsInIdentifier`, `ConcurrentAccessFromMultipleThreads`).
   - **ExpectedBehavior**: The observable invariant, result, or exception (e.g., `ShouldGenerateIsNullPredicate`, `ThrowsInvalidOperationException`, `ShouldBeThreadSafeAndDeterministic`).

2. **Suppress Roslyn IDE1006 locally and exclusively for test projects**:
   - Configure `.editorconfig` with `[tests/**/*.cs] dotnet_diagnostic.IDE1006.severity = none`.
   - Configure `Directory.Build.props` to include `IDE1006` in `<NoWarn>` for test assemblies (`MSBuildProjectName.Contains('Tests')`).
   - Preserve strict `IDE1006` enforcement across all production libraries in `src/`.

## Decision Drivers

- **Living Specifications**: Test names in CI console outputs and failure reports read as self-explanatory natural sentences.
- **Fast Failure Triage**: Developers can identify the broken unit, scenario, and expectation directly from test logs without navigating to the source code.
- **Team Consistency**: Provides an objective, enforceable convention for all current and future contributors.
- **Zero Production Contamination**: Scoped strictly to non-shipping test assemblies.
