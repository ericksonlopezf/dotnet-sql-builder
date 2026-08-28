# SQL Contracts

SQL Contracts are a mechanism in EricksonLopez.SqlBuilder to verify the structural shape and output of SQL queries without needing to execute them against a live database. They enable true deterministic snapshot testing and regression checking.

## What is a QueryContract?

A QueryContract represents the deterministic shape of a query:
- The tables being queried (FROM and JOINs)
- The columns being selected
- A deterministic Fingerprint hash that identifies the query's structural AST

By extracting a QueryContract from an IAstQuery, you can assert that the query performs the expected joins and selects the correct columns, even if the underlying runtime parameters change.

## Using Query Contracts in Tests

You can use SnapshotAssert.VerifyContract(query) or SnapshotAssert.MatchesContract(query, expectedFingerprint) in your xUnit tests to verify that the query matches an expected baseline contract.

`csharp
[Fact]
public async Task Test_Complex_Query_Contract()
{
    var query = Sql.From<Order>()
        .InnerJoin("Customers", "c", "c.Id = Order.CustomerId")
        .Select("Order.Id", "c.Name");

    // Creates a snapshot file of the contract (tables, columns, fingerprint)
    await SnapshotAssert.VerifyContract(query);
}
`

## Guarantees
- **Deterministic**: The fingerprint is guaranteed to be identical for any two identical ASTs, regardless of parameter values.
- **Fast**: Computing the contract does not require invoking the SQL Compiler or generating SQL strings.
