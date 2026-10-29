using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace Tests;

class AsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncQuery(IEnumerable<T> items) : base(items) { }
    public AsyncQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator(this.AsEnumerable().GetEnumerator());

    private class AsyncEnumerator(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    }

    private class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => new AsyncQuery<T>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQuery<TElement>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var value = inner.Execute(expression);
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0]).Invoke(null, [value])!;
        }
    }
}

static class TestSet
{
    public static DbSet<T> Create<T>(params T[] items) where T : class
    {
        IQueryable<T> query = new AsyncQuery<T>(items);
        var set = new Mock<DbSet<T>>();
        set.As<IQueryable<T>>().Setup(x => x.Provider).Returns(query.Provider);
        set.As<IQueryable<T>>().Setup(x => x.Expression).Returns(query.Expression);
        set.As<IQueryable<T>>().Setup(x => x.ElementType).Returns(query.ElementType);
        set.As<IQueryable<T>>().Setup(x => x.GetEnumerator()).Returns(() => query.GetEnumerator());
        set.As<IAsyncEnumerable<T>>().Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => ((IAsyncEnumerable<T>)query).GetAsyncEnumerator());
        return set.Object;
    }
}
