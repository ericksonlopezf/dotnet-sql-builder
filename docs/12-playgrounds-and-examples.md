# 12. Playgrounds and Examples

The ecosystem contains a `samples` folder that provides runnable projects ready to test each database engine without the need to set up complex configurations (Requires Docker).

Available:
- `samples/EricksonLopez.SqlBuilder.Samples` (Interactive Showcase with 10 progressive levels of integration)
- `samples/Playgrounds/PostgreSql/PostgreSqlPlayground.csproj`
- `samples/Playgrounds/SqlServer/SqlServerPlayground.csproj`
- `samples/Playgrounds/MySql/MySqlPlayground.csproj`
- `samples/Playgrounds/Sqlite/SqlitePlayground.csproj`
- `samples/Playgrounds/Oracle/OraclePlayground.csproj`

Each Playground has an automatic initialization script, seeds 10,000 records, and tests all aspects of `EricksonLopez.SqlBuilder`.

To test them, for example SQL Server: 
```bash
dotnet run --project samples/Playgrounds/SqlServer/SqlServerPlayground.csproj
```
