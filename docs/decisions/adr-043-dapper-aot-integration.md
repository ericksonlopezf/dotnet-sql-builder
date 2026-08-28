# ADR-043: Dapper.AOT Integration Package Strategy

## Status
Accepted

## Date
2026-08-19

## Context
With the rise of NativeAOT in .NET 8, 9, and 10, Dapper introduced AOT source generators while `EricksonLopez.SqlBuilder` pioneered `AotQueryExecutor` and `IDataReaderMapper<T>` for reflection-free AST and execution mapping.

Users building high-performance microservices and serverless apps need a dedicated integration package that bridges Dapper with the typed SQL builder in full AOT mode without bringing reflection dependencies.

## Decision
Create the package `EricksonLopez.SqlBuilder.Dapper.Aot`:
1. Targets `net8.0`, `net9.0`, `net10.0` with `<IsAotCompatible>true</IsAotCompatible>`.
2. Provides asynchronous and synchronous execution extensions (`AotQueryAsync<T>`, `AotQueryFirstOrDefaultAsync<T>`, `AotExecuteAsync`) over `System.Data.Common.DbConnection` without using runtime reflection.
3. Leverages `IDataReaderMapper<T>` and `AotQueryExecutor`.

## Consequences
- ✅ NativeAOT zero-reflection execution path directly on `DbConnection`.
- ✅ Clean boundary: users not using AOT stay on `EricksonLopez.SqlBuilder.Dapper`; AOT users consume `EricksonLopez.SqlBuilder.Dapper.Aot`.
