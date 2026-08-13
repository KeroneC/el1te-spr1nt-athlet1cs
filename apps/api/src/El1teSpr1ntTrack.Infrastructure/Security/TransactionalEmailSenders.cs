using Azure;
using Azure.Communication.Email;
using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Infrastructure.Security;

public sealed class DevelopmentFileEmailSender(TransactionalEmailSettings settings) : ITransactionalEmailSender
{
    public async Task<TransactionalEmailSendResult> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
    {
        var directory = Path.GetFullPath(settings.DevelopmentOutboxPath);
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var file = Path.Combine(directory, $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
        var body = $"To: {message.Recipient}\nSubject: {message.Subject}\n\n{message.PlainText}\n";
        await File.WriteAllTextAsync(file, body, cancellationToken);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        return new TransactionalEmailSendResult(null);
    }
}

public sealed class AzureCommunicationEmailSender(TransactionalEmailSettings settings) : ITransactionalEmailSender
{
    private readonly EmailClient _client = new(settings.ConnectionString ??
        throw new InvalidOperationException("TransactionalEmail:ConnectionString is required."));

    public async Task<TransactionalEmailSendResult> SendAsync(TransactionalEmail message, CancellationToken cancellationToken)
    {
        var operation = await _client.SendAsync(
            WaitUntil.Completed,
            settings.SenderAddress,
            message.Recipient,
            message.Subject,
            message.Html,
            cancellationToken: cancellationToken);
        return new TransactionalEmailSendResult(operation.Id);
    }
}
