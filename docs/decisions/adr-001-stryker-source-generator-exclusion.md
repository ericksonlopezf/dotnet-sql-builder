# ADR 001: Exclusion of Source Generator from Mutation Testing

## Status
Accepted

## Context
The `EricksonLopez.SqlBuilder` project is a high-performance SQL Builder library featuring a custom Roslyn Source Generator (`EricksonLopez.SqlBuilder.SourceGenerators`) that serves as the core engine for generating SQL code at compile-time. We rely on Stryker.NET for mutation testing to ensure code quality, targeting strict metrics (Line Coverage ≥ 100%, Branch Coverage ≥ 100%, Mutation Score ≥ 100%, Method Coverage ≥ 100%).

However, when Stryker attempts to mutate the AST and compilation context of the Source Generator project, it encounters catastrophic infrastructure failures. Specifically:
1. **Deadlocks in Test Host:** The `vstest.console.exe` instances hang completely (0% CPU usage) during mutation test runs.
2. **Infinite Loops:** Mutating the logic that generates the AST or evaluates Roslyn symbols induces unrecoverable loops or socket IPC deadlocks between the test runner and the Stryker orchestrator.
3. **Buildalyzer Constraints:** Stryker leverages Buildalyzer to evaluate the MSBuild workspace. Since Source Generators interact heavily with the compiler internals (`Microsoft.CodeAnalysis`), dynamic injection of mutants during the workspace evaluation phase results in `MSBuild` evaluation hangs.

Despite attempts to isolate the execution (using single concurrency, restricted timeouts, and no caching), the architectural mismatch between Stryker's in-memory mutant injection and the Roslyn Source Generator compilation lifecycle makes mutation testing of this specific project technically unviable with current tooling.

## Decision
We will explicitly exclude the `EricksonLopez.SqlBuilder.SourceGenerators` project from Stryker mutation testing.
- The Source Generator will still maintain ≥ 100% Line, Branch, and Method Coverage via standard xUnit tests.
- Mutation testing metrics will apply to 100% of the runtime library (`EricksonLopez.SqlBuilder`), database extensions (Dapper, SqlServer, PostgreSql, etc.), and all abstractions.

## Consequences
- **Positive:** The Stryker mutation testing process will complete reliably without hanging, allowing us to enforce 100% Mutation Score on the rest of the library.
- **Negative:** The Source Generator project's logic will not be guarded by mutation testing. We must rely exclusively on rigorous unit tests, integration tests, and manual code reviews to ensure its correctness.
- **Implementation:** The `stryker-config.json` is updated to exclude `src/EricksonLopez.SqlBuilder.SourceGenerators/**/*.cs` in the `mutate` property and removes the `EricksonLopez.SqlBuilder.SourceGenerators.UnitTests.csproj` from the `test-projects` array to prevent Stryker from evaluating its workspace.
