using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IAllAmericanArchiveService
{
    Task<PagedResultDto<AdminAllAmericanYearListItemDto>> GetAdminAsync(AdminAllAmericanYearOptions options, CancellationToken token);
    Task<AdminAllAmericanYearDto> GetAdminAsync(Guid id, CancellationToken token);
    Task<AdminAllAmericanYearDto> CreateAsync(AllAmericanYearWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> UpdateAsync(Guid id, AllAmericanYearWriteDto request, CancellationToken token);
    Task DeactivateAsync(Guid id, CancellationToken token);
    Task<AdminAllAmericanYearDto> AddMediaAsync(Guid yearId, AllAmericanYearMediaWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> UpdateMediaAsync(Guid yearId, Guid mediaId, AllAmericanYearMediaWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> RemoveMediaAsync(Guid yearId, Guid mediaId, CancellationToken token);
    Task<AdminAllAmericanYearDto> ReorderMediaAsync(Guid yearId, AllAmericanOrderDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> AddRecipientAsync(Guid yearId, AllAmericanRecipientWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> UpdateRecipientAsync(Guid yearId, Guid recipientId, AllAmericanRecipientWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> DeactivateRecipientAsync(Guid yearId, Guid recipientId, CancellationToken token);
    Task<AdminAllAmericanYearDto> AddPerformanceAsync(Guid yearId, AllAmericanPerformanceWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> UpdatePerformanceAsync(Guid yearId, Guid performanceId, AllAmericanPerformanceWriteDto request, CancellationToken token);
    Task<AdminAllAmericanYearDto> DeactivatePerformanceAsync(Guid yearId, Guid performanceId, CancellationToken token);
    Task<PagedResultDto<PublicAllAmericanYearListItemDto>> GetPublicAsync(int page, int pageSize, CancellationToken token);
    Task<PublicAllAmericanYearDto?> GetPublicAsync(string slug, CancellationToken token);
}

public interface IAllAmericanArchiveRepository
{
    Task<AdminPage<AllAmericanYear>> GetAdminAsync(AdminAllAmericanYearOptions options, CancellationToken token);
    Task<AllAmericanYear?> GetAdminAsync(Guid id, CancellationToken token);
    Task<AllAmericanYear?> GetPublicAsync(string slug, CancellationToken token);
    Task<AdminPage<AllAmericanYear>> GetPublicAsync(int page, int pageSize, CancellationToken token);
    Task<bool> YearExistsAsync(int year, Guid? excludingId, CancellationToken token);
    Task<MediaAsset?> GetActiveMediaAsync(Guid id, CancellationToken token);
    Task<AllAmericanYearMedia?> GetMediaAsync(Guid yearId, Guid id, CancellationToken token);
    Task<AllAmericanRecipient?> GetRecipientAsync(Guid yearId, Guid id, CancellationToken token);
    Task<AllAmericanPerformance?> GetPerformanceAsync(Guid yearId, Guid id, CancellationToken token);
    Task AddAsync<T>(T entity, CancellationToken token) where T : class;
    void Delete<T>(T entity) where T : class;
    Task SaveChangesAsync(CancellationToken token);
}
