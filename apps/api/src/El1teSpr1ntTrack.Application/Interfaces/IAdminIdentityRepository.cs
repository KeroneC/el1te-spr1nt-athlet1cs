using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IAdminIdentityRepository
{
    Task<AdminPage<User>> GetUsersAsync(AdminUserOptions options, CancellationToken cancellationToken);
    Task<User?> GetPrivilegedUserAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken);
    Task<AdminPage<AdminInvitation>> GetInvitationsAsync(AdminInvitationOptions options, DateTimeOffset now, CancellationToken cancellationToken);
    Task<AdminInvitation?> GetInvitationAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<AdminInvitation?> GetInvitationByTokenHashAsync(string tokenHash, bool tracked, CancellationToken cancellationToken);
    Task<bool> HasPendingInvitationAsync(string email, DateTimeOffset now, CancellationToken cancellationToken);
    Task<AdminPage<AdminActivityLog>> GetActivityAsync(AdminActivityOptions options, CancellationToken cancellationToken);
    Task AddInvitationAsync(AdminInvitation invitation, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddActivityAsync(AdminActivityLog activity, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task ExecuteSerializableAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
