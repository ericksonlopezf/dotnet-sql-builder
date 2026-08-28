// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.SqlServer;

internal sealed class EntityDataReader<T> : IDataReader where T : IStaticEntityMetadata<T>
{
    private readonly IList<T> _entities;
    private readonly int[] _activeColumnIndices;
    private int _currentIndex = -1;
    private bool _disposed;

    public EntityDataReader(IList<T> entities)
    {
        _entities = entities;
        var columns = T.GetColumns();

        var indices = new List<int>(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            indices.Add(columns[i].Index);
        }
        _activeColumnIndices = indices.ToArray();
    }

    /// <inheritdoc />
    public bool Read()
    {
        _currentIndex++;
        return _currentIndex < _entities.Count;
    }

    /// <inheritdoc />
    public int FieldCount => _activeColumnIndices.Length;

    /// <inheritdoc />
    public object GetValue(int i)
    {
        var entity = _entities[_currentIndex];
        int colIdx = _activeColumnIndices[i];
        if (T.IsNull(entity, colIdx))
        {
            return DBNull.Value;
        }

        // Bind via ParameterManager to extract the typed value
        var pm = new ParameterManager();
        T.BindParameter(entity, colIdx, pm);
        var parameters = pm.GetParameters();
        var firstVal = parameters.Values.FirstOrDefault();
        return firstVal ?? DBNull.Value;
    }

    /// <inheritdoc />
    public string GetName(int i) => T.GetColumnName(_activeColumnIndices[i]);

    /// <inheritdoc />
    public int GetOrdinal(string name)
    {
        for (int i = 0; i < _activeColumnIndices.Length; i++)
        {
            if (T.GetColumnName(_activeColumnIndices[i]) == name)
            {
                return i;
            }
        }
        return -1;
    }

    /// <inheritdoc />
    public bool IsDBNull(int i)
    {
        var entity = _entities[_currentIndex];
        return T.IsNull(entity, _activeColumnIndices[i]);
    }

    // ─── IDataReader contract (minimal implementation for SqlBulkCopy) ────────

    /// <inheritdoc />
    public int Depth => 0;
    /// <inheritdoc />
    public bool IsClosed => _disposed;
    /// <inheritdoc />
    public int RecordsAffected => -1;

    /// <inheritdoc />
    public bool NextResult() => false;
    /// <inheritdoc />
    public void Close() => _disposed = true;
    /// <inheritdoc />
    public void Dispose() => Close();

    // ─── Unsupported members (SqlBulkCopy only needs Read/GetValue/FieldCount) ─

    /// <inheritdoc />
    public string GetDataTypeName(int i) => throw new NotSupportedException();
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2093", Justification = "IDataRecord.GetFieldType is stubbed and returns object.")]
    public Type GetFieldType(int i) => typeof(object);
    /// <inheritdoc />
    public bool GetBoolean(int i) => (bool)GetValue(i);
    /// <inheritdoc />
    public byte GetByte(int i) => (byte)GetValue(i);
    /// <inheritdoc />
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    /// <inheritdoc />
    public char GetChar(int i) => (char)GetValue(i);
    /// <inheritdoc />
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public Guid GetGuid(int i) => (Guid)GetValue(i);
    /// <inheritdoc />
    public short GetInt16(int i) => (short)GetValue(i);
    /// <inheritdoc />
    public int GetInt32(int i) => (int)GetValue(i);
    /// <inheritdoc />
    public long GetInt64(int i) => (long)GetValue(i);
    /// <inheritdoc />
    public float GetFloat(int i) => (float)GetValue(i);
    /// <inheritdoc />
    public double GetDouble(int i) => (double)GetValue(i);
    /// <inheritdoc />
    public string GetString(int i) => (string)GetValue(i);
    /// <inheritdoc />
    public decimal GetDecimal(int i) => (decimal)GetValue(i);
    /// <inheritdoc />
    public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
    /// <inheritdoc />
    public IDataReader GetData(int i) => throw new NotSupportedException();
    /// <inheritdoc />
    public int GetValues(object[] values) { for (int i = 0; i < FieldCount; i++) values[i] = GetValue(i); return FieldCount; }

    public System.Data.DataTable? GetSchemaTable() => null;

    /// <inheritdoc />
    public object this[int i] => GetValue(i);
    /// <inheritdoc />
    public object this[string name] => GetValue(GetOrdinal(name));
}
