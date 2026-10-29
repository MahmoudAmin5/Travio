using Microsoft.EntityFrameworkCore.Storage;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Stack<string> _savepoints = new();
    private int _depth;
    private IDbContextTransaction? _transaction;
    private bool _isAmbientTransaction;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteInternalAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        return ExecuteInternalAsync(action, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        GC.SuppressFinalize(this);
    }

    private async Task<T> ExecuteInternalAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var isOutermost = _depth == 0;
        IDbContextTransaction? activeTx;

        if (isOutermost)
        {
            if (_dbContext.Database.CurrentTransaction != null)
            {
                _isAmbientTransaction = true;
                _transaction = null;
                activeTx = _dbContext.Database.CurrentTransaction;
                var savepointName = $"SP_Ambient_{_depth}_{Guid.NewGuid():N}"[..32];
                _savepoints.Push(savepointName);
                await activeTx.CreateSavepointAsync(savepointName, cancellationToken);
            }
            else
            {
                _isAmbientTransaction = false;
                _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                activeTx = _transaction;
            }
        }
        else
        {
            activeTx = _transaction ?? _dbContext.Database.CurrentTransaction;
            var savepointName = $"SP_Depth{_depth}_{Guid.NewGuid():N}"[..32];
            _savepoints.Push(savepointName);
            await activeTx!.CreateSavepointAsync(savepointName, cancellationToken);
        }

        _depth++;

        try
        {
            var result = await action();

            var shouldRollback = result is IServiceResult r && r.BusinessRuleFailed;

            _depth--;

            if (shouldRollback)
            {
                if (_depth == 0)
                {
                    if (_isAmbientTransaction && _savepoints.Count > 0)
                    {
                        var savepoint = _savepoints.Pop();
                        await activeTx!.RollbackToSavepointAsync(savepoint, cancellationToken);
                    }
                    else if (_transaction != null)
                    {
                        await _transaction.RollbackAsync(cancellationToken);
                    }
                }
                else if (_savepoints.Count > 0)
                {
                    var savepoint = _savepoints.Pop();
                    await activeTx!.RollbackToSavepointAsync(savepoint, cancellationToken);
                }
            }
            else
            {
                if (_depth == 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    if (!_isAmbientTransaction && _transaction != null)
                    {
                        await _transaction.CommitAsync(cancellationToken);
                    }
                    else if (_savepoints.Count > 0)
                    {
                        _savepoints.Pop();
                    }
                }
                else if (_savepoints.Count > 0)
                {
                    _savepoints.Pop();
                }
            }

            return result;
        }
        catch
        {
            _depth--;

            if (_depth == 0)
            {
                if (_isAmbientTransaction && _savepoints.Count > 0)
                {
                    var savepoint = _savepoints.Pop();
                    await activeTx!.RollbackToSavepointAsync(savepoint, cancellationToken);
                }
                else if (_transaction != null)
                {
                    await _transaction.RollbackAsync(cancellationToken);
                }
            }
            else if (_savepoints.Count > 0)
            {
                var savepoint = _savepoints.Pop();
                await activeTx!.RollbackToSavepointAsync(savepoint, cancellationToken);
            }

            throw;
        }
        finally
        {
            if (_depth == 0)
            {
                if (_transaction is not null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
                _isAmbientTransaction = false;
                _savepoints.Clear();
            }
        }
    }
}
