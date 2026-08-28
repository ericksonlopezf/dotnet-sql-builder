# Feature Implementation: P1-F002 - SqlEntityCache Non-AOT Fallback Safety Guard

## Metadata
* **ID:** `P1-F002`
* **Title:** `SqlEntityCache<T>` Safety Guard and Generic Constraint Simplification
* **Layer / Component:** `EricksonLopez.SqlBuilder` (`SqlEntityCache.cs`)
* **Priority:** P1 (AOT Safety & Compiler Warning Elimination)
* **Status:** `COMPLETED`
* **Test Coverage:** Automated unit tests in `tests/EricksonLopez.SqlBuilder.UnitTests`

---

## 1. Context & Motivation
Previously, `SqlEntityCache<T>` included an unnecessary `[DynamicallyAccessedMembers]` attribute on its generic parameter that was causing IL2091 trim warnings on call sites when types without trim annotations were passed.
Furthermore, in NativeAOT mode, `[SqlEntity]` source-generated metadata provides zero-reflection table and column mappings.

---

## 2. Technical Implementation
1. Simplified `SqlEntityCache<T>` constraint to `where T : new()`.
2. When `T` implements `ISqlEntity`, metadata (`TableName`, `ColumnNames`, `PropertyMap`, `IndexedColumns`) is initialized via zero-allocation generated methods without reflection.
3. When `T` does not implement `ISqlEntity`, default non-reflection fallback (`type.Name.ToLower() + "s"`) is provided safely.

---

## 3. Verification & Test Evidence
All unit tests in `EricksonLopez.SqlBuilder.UnitTests` pass (667 tests).
Clean NativeAOT trim analysis across all referencing projects.
