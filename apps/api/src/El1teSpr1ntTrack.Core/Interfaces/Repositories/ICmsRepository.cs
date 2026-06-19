using System.Linq.Expressions;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Core.Interfaces.Repositories;

public interface ICmsRepository<T>
    where T : CmsEntityBase
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
