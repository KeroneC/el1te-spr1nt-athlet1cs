using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;

namespace El1teSpr1ntTrack.UnitTests;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsUtcTimestamp()
    {
        IClock clock = new SystemClock();

        var timestamp = clock.UtcNow;

        Assert.Equal(TimeSpan.Zero, timestamp.Offset);
    }
}
