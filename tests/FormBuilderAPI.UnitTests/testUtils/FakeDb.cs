using System;
using System.Data;
using System.Data.Common;

namespace FormBuilderAPI.UnitTests.TestUtils;

public sealed class FakeDbConnectionFactory : FormBuilderAPI.Data.IDbConnectionFactory
{
    private readonly DbConnection _conn;
    public FakeDbConnectionFactory(DbConnection conn) => _conn = conn;
    public IDbConnection Create() => _conn;
}

public sealed class FakeDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
    public override void ChangeDatabase(string databaseName) {}
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;
    public override string ConnectionString { get; set; } = "Server=unit-tests";
    public override string Database => "unit";
    public override ConnectionState State => _state;
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

    public int Opens { get; private set; }
    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        Opens++;
        Open();
        return Task.CompletedTask;
    }
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly DbConnection _conn;
    public FakeDbCommand(DbConnection conn) => _conn = conn;
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    protected override DbConnection DbConnection { get => _conn; set { } }
    protected override DbParameterCollection DbParameterCollection { get; } = new FakeParamCollection();
    protected override DbTransaction DbTransaction { get; set; } = null!;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    public override void Cancel() { }
    protected override DbParameter CreateDbParameter() => new FakeParam();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
    public override int ExecuteNonQuery() => 1;
    public override object ExecuteScalar() => 1;
    public override void Prepare() { }
}

internal sealed class FakeParam : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; }
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; } = "";
    public override string SourceColumn { get; set; } = "";
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
}

internal sealed class FakeParamCollection : DbParameterCollection
{
    private readonly List<DbParameter> _list = new();
    public override int Count => _list.Count;
    public override object SyncRoot => this;
    public override int Add(object value) { _list.Add((DbParameter)value); return _list.Count-1; }
    public override void AddRange(Array values) { foreach (var v in values) Add(v!); }
    public override void Clear() => _list.Clear();
    public override bool Contains(object value) => _list.Contains((DbParameter)value);
    public override bool Contains(string value) => _list.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => _list.ToArray().CopyTo((object[])array, index);
    public override IEnumerator GetEnumerator() => _list.GetEnumerator();
    public override int IndexOf(object value) => _list.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => _list.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value) => _list.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _list.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _list.RemoveAt(index);
    public override void RemoveAt(string parameterName) { var i = IndexOf(parameterName); if (i>=0) RemoveAt(i); }
    protected override DbParameter GetParameter(int index) => _list[index];
    protected override DbParameter GetParameter(string parameterName) => _list.First(p => p.ParameterName == parameterName);
    protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) { var i = IndexOf(parameterName); if (i>=0) _list[i]=value; }
}