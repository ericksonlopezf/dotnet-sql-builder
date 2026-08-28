# 11. Real World Use Cases

### 1. Dynamic Search APIs
When an API receives multiple filter parameters and they must be added conditionally:

```csharp
var q = Sql.From<Product>().Where(p => p.IsActive);

if (request.MinPrice.HasValue)
    q.Where(p => p.Price >= request.MinPrice.Value);

if (!string.IsNullOrEmpty(request.SearchTerm))
    q.Where(p => p.Name.Contains(request.SearchTerm)); // AOT compiled to ILIKE / LIKE pattern

var products = await conn.QueryAsync(q, compiler);
```

### 2. Multitenant Support
Adding a Tenant filter to the entire builder:

```csharp
public SelectQuery<T> BaseQuery<T>(int tenantId) => Sql.From<T>().Where("TenantId = @TenantId").WithParam("TenantId", tenantId);
```
