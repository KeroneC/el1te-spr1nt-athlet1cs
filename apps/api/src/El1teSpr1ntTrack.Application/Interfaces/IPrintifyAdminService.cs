using El1teSpr1ntTrack.Core.DTOs.Commerce;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IPrintifyAdminService
{
    Task<PrintifyIntegrationHealthDto> GetHealthAsync(CancellationToken cancellationToken);
    Task<PrintifyCatalogPreviewDto> PreviewAsync(CancellationToken cancellationToken);
    Task<PrintifyCatalogImportResultDto> ImportAsync(
        PrintifyCatalogImportRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task<PrintifyRefreshResultDto> RefreshMappingsAsync(CancellationToken cancellationToken);
}
