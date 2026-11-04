using System.Data;
using FarmFMS.Application.Common.Results;
using FarmFMS.Infrastructure.Data;
using FarmFMS.UnitTests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmFMS.UnitTests.Infrastructure;

public class UnitOfWorkTests
{
    private readonly FakeConnectionFactory _factory = new();

    private UnitOfWork CreateUnitOfWork() => new(_factory, NullLogger<UnitOfWork>.Instance);

    [Fact]
    public async Task No_connection_is_opened_until_a_transaction_begins()
    {
        await using var unitOfWork = CreateUnitOfWork();

        Assert.False(unitOfWork.HasActiveTransaction);
        Assert.Empty(_factory.Created);
    }

    [Fact]
    public async Task Begin_opens_a_connection_through_the_factory_and_exposes_the_transaction()
    {
        await using var unitOfWork = CreateUnitOfWork();

        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        var connection = Assert.Single(_factory.Created);
        Assert.True(unitOfWork.HasActiveTransaction);
        Assert.Same(connection.LastTransaction, ((ITransactionAccessor)unitOfWork).CurrentTransaction);
        Assert.Equal(IsolationLevel.Serializable, connection.LastTransaction!.IsolationLevel);
    }

    [Fact]
    public async Task Commit_commits_and_releases_the_connection()
    {
        await using var unitOfWork = CreateUnitOfWork();
        await unitOfWork.BeginTransactionAsync();

        await unitOfWork.CommitAsync();

        var connection = Assert.Single(_factory.Created);
        Assert.True(connection.LastTransaction!.IsCommitted);
        Assert.True(connection.IsDisposed);
        Assert.False(unitOfWork.HasActiveTransaction);
    }

    [Fact]
    public async Task Nested_transactions_are_rejected()
    {
        await using var unitOfWork = CreateUnitOfWork();
        await unitOfWork.BeginTransactionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task Commit_without_a_transaction_throws()
    {
        await using var unitOfWork = CreateUnitOfWork();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CommitAsync());
    }

    [Fact]
    public async Task ExecuteInTransaction_commits_a_successful_result()
    {
        await using var unitOfWork = CreateUnitOfWork();

        var result = await unitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(Result.Success()));

        Assert.True(result.IsSuccess);
        Assert.True(_factory.Created.Single().LastTransaction!.IsCommitted);
    }

    [Fact]
    public async Task ExecuteInTransaction_rolls_back_a_failed_result()
    {
        await using var unitOfWork = CreateUnitOfWork();
        var error = Error.Conflict("Test.Conflict", "Conflict.");

        var result = await unitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(Result.Failure(error)));

        var transaction = _factory.Created.Single().LastTransaction!;
        Assert.Same(error, result.Error);
        Assert.True(transaction.IsRolledBack);
        Assert.False(transaction.IsCommitted);
    }

    [Fact]
    public async Task ExecuteInTransaction_rolls_back_and_rethrows_on_exception()
    {
        await using var unitOfWork = CreateUnitOfWork();

        await Assert.ThrowsAsync<TimeoutException>(() =>
            unitOfWork.ExecuteInTransactionAsync<Result>(_ => throw new TimeoutException()));

        var connection = _factory.Created.Single();
        Assert.True(connection.LastTransaction!.IsRolledBack);
        Assert.True(connection.IsDisposed);
    }

    [Fact]
    public async Task Disposing_rolls_back_an_uncommitted_transaction()
    {
        var unitOfWork = CreateUnitOfWork();
        await unitOfWork.BeginTransactionAsync();

        await unitOfWork.DisposeAsync();

        var connection = _factory.Created.Single();
        Assert.True(connection.LastTransaction!.IsRolledBack);
        Assert.True(connection.IsDisposed);
    }

    [Fact]
    public async Task Synchronous_dispose_rolls_back_an_uncommitted_transaction()
    {
        var unitOfWork = CreateUnitOfWork();
        await unitOfWork.BeginTransactionAsync();

        unitOfWork.Dispose();

        var connection = _factory.Created.Single();
        Assert.True(connection.LastTransaction!.IsRolledBack);
        Assert.True(connection.IsDisposed);
    }
}
