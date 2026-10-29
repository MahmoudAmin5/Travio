namespace Travio.Core.Domain.Infrastructure.Contract
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> GetQueryable();
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Delete(T entity);
        void SaveInclude(T entity, params string[] includedProperties);
    }
}
