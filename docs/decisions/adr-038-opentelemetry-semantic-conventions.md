# ADR-038: OpenTelemetry Database Semantic Conventions per Dialect

## Status

Accepted

## Date

2026-08-15

## Context

OpenTelemetry semantic conventions for database spans mandate standard values for the `db.system` attribute (e.g. `mssql`, `postgresql`, `mysql`, `sqlite`, `oracle`).
Previously, `SqlBuilderInstrumentation` emitted generic `db.system = "sql"`.

## Problem

Monitoring dashboards (Grafana, Datadog, Honeycomb) could not categorize database query metrics and traces by specific provider.

## Decision

1. Multi-target `EricksonLopez.SqlBuilder.OpenTelemetry` across `net8.0`, `net9.0`, and `net10.0`.
2. Update `SqlBuilderInstrumentation.StartQueryActivity` to accept an optional `ISqlCompiler` and map compiler types to standard OpenTelemetry `db.system` tags.

## Decision Drivers

- **Observability Parity:** Standards-compliant OTel trace attributes.
- **Modern TFMs:** First-class support for .NET 8, .NET 9, and .NET 10 runtimes.
