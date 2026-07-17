using System.Data;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace El1teSpr1ntTrack.Infrastructure.Repositories;

public sealed class AdminIdentityRepository(El1teDbContext dbContext) : IAdminIdentityRepository
{
    public async Task<AdminPage<User>> GetUsersAsync(AdminUserOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin);
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(user => user.Email.Contains(search) || user.FirstName.Contains(search) || user.LastName.Contains(search));
        }
        if (options.Role.HasValue) query = query.Where(user => user.Role == options.Role.Value);
        if (options.IsActive.HasValue) query = query.Where(user => user.IsActive == options.IsActive.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
            .Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(cancellationToken);
        return new AdminPage<User>(items, total);
    }

    public Task<User?> GetPrivilegedUserAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<User> query = dbContext.Users;
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(user => user.Id == id && (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin), cancellationToken);
    }

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken) =>
        dbContext.Users.CountAsync(user => user.Role == UserRole.SuperAdmin && user.IsActive, cancellationToken);

    public async Task<AdminPage<AdminInvitation>> GetInvitationsAsync(AdminInvitationOptions options, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var query = dbContext.AdminInvitations.AsNoTracking().Include(value => value.InvitedByUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(value => value.Email.Contains(search) || value.FirstName.Contains(search) || value.LastName.Contains(search));
        }
        query = options.Status?.Trim().ToLowerInvariant() switch
        {
            "pending" => query.Where(value => value.AcceptedAt == null && value.RevokedAt == null && value.ExpiresAt > now),
            "accepted" => query.Where(value => value.AcceptedAt != null),
            "revoked" => query.Where(value => value.RevokedAt != null),
            "expired" => query.Where(value => value.AcceptedAt == null && value.RevokedAt == null && value.ExpiresAt <= now),
            _ => query
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(value => value.CreatedAt)
            .Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(cancellationToken);
        return new AdminPage<AdminInvitation>(items, total);
    }

    public Task<AdminInvitation?> GetInvitationAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<AdminInvitation> query = dbContext.AdminInvitations.Include(value => value.InvitedByUser);
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
    }

    public Task<AdminInvitation?> GetInvitationByTokenHashAsync(string tokenHash, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<AdminInvitation> query = dbContext.AdminInvitations.Include(value => value.InvitedByUser);
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(value => value.TokenHash == tokenHash, cancellationToken);
    }

    public Task<bool> HasPendingInvitationAsync(string email, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.AdminInvitations.AnyAsync(value => value.Email == email && value.AcceptedAt == null && value.RevokedAt == null && value.ExpiresAt > now, cancellationToken);

    public async Task<AdminPage<AdminActivityLog>> GetActivityAsync(AdminActivityOptions options, CancellationToken cancellationToken)
    {
        var query = dbContext.AdminActivityLogs.AsNoTracking().Include(value => value.ActorUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(value => value.Summary.Contains(search) || value.Action.Contains(search) ||
                (value.ActorUser != null && (value.ActorUser.Email.Contains(search) || value.ActorUser.FirstName.Contains(search) || value.ActorUser.LastName.Contains(search))));
        }
        if (!string.IsNullOrWhiteSpace(options.Action)) query = query.Where(value => value.Action == options.Action);
        if (options.FromDate.HasValue) query = query.Where(value => value.CreatedAt >= options.FromDate.Value);
        if (options.ToDate.HasValue) query = query.Where(value => value.CreatedAt <= options.ToDate.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(value => value.CreatedAt)
            .Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).ToListAsync(cancellationToken);
        return new AdminPage<AdminActivityLog>(items, total);
    }

    public async Task AddInvitationAsync(AdminInvitation invitation, CancellationToken cancellationToken) =>
        await dbContext.AdminInvitations.AddAsync(invitation, cancellationToken);

    public async Task AddUserAsync(User user, CancellationToken cancellationToken) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public async Task AddActivityAsync(AdminActivityLog activity, CancellationToken cancellationToken) =>
        await dbContext.AdminActivityLogs.AddAsync(activity, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteSerializableAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
