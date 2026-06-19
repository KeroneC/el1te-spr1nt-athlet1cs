namespace El1teSpr1ntTrack.Application.Common.Exceptions;

public sealed class AuthValidationException : Exception
{
    public AuthValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Authentication request validation failed.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
