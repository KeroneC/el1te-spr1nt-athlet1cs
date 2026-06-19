namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
