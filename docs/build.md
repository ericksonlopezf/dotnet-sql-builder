# Build System & MSBuild Properties

This document describes the MSBuild configuration, central build properties, strong naming, and SourceLink integration for the EricksonLopez.SqlBuilder repository.

---

## Central Build Configuration

All MSBuild properties are centralized in [`Directory.Build.props`](../Directory.Build.props), which is automatically imported by every project in the solution. This ensures consistency across all 17+ source projects without per-project duplication.

---

## Version Management

```xml
<VersionPrefix>1.0.0</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

- **Versioning scheme:** Semantic Versioning (`MAJOR.MINOR.PATCH`)
- **`VersionPrefix`** is the authoritative version source for all packages and GitHub Release tags
- **`VersionSuffix`** is empty for stable releases; would contain a pre-release label (e.g., `beta.1`) for pre-release builds
- The CI workflow reads this value dynamically to create the GitHub Release tag (`v{VersionPrefix}`)

---

## NuGet Package Metadata

All packages share these metadata properties:

| Property | Value |
|----------|-------|
| `Authors` | Erickson López |
| `Copyright` | Copyright © 2026 Erickson López |
| `PackageLicenseExpression` | MIT |
| `PackageProjectUrl` | https://ericksonlopez.dev/sql-builder |
| `PackageReadmeFile` | README.md (embedded in each `.nupkg`) |
| `PackageIcon` | icon.png (embedded in each `.nupkg`) |
| `PackageTags` | sql, sql-builder, fluent, linq, dapper, query, postgresql, mysql, sqlserver, dotnet |

---

## Compiler & Analysis Settings

| Property | Value | Purpose |
|----------|-------|---------|
| `LangVersion` | `latest` | Always uses the latest C# language version |
| `Nullable` | `enable` | Full nullable reference types enabled |
| `ImplicitUsings` | `disable` | Explicit `using` directives required across all projects |
| `TreatWarningsAsErrors` | `true` | All warnings are errors in Release; relaxed in CI with `-p:TreatWarningsAsErrors=false` |
| `WarningLevel` | `5` | Maximum warning level |
| `AnalysisLevel` | `latest-recommended` | Latest Roslyn analysis ruleset |
| `EnforceCodeStyleInBuild` | `true` | `.editorconfig` style rules enforced at build time |
| `EnablePackageValidation` | `true` | Package API surface compared against shipped baseline on each pack |
| `GenerateDocumentationFile` | `true` | XML documentation generated for IntelliSense and tooling |

### Public API Enforcement

`Microsoft.CodeAnalysis.PublicApiAnalyzers` is applied to all source projects (except Analyzers, SourceGenerators, and test projects). Any undeclared addition or removal of public API surface fails the build, enforced via `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` in each project directory.

---

## Strong Naming

Strong naming is **conditional** — assemblies are signed only when the private key file is present at build time:

```xml
<SignAssembly Condition="Exists('$(MSBuildThisFileDirectory)EricksonLopez.snk')">true</SignAssembly>
<AssemblyOriginatorKeyFile Condition="Exists('$(MSBuildThisFileDirectory)EricksonLopez.snk')">
    $(MSBuildThisFileDirectory)EricksonLopez.snk
</AssemblyOriginatorKeyFile>
```

- The `.snk` file is **not committed** to the repository (it is in `.gitignore`) — it is stored as an encrypted secret in GitHub Actions
- Local development builds without the key file are unsigned
- CI/CD release builds are signed when the key is injected from the secret store
- The public key token can be inspected with `sn.exe -p EricksonLopez.snk public.snk` and `sn.exe -tp public.snk`

---

## Deterministic Builds

```xml
<Deterministic>true</Deterministic>
<ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
```

- `<Deterministic>true</Deterministic>` ensures identical source + identical toolchain → identical binary output
- `<ContinuousIntegrationBuild>` is activated automatically in GitHub Actions (where `CI=true`) and enables full path normalization in PDB files, making `.nupkg` and `.snupkg` byte-for-byte reproducible

---

## SourceLink

```xml
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
```

**`Microsoft.SourceLink.GitHub`** is applied globally via the `Directory.Build.props` `<ItemGroup>`:

```xml
<PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
```

This embeds a mapping from each binary to its source commit in the GitHub repository, enabling debuggers (Visual Studio, Rider, VS Code) to step through library source code during debugging.

Symbol packages (`.snupkg`) are published alongside `.nupkg` to NuGet.org.

---

## NuGet Audit

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>   <!-- scans transitive dependencies -->
<NuGetAuditLevel>low</NuGetAuditLevel> <!-- reports all severity levels -->
```

Every `dotnet restore` and `dotnet build` scans all direct **and transitive** NuGet dependencies against the GitHub Advisory Database for known vulnerabilities. Any match at any severity level (Low, Moderate, High, Critical) produces a build warning (or error if `TreatWarningsAsErrors=true`).

---

## Documentation XML

All source projects generate XML documentation files (`GenerateDocumentationFile=true`). These are embedded in the `.nupkg` for IDE IntelliSense support when the package is consumed.

Missing XML documentation warnings (`CS1591`) are suppressed globally since some internal types do not require public documentation.

---

## Test & Sample Project Overrides

Test and sample projects (`*.Tests`, `*.Playground`, `*.Sample`) have relaxed settings (such as documentation generation disabled and specific warning suppressions):

```xml
<GenerateDocumentationFile>false</GenerateDocumentationFile>
<NoWarn>$(NoWarn);CA1016</NoWarn>
```

They also include project references to sibling libraries (`EricksonLopez.Pagination.Abstractions`, `EricksonLopez.Result`, `EricksonLopez.SharedKernel`) from adjacent repositories — these are workspace-level dependencies used for integration testing and sample scenarios.

---

## See Also

- [`Directory.Packages.props`](../Directory.Packages.props) — Central Package Management (all pinned versions)
- [`docs/dependency-management.md`](dependency-management.md) — CPM version table and dependency policy
- [`docs/ci-cd.md`](ci-cd.md) — How the build runs in GitHub Actions
