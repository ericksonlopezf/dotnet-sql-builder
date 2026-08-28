# Migration Guide

## Migrating from v1 to v2

If upgrading from v1:
- `QueryBuilder.Select` has been replaced by the static entrypoint `Sql.From<T>()`.
- Transaction management is standardized through `IUnitOfWork`.
- Mutable query builders have been replaced with immutable AST query records.
