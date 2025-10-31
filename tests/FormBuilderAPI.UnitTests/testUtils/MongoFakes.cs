using MongoDB.Driver;

namespace FormBuilderAPI.UnitTests.TestUtils;

internal sealed class FakeAsyncCursor<T> : IAsyncCursor<T>
{
    private readonly IEnumerator<T> _enumerator;
    private bool _moved = false;
    public FakeAsyncCursor(IEnumerable<T> items)
    {
        _enumerator = items.GetEnumerator();
    }
    public IEnumerable<T> Current => _moved && _enumerator.Current != null ? new[] { _enumerator.Current } : Enumerable.Empty<T>();
    public bool MoveNext(CancellationToken cancellationToken = default)
    {
        var ok = _enumerator.MoveNext();
        _moved = ok;
        return ok;
    }
    public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(MoveNext(cancellationToken));
    public void Dispose() => _enumerator.Dispose();
}