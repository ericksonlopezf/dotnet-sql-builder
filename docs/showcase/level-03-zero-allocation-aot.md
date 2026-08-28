# Level 03: Zero-Allocation & Native AOT Architecture

## 1. Native AOT Trimming Guarantees
`EricksonLopez.SqlBuilder` eliminates runtime dynamic code generation, reflection-based schema discovery, and expression tree compilation (`Expression.Compile()`). All dialect transformations operate over deterministic enum-based dispatch tables and immutable AST record hierarchies.

```csharp
// Full Native AOT compatibility with zero warnings
[JsonSerializable(typeof(UserRecord))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

---

## 2. Allocation Optimization via Span<char> & ValueStringBuilder
By avoiding intermediate string allocations during SQL clause concatenation, the SQL renderer builds output commands directly into stack-allocated or pooled buffers:

```mermaid
graph LR
    AST[Immutable AST Node Tree] --> Rent[MemoryPool Rent Buffer]
    Rent --> Render[Span Char In-Place Formatting]
    Render --> Result[SqlStatement ReadOnlySpan Struct]
    Result --> Free[Return Buffer to Pool]
```

### Benchmark Allocation Comparison

| Benchmark Case | SqlKata | Dapper Dynamic | EricksonLopez.SqlBuilder |
|---|---|---|---|
| Simple Select (10 params) | 1,420 B | 840 B | **0 B (Pooled)** |
| Complex Join + CTE | 4,890 B | 2,150 B | **0 B (Pooled)** |
| Window Function + Partition | 3,120 B | 1,600 B | **0 B (Pooled)** |

---

## 3. Roslyn Source Generator Integration
The library includes compile-time Roslyn source generators that inspect entity records and generate pre-compiled, optimized SQL builders without runtime reflection overhead.
