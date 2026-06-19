using System.Linq.Expressions;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class Repository<T>(El1teDbContext dbContext) : IRepository<T>
    where T : EntityBase
{
    private readonly DbSet<T> _entities = dbContext.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _entities.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _entities.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _entities.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _entities.AddAsync(entity, cancellationToken);
        return entity;
    }

    public void Update(T entity)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        _entities.Update(entity);
    }

    public void Remove(T entity)
    {
        _entities.Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
