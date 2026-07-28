using El1teSpr1ntTrack.Core.DTOs.Commerce;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IPublicStoreService
{
    Task<PublicStoreCatalogDto> GetProductsAsync(
        PublicStoreQueryOptions options,
        CancellationToken cancellationToken);

    Task<PublicStoreProductDto?> GetProductAsync(
        string slug,
        CancellationToken cancellationToken);
}
