// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;

namespace EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

/// <summary>
/// Represents a bulk insert operation.
/// </summary>
internal sealed class BulkInsertOperation<T> : IBulkOperation<T> where T : IStaticEntityMetadata<T>
{
    private readonly IEnumerable<T> _entities;
    private readonly List<IColumnSelectionRule<T>> _rules;
    private readonly int _batchSize;

    public BulkInsertOperation(IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize)
    {
        _entities = entities;
        _rules = rules;
        _batchSize = batchSize;
    }

    public BulkSqlResult Build(ISqlRenderer dialect)
    {
        return dialect.RenderBulkInsert(_entities, _rules, _batchSize);
    }
}
