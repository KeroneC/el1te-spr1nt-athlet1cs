using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.DTOs.Cms;
using El1teSpr1ntTrack.Core.DTOs.Commerce;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IStoreOrderService
{
    Task<PublicStoreCheckoutResultDto> CheckoutAsync(
        PublicStoreCheckoutRequestDto request,
        CancellationToken cancellationToken);
    Task<PublicStoreOrderStatusDto> GetPublicStatusAsync(string token, CancellationToken cancellationToken);
    Task<PublicStoreOrderStatusDto> CancelPublicOrderAsync(string token, CancellationToken cancellationToken);
    Task<PublicCheckoutReturnStatusDto?> GetReturnStatusAsync(string returnToken, CancellationToken cancellationToken);

    Task<AdminStoreOperationsDashboardDto> GetOperationsDashboardAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<AdminStoreOrderSummaryDto>> GetOrdersAsync(
        AdminStoreOrderOptions options,
        CancellationToken cancellationToken);
    Task<AdminStoreOrderDto> GetOrderAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminStoreOrderDto> TransitionAsync(
        Guid id,
        AdminStoreOrderTransitionDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task<AdminStoreOrderDto> AddNoteAsync(
        Guid id,
        AdminStoreOrderNoteWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task<AdminStoreRefundDto> RefundAsync(
        Guid id,
        AdminStoreRefundWriteDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task RetryRefundAsync(Guid id, Guid refundId, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminTrackingLinkResultDto> RotateTrackingLinkAsync(
        Guid id,
        Guid actorUserId,
        CancellationToken cancellationToken);
    Task RetryEmailAsync(Guid id, Guid emailId, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminCommerceIntegrationHealthDto> GetIntegrationHealthAsync(CancellationToken cancellationToken);

    Task ProcessSquareWebhookAsync(Guid webhookEventId, CancellationToken cancellationToken);
    Task ProcessRefundAsync(Guid refundId, CancellationToken cancellationToken);
    Task SendOrderEmailAsync(Guid emailId, CancellationToken cancellationToken);
    Task<int> RunMaintenanceAsync(CancellationToken cancellationToken);
}
