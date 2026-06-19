using El1teSpr1ntTrack.Application.Interfaces;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
