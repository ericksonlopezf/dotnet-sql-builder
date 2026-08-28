# Level 1: Quick Start

## Overview

Demonstrates minimal setup and basic CRUD operations using `EricksonLopez.SqlBuilder` and Dapper with SQLite.

## Key APIs Covered

- `[SqlEntity("tableName")]` and `[DatabaseGenerated]` annotations.
- `DapperExtensions.RegisterCompiler<TConnection>()` for global compiler registration.
- `Sql.Insert<T>()` and `.Returning(x => x.Id)`.
- `Sql.From<T>()` with `.Where(x => x.Id == ...)`.
- `Sql.Delete<T>()` with `.Where(...)`.
