namespace El1teSpr1ntTrack.Application.Common;

public sealed class AdminInvitationSettings
{
    public const string SectionName = "AdminInvitations";

    public string SiteUrl { get; set; } = "http://localhost:3000";

    public int ExpiresHours { get; set; } = 72;
}
