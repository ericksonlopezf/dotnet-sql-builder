## Description

Please include a summary of the change and which issue is fixed (if any).
Include relevant motivation and context.

## Affected Packages
Please check all packages that are affected by this PR:
- [ ] `EricksonLopez.SqlBuilder` (Core)
- [ ] `EricksonLopez.SqlBuilder.Abstractions`
- [ ] `EricksonLopez.SqlBuilder.SqlServer`
- [ ] `EricksonLopez.SqlBuilder.PostgreSql`
- [ ] `EricksonLopez.SqlBuilder.MySql`
- [ ] `EricksonLopez.SqlBuilder.MariaDb`
- [ ] `EricksonLopez.SqlBuilder.Sqlite`
- [ ] `EricksonLopez.SqlBuilder.Oracle`
- [ ] `EricksonLopez.SqlBuilder.Dapper`
- [ ] `EricksonLopez.SqlBuilder.Dapper.Aot`
- [ ] `EricksonLopez.SqlBuilder.Aot`
- [ ] `EricksonLopez.SqlBuilder.Pagination`
- [ ] `EricksonLopez.SqlBuilder.OpenTelemetry`
- [ ] `EricksonLopez.SqlBuilder.Analyzers`
- [ ] `EricksonLopez.SqlBuilder.SourceGenerators`
- [ ] `EricksonLopez.SqlBuilder.Testing`

## Checklist

Before submitting this PR, please verify the following:
- [ ] I have performed a self-review of my own code.
- [ ] I have updated the `CHANGELOG.md` (if applicable).
- [ ] I have added/updated unit tests or integration tests (Docker required for some).
- [ ] Local build passes (`dotnet build dotnet-sql-builder.slnx -c Release`).
- [ ] Local tests pass (`dotnet test dotnet-sql-builder.slnx`).
- [ ] If changing AST/Compilers, I ran Stryker mutation testing and maintained the **95%** mutation score threshold.
- [ ] If changing core/compilers, I ran Benchmarks and confirmed no performance regressions according to the Benchmark Policy.
