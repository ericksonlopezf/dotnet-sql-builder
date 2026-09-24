## Description

Please include a summary of the change and which issue is fixed (if any).
Include relevant motivation and context.

## Affected Packages

Please check all packages that are affected by this PR:
- [ ] `EricksonLopez.SqlBuilder` (Core)
- [ ] `EricksonLopez.SqlBuilder.Abstractions`
- [ ] `EricksonLopez.SqlBuilder.Aot`
- [ ] `EricksonLopez.SqlBuilder.Analyzers`
- [ ] `EricksonLopez.SqlBuilder.Dapper`
- [ ] `EricksonLopez.SqlBuilder.Dapper.Aot`
- [ ] `EricksonLopez.SqlBuilder.MariaDb`
- [ ] `EricksonLopez.SqlBuilder.MySql`
- [ ] `EricksonLopez.SqlBuilder.OpenTelemetry`
- [ ] `EricksonLopez.SqlBuilder.Oracle`
- [ ] `EricksonLopez.SqlBuilder.Pagination`
- [ ] `EricksonLopez.SqlBuilder.PostgreSql`
- [ ] `EricksonLopez.SqlBuilder.SourceGenerators`
- [ ] `EricksonLopez.SqlBuilder.Sqlite`
- [ ] `EricksonLopez.SqlBuilder.SqlServer`
- [ ] `EricksonLopez.SqlBuilder.Testing`

## Checklist

Before submitting this PR, please verify the following:
- [ ] I have performed a self-review of my own code.
- [ ] I have updated the `CHANGELOG.md` under `[Unreleased]` (if applicable).
- [ ] I have added/updated unit tests or integration tests (Docker required for database integration tests).
- [ ] Local build passes (`dotnet build dotnet-sql-builder.slnx --configuration Release`).
- [ ] Local tests pass (`dotnet test dotnet-sql-builder.slnx --configuration Release`).
- [ ] Architecture compliance script passes (`powershell -ExecutionPolicy Bypass -File ./scripts/verify-compliance.ps1`).
- [ ] If changing AST/Compilers, I ran Stryker mutation testing and maintained the **95%** mutation score threshold.
- [ ] If changing core/compilers, I ran Benchmarks and confirmed no performance regressions (0 B heap allocation invariant on hot paths and <=5% latency regression).
