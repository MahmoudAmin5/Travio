using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Travio.Core.Domain.Infrastructure.Contract;

namespace Travio.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void SaveInclude(T entity, params string[] includedProperties)
    {
        var primaryKey = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
        var localEntity = primaryKey == null
            ? _dbSet.Local.FirstOrDefault(e => ((dynamic)e).Id == ((dynamic)entity).Id)
            : _dbSet.Local.FirstOrDefault(e =>
                primaryKey.Properties.All(p =>
                    Equals(p.PropertyInfo?.GetValue(e), p.PropertyInfo?.GetValue(entity))));

        EntityEntry entry;

        if (localEntity == null)
        {
            entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
        }
        else
        {
            entry = _context.Entry(localEntity);
            if (!ReferenceEquals(localEntity, entity))
            {
                var incomingEntry = _context.Entry(entity);
                foreach (var prop in includedProperties)
                {
                    entry.Property(prop).CurrentValue = incomingEntry.Property(prop).CurrentValue;
                }
            }
        }

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey() || property.Metadata.IsConcurrencyToken)
                continue;
            else
            {
                if (includedProperties.Contains(property.Metadata.Name))
                {
                    property.IsModified = true;
                }
                else
                {
                    property.IsModified = false;
                }
            }
        }
    }
}
