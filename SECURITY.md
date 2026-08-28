# Security Policy

## Supported Versions

Only the latest release of `EricksonLopez.SqlBuilder` is actively supported for security updates.

| Version | Supported          |
| ------- | ------------------ |
| 1.1.x   | :white_check_mark: |
| < 1.1   | :x:                |

## Reporting a Vulnerability

We take the security of this project seriously. If you discover a vulnerability, please **do NOT open a public issue**.

Instead, report it via [GitHub Security Advisories](https://github.com/ericksonlopezf/dotnet-sql-builder/security/advisories/new) or by emailing **ericksonlopezf@gmail.com**.

Please include the following details in your report:

- A description of the vulnerability.
- Steps to reproduce the issue (including any PoC code).
- The package(s) and version(s) affected.
- The dialect/engine involved (if applicable).

You will receive a response within 48 hours. If the issue is confirmed, we will work with you to patch it and release a security update. You will be credited in the security advisory if you wish.

## Supply Chain Security

The `EricksonLopez.SqlBuilder` project follows these supply chain security practices:

- **Strong Naming:** All assemblies are conditionally signed using `EricksonLopez.snk` when the key file is present at build time. The private key is stored as an encrypted repository secret (`SNK_KEY`), never committed to the repository, and decoded at runtime during CI/CD.
- **NuGet Trusted Publishing (OIDC):** Packages are published to NuGet.org using GitHub OIDC token exchange via `NuGet/login@v1`. No long-lived static API keys are required or stored; the ephemeral token is scoped to the publish workflow run and expires immediately after use.
- **Sigstore Provenance Attestation:** All `.nupkg` artifacts receive a cryptographic build provenance attestation via `actions/attest-build-provenance@v2` before publication. This links each published binary to the exact repository commit and workflow run, enabling consumers to verify supply chain integrity.
- **GitHub Packages:** Published using the built-in `GITHUB_TOKEN`, which is automatically scoped to the current repository and expires after each workflow run.
- **Deterministic Builds:** All packages are built with `<Deterministic>true</Deterministic>` and `<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>` in CI, ensuring byte-for-byte reproducibility.
- **SourceLink:** All packages embed SourceLink metadata (`Microsoft.SourceLink.GitHub`) so consumers can step through library source in the debugger and verify the binary matches the published commit.
- **NuGet Audit:** `<NuGetAudit>true</NuGetAudit>` with `<NuGetAuditMode>all</NuGetAuditMode>` is enabled globally, scanning all dependencies (including transitive) for known vulnerabilities on every build.
- **Dependabot:** Configured to scan both NuGet and GitHub Actions ecosystems weekly.

## Known Security Boundaries

The `EricksonLopez.SqlBuilder` API boundary strictly assumes that **developers control the expressions and structure of the query**.

- **Safe:** All literal values passed into `.Where()`, `.Insert()`, or `.Update()` via closure variables are automatically parameterized by the dialect compilers, protecting against SQL injection.
- **Unsafe (By Design):** Dynamic raw strings passed into escape hatches (e.g., `Sql.Raw()`, or table/column names dynamically generated via string concatenation instead of strongly-typed mappings) are **not** parameterized. Users must sanitize identifiers if they originate from untrusted input.

> The Roslyn Analyzer rule **ESQL011** (Unsafe `Sql.Raw()` with non-constant string) will emit a warning when `Sql.Raw()` is called with a non-literal string argument.
