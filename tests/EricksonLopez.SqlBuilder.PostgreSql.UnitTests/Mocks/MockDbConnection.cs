// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Annotations;

#nullable disable
#pragma warning disable CA1010
namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests.Mocks
{
    [ExcludeFromCodeCoverage]
    public class MockDbConnection : DbConnection
    {
        public override string ConnectionString { get => ""; set { } }
        public override string Database => "TestDb";
        public override string DataSource => "Memory";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;

        public List<MockDbCommand> Commands { get; } = new();

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new MockDbTransaction(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            var cmd = new MockDbCommand(this);
            Commands.Add(cmd);
            return cmd;
        }
    }

    [ExcludeFromCodeCoverage]
    public class MockDbCommand : DbCommand
    {
        public MockDbCommand(DbConnection connection)
        {
            DbConnection = connection;
        }

        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new MockDbParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new InvalidOperationException("Synchronous ExecuteNonQuery called on MockDbCommand");
        public override object? ExecuteScalar() => 1;
        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new MockDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
        
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    [ExcludeFromCodeCoverage]
    public class MockDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = new();
        public override int Count => _parameters.Count;
        public override object SyncRoot => this;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values) => _parameters.AddRange((IEnumerable<DbParameter>)values);
        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) { }
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => throw new NotImplementedException();
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }

    [ExcludeFromCodeCoverage]
    public class MockDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = "";
        public override int Size { get; set; }
        public override string SourceColumn { get; set; } = "";
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    [ExcludeFromCodeCoverage]
    public class MockDbTransaction : DbTransaction
    {
        public MockDbTransaction(DbConnection connection, IsolationLevel il)
        {
            DbConnection = connection;
            IsolationLevel = il;
        }

        public override IsolationLevel IsolationLevel { get; }
        protected override DbConnection? DbConnection { get; }

        public override void Commit() { }
        public override void Rollback() { }
    }
}



