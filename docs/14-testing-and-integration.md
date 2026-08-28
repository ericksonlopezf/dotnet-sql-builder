# 14. Testing and Integration (CI/CD)

## Unit Tests
The AST ensures that the SQL can be analyzed unit by unit. You can verify that the compiled syntax matches without needing to spin up a DB:

```csharp
[Fact]
public void GeneratesCorrectSql() 
{
    var sql = Sql.From<User>().Where(u => u.Id == 1).Build(new PostgreSqlCompiler());
    Assert.Equal("SELECT ...", sql.Sql);
}
```

## CI/CD
The ecosystem uses GitHub Actions with Testcontainers (or pure Docker services) for integration tests in PR pipelines.
