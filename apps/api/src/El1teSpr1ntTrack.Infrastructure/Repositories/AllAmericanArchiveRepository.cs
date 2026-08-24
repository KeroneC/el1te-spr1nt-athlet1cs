using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class AllAmericanArchiveRepository(El1teDbContext db) : IAllAmericanArchiveRepository
{
    private IQueryable<AllAmericanYear> Detailed(bool tracking = true)
    {
        var query = tracking ? db.AllAmericanYears.AsQueryable() : db.AllAmericanYears.AsNoTracking();
        return query.Include(x => x.HeroMediaAsset)
            .Include(x => x.Media).ThenInclude(x => x.MediaAsset)
            .Include(x => x.Recipients).ThenInclude(x => x.PhotoMediaAsset)
            .Include(x => x.Performances).ThenInclude(x => x.Recipients).ThenInclude(x => x.Recipient);
    }

    public async Task<AdminPage<AllAmericanYear>> GetAdminAsync(AdminAllAmericanYearOptions options, CancellationToken token)
    {
        var query = Detailed(false);
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(x => x.Title.Contains(search) || x.Summary.Contains(search) || x.Year.ToString().Contains(search));
        }
        if (options.IsPublished.HasValue) query = query.Where(x => x.IsPublished == options.IsPublished.Value);
        var count = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.Year).ThenBy(x => x.DisplayOrder)
            .Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(token);
        return new(items, count);
    }

    public Task<AllAmericanYear?> GetAdminAsync(Guid id, CancellationToken token) => Detailed().FirstOrDefaultAsync(x => x.Id == id, token);
    public Task<AllAmericanYear?> GetPublicAsync(string slug, CancellationToken token) => Detailed(false).FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished, token);

    public async Task<AdminPage<AllAmericanYear>> GetPublicAsync(int page, int pageSize, CancellationToken token)
    {
        var query = Detailed(false).Where(x => x.IsPublished);
        var count = await query.CountAsync(token);
        var items = await query.OrderByDescending(x => x.Year).ThenBy(x => x.DisplayOrder)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(items, count);
    }

    public Task<bool> YearExistsAsync(int year, Guid? excludingId, CancellationToken token) =>
        db.AllAmericanYears.AnyAsync(x => x.Year == year && (!excludingId.HasValue || x.Id != excludingId.Value), token);
    public Task<MediaAsset?> GetActiveMediaAsync(Guid id, CancellationToken token) => db.MediaAssets.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, token);
    public Task<AllAmericanYearMedia?> GetMediaAsync(Guid yearId, Guid id, CancellationToken token) => db.AllAmericanYearMedia.FirstOrDefaultAsync(x => x.AllAmericanYearId == yearId && x.Id == id, token);
    public Task<AllAmericanRecipient?> GetRecipientAsync(Guid yearId, Guid id, CancellationToken token) => db.AllAmericanRecipients.FirstOrDefaultAsync(x => x.AllAmericanYearId == yearId && x.Id == id, token);
    public Task<AllAmericanPerformance?> GetPerformanceAsync(Guid yearId, Guid id, CancellationToken token) => db.AllAmericanPerformances.Include(x => x.Recipients).FirstOrDefaultAsync(x => x.AllAmericanYearId == yearId && x.Id == id, token);
    public async Task AddAsync<T>(T entity, CancellationToken token) where T : class => await db.Set<T>().AddAsync(entity, token);
    public void Delete<T>(T entity) where T : class => db.Set<T>().Remove(entity);
    public Task SaveChangesAsync(CancellationToken token) => db.SaveChangesAsync(token);
}
