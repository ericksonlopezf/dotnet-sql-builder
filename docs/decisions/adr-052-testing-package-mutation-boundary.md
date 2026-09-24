# ADR 052: Testing Package Mutation Boundaries and Test Harness Architecture

## Status
Accepted

## Date
2026-09-24

## Context
`EricksonLopez.SqlBuilder.Testing` is an official support and test harness package distributed to consumers of the `EricksonLopez.SqlBuilder` ecosystem. It serves two distinct purposes:
1. **Assertion & Verification Engine (Pure Logic):** Provides algorithms for comparing AST nodes, normalizing SQL queries across dialects, asserting golden files, snapshot verification, and mock compilation (`QueryAssert`, `QueryComparer`, `GoldenFileAssert`, `SnapshotAssert`, `QueryTestingExtensions`, `MockSqlCompiler`, `DiagnosticActivityScope`).
2. **Integration Fixtures & Data Generators (I/O & Anemic Models):** Provides Testcontainers fixtures for real database engines (`MySqlFixture`, `SqlServerFixture`, `OracleFixture`, `PostgreSqlFixture`, `SqliteFixture`), database seeders (`TestDataSeeder`, `StandardDataset`), and anemic test domain entities (`Domain/*.cs`, `DataBuilders/*.cs`).

Prior to this decision, the package suffered from two architectural issues:
- **Absence of Dedicated Test Suite:** `EricksonLopez.SqlBuilder.Testing` was the only package in the solution without a dedicated unit test project (`tests/EricksonLopez.SqlBuilder.Testing.UnitTests`), relying on the Core test suite to exercise it incidentally.
- **Undefined Mutation Boundaries:** Running Stryker.NET indiscriminately across the package generated hundreds of false-positive survivors in integration fixtures (which require active Docker containers unavailable in unit mutation runners) and in anemic POCO properties (which have no business invariants).

## Decision
We formally establish an enterprise mutation boundary and dedicated test architecture for `EricksonLopez.SqlBuilder.Testing`:

1. **Dedicated Unit Test Project:**
   We establish `tests/EricksonLopez.SqlBuilder.Testing.UnitTests` as the official test project for all verification logic within the testing package.

2. **Mutation Testing Scope & Boundaries:**
   Stryker.NET will strictly mutate and enforce a 100% quality gate on the computable assertion and comparison engine:
   - `QueryAssert.cs`
   - `QueryComparer.cs`
   - `QueryComparerResult.cs`
   - `GoldenFileAssert.cs`
   - `SnapshotAssert.cs`
   - `QueryTestingExtensions.cs`
   - `MockSqlCompiler.cs`
   - `DiagnosticActivityScope.cs`
   - `DummyEntity.cs`
   - `ThreeColumnEntity.cs`

3. **Exclusion of I/O and Test Data Fixtures:**
   The following directories are explicitly excluded from unit mutation testing:
   - `Infrastructure/**`: Fixtures dependent on Docker / Testcontainers.
   - `Seeders/**`: Pseudo-random test data generators (Bogus).
   - `Domain/**`: Anemic DTO/POCO models.
   - `DataBuilders/**`: Test entity factory builders.
   - `Abstractions/**`: Shared test harness bases.

4. **Quality Gate Compliance:**
   The threshold policy (`high: 100`, `low: 98`, `break: 95`) applies to the defined mutation scope.

## Consequences
- **Positive:**
  - Complete isolation and decoupling of `Testing` from `Core`.
  - 100% mutation testing score achieved deterministically on all assertion algorithms.
  - Zero deadlocks, false positives, or missing coverage errors from I/O-bound database fixtures.
- **Negative:**
  - Fixtures requiring live database engines are verified through integration test suites rather than mutation testing.
