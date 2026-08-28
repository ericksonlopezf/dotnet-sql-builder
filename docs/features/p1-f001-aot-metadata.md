# Feature Implementation — P1-F001: AOT Package Metadata

## Metadata

- **Feature ID:** `P1-F001`
- **Feature Name:** Add `IsAotCompatible` & Trim Analysis Metadata to AOT Packages
- **Category:** Packaging / NativeAOT
- **Phase:** Phase 1
- **Package(s):** All AOT-compatible `.csproj` projects in `src/`
- **Priority:** P1
- **Current State:** `PLANNED`
- **Target Version:** `v1.2.0`
- **ADR Reference:** [ADR-013](../decisions/adr-013-aot-guarantees.md)
- **Dependencies:** None
- **Architecture Decision:** Declare `<IsAotCompatible>true</IsAotCompatible>` and `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` on all reflection-free packages.
- **Breaking Change:** No
- **Started:** 2026-08-14
- **Last Updated:** 2026-08-14

---

## 1. Objective

Enable the .NET SDK trim and AOT analyzers at compile-time across all packages designed for NativeAOT. This allows consumers publishing with `PublishAot=true` or `PublishTrimmed=true` to receive compiler-verified trim compatibility guarantees without false-positive package warnings.

---

## 2. Current Evidence

### Source
* All core AST, visitor compilations, dialect renderers, and source generator output are built with reflection-free paths.
* `Directory.Build.props` currently lacks `<IsAotCompatible>true</IsAotCompatible>`.
* Individual package `.csproj` files do not declare AOT capability.

### Tests
* Core unit tests and architecture tests pass under .NET 8, 9, and 10.

### Documentation
* Documented in `aot-audit.md` and `technical-debt.md` (TD-002).

---

## 3. Problem

Without `<IsAotCompatible>true</IsAotCompatible>` in package metadata:
1. The Roslyn trim analyzer (`IL2026`, `IL2067`, `IL2091`) is not enforced during package compilation.
2. Downstream consumers using `dotnet publish -p:PublishAot=true` get warnings that the referenced NuGet packages have not declared AOT compatibility.

---

## 4. Why It Belongs to SqlBuilder

`EricksonLopez.SqlBuilder` is fundamentally designed as an AOT-first SQL compilation library. Providing verified package-level AOT metadata is an essential packaging and quality requirement.

---

## 5. Architecture Impact

No runtime API change. Enables compile-time trim analysis across the package build pipeline.

---

## 6. Public API Impact

None (MSBuild property change only).

---

## 7. Implementation Plan

- [x] **CP-01 Audit:** Enumerate all packages in `src/` to classify which are AOT-compatible and which are reflection-dependent.
- [x] **CP-02 Design:** Configure `<IsAotCompatible>true</IsAotCompatible>` and `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` in candidate `.csproj` files or selectively in `Directory.Build.props`.
- [x] **CP-03 Implementation:** Apply properties to the AOT-compatible project files.
- [x] **CP-04 Build & Analyzer Validation:** Verify `dotnet build` passes with zero trim/AOT warnings.
- [x] **CP-05 NativeAOT Verification:** Validate with `PublishAot=true` test.
- [x] **CP-06 Documentation:** Update `technical-debt.md` and `aot-roadmap.md`.
- [x] **CP-07 Final Validation:** Mark `P1-F001` as `COMPLETED` in `IMPLEMENTATION-index.md`.

---

## 8. Files Affected

### Modify
- `Directory.Build.props`
- `src/EricksonLopez.SqlBuilder.Abstractions/EricksonLopez.SqlBuilder.Abstractions.csproj`
- `src/EricksonLopez.SqlBuilder/EricksonLopez.SqlBuilder.csproj`
- `src/EricksonLopez.SqlBuilder.Aot/EricksonLopez.SqlBuilder.Aot.csproj`
- `src/EricksonLopez.SqlBuilder.SqlServer/EricksonLopez.SqlBuilder.SqlServer.csproj`
- `src/EricksonLopez.SqlBuilder.PostgreSql/EricksonLopez.SqlBuilder.PostgreSql.csproj`
- `src/EricksonLopez.SqlBuilder.MySql/EricksonLopez.SqlBuilder.MySql.csproj`
- `src/EricksonLopez.SqlBuilder.Sqlite/EricksonLopez.SqlBuilder.Sqlite.csproj`
- `src/EricksonLopez.SqlBuilder.Oracle/EricksonLopez.SqlBuilder.Oracle.csproj`
- `src/EricksonLopez.SqlBuilder.OpenTelemetry/EricksonLopez.SqlBuilder.OpenTelemetry.csproj`
- `src/EricksonLopez.SqlBuilder.Dapper.UnitOfWork/EricksonLopez.SqlBuilder.Dapper.UnitOfWork.csproj`
- `src/EricksonLopez.SqlBuilder.Dapper/EricksonLopez.SqlBuilder.Dapper.csproj` (IsAotCompatible=false)
- `src/EricksonLopez.SqlBuilder.Dapper.MultiMap/EricksonLopez.SqlBuilder.Dapper.MultiMap.csproj` (IsAotCompatible=false)
- `src/EricksonLopez.SqlBuilder.Dapper.Resilience/EricksonLopez.SqlBuilder.Dapper.Resilience.csproj` (IsAotCompatible=false)

---

## 9. Acceptance Criteria

1. `<IsAotCompatible>true</IsAotCompatible>` is present in all AOT package files.
2. `<IsAotCompatible>false</IsAotCompatible>` is documented/configured for `EricksonLopez.SqlBuilder.Dapper`, `EricksonLopez.SqlBuilder.Dapper.MultiMap`, and `EricksonLopez.SqlBuilder.Dapper.Resilience`.
3. Solution builds cleanly with 0 compilation errors across .NET 8, 9, 10.

---

## 10. Definition of Done

- Code modified and verified.
- Build and tests green.
- `IMPLEMENTATION-index.md` synchronized.

---

## 11. Implementation Log

### 2026-08-14
- **Action:** Implemented package metadata `<IsAotCompatible>true</IsAotCompatible>` in `Directory.Build.props` and projects.
- **Result:** Trim analyzers validated with 0 errors across all packages.
- **Status:** COMPLETED.

---

## 12. Final Status

`COMPLETED`
