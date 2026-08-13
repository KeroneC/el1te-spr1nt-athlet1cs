using El1teSpr1ntTrack.Core.DTOs.Auth;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IAdminAuthenticationService
{
    Task<AdminLoginResultDto> LoginAsync(AdminLoginRequestDto request, string clientPartition, CancellationToken cancellationToken);
    Task<AuthResponseDto> VerifyMfaAsync(AdminMfaVerifyRequestDto request, string clientPartition, CancellationToken cancellationToken);
    Task RequestPasswordResetAsync(AdminPasswordResetRequestDto request, string clientPartition, CancellationToken cancellationToken);
    Task<AdminPasswordResetInspectionDto> InspectPasswordResetAsync(string token, CancellationToken cancellationToken);
    Task CompletePasswordResetAsync(AdminPasswordResetCompleteDto request, string clientPartition, CancellationToken cancellationToken);
}

public sealed record TransactionalEmail(
    string Recipient,
    string Subject,
    string PlainText,
    string Html);

public sealed record TransactionalEmailSendResult(string? ProviderMessageId);

public interface ITransactionalEmailSender
{
    Task<TransactionalEmailSendResult> SendAsync(TransactionalEmail message, CancellationToken cancellationToken);
}
