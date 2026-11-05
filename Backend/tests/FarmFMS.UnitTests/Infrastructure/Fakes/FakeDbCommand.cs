using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace FarmFMS.UnitTests.Infrastructure.Fakes;

/// <summary>
/// In-memory command for <see cref="FakeDbConnection"/>. <c>ExecuteNonQuery</c> calls the connection's handler,
/// which plays the Stored Procedure (e.g. sets output parameters). Readers are not supported.
/// </summary>
internal sealed class FakeDbCommand(FakeDbConnection connection, Func<FakeDbCommand, int> handler) : DbCommand
{
    private readonly FakeDbParameterCollection _parameters = new();

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; } = connection;

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    /// <summary>
    /// Parameter by name (without '@'), for the handler and assertions.
    /// </summary>
    public DbParameter Parameter(string name) => _parameters.Find(name)
        ?? throw new InvalidOperationException($"The command has no parameter '{name}'.");

    public bool HasParameter(string name) => _parameters.Find(name) is not null;

    public override int ExecuteNonQuery()
    {
        connection.ExecutedCommands.Add(this);
        return handler(this);
    }

    public override object? ExecuteScalar() => throw new NotSupportedException();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

    public override void Cancel()
    {
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;

    public override int Size { get; set; }

    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType()
    {
    }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = [];

    public override int Count => _items.Count;

    public override object SyncRoot { get; } = new();

    public DbParameter? Find(string name) =>
        _items.FirstOrDefault(p => string.Equals(Clean(p.ParameterName), Clean(name), StringComparison.OrdinalIgnoreCase));

    public override int Add(object value)
    {
        _items.Add((DbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
            Add(value);
    }

    public override void Clear() => _items.Clear();

    public override bool Contains(object value) => _items.Contains((DbParameter)value);

    public override bool Contains(string value) => Find(value) is not null;

    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => _items.GetEnumerator();

    public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName) => Find(parameterName) is { } p ? _items.IndexOf(p) : -1;

    public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);

    public override void Remove(object value) => _items.Remove((DbParameter)value);

    public override void RemoveAt(int index) => _items.RemoveAt(index);

    public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOf(parameterName));

    protected override DbParameter GetParameter(int index) => _items[index];

    protected override DbParameter GetParameter(string parameterName) =>
        Find(parameterName) ?? throw new IndexOutOfRangeException(parameterName);

    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;

    private static string Clean(string? name) => (name ?? string.Empty).TrimStart('@');
}
