namespace El1teSpr1ntTrack.Application.Common;

public sealed class AuthFeatureSettings
{
    public const string SectionName = "AuthFeatures";
    public bool AllowPublicRegistration { get; set; }
    public int FailedLoginLimit { get; set; } = 5;
    public int FailedLoginWindowMinutes { get; set; } = 15;
    public int LockoutMinutes { get; set; } = 15;
    public int PasswordResetMinutes { get; set; } = 30;
    public int MfaMinutes { get; set; } = 10;
    public int MfaMaximumAttempts { get; set; } = 5;
}

public sealed class TransactionalEmailSettings
{
    public const string SectionName = "TransactionalEmail";
    public string Provider { get; set; } = "DevelopmentFile";
    public string? ConnectionString { get; set; }
    public string SenderAddress { get; set; } = "DoNotReply@localhost";
    public string? ReplyToAddress { get; set; }
    public string DevelopmentOutboxPath { get; set; } = "App_Data/dev-mail";
    public string AdminSiteUrl { get; set; } = "http://localhost:3000";
}
