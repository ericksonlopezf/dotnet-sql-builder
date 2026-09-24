# 11. Real World Use Cases

### 1. Dynamic Search APIs
When an API receives multiple filter parameters and they must be added conditionally:

```csharp
var q = Sql.From<Product>().Where(p => p.IsActive);

// SelectQuery is immutable: always reassign the returned instance!
if (request.MinPrice.HasValue)
    q = q.Where(p => p.Price >= request.MinPrice.Value);

if (!string.IsNullOrEmpty(request.SearchTerm))
    q = q.Where(p => p.Name.Contains(request.SearchTerm));

var products = await conn.QueryAsync<Product>(q);
```

### 2. Multitenant Support
Adding a tenant filter to a shared base query factory using parameterized interpolation:

```csharp
public SelectQuery<T> BaseQuery<T>(int tenantId) where T : class, new() 
    => Sql.From<T>().Where($"TenantId = {tenantId}");
```
