namespace El1teSpr1ntTrack.Application.Common;

public sealed record AuthTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
