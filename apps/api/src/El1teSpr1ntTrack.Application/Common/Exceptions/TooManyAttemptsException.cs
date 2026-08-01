namespace El1teSpr1ntTrack.Application.Common.Exceptions;

public sealed class TooManyAttemptsException : Exception
{
    public TooManyAttemptsException() : base("Too many attempts. Wait before trying again.") { }
}
