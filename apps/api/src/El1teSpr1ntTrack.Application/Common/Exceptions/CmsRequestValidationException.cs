namespace El1teSpr1ntTrack.Application.Common.Exceptions;

public sealed class CmsRequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The CMS request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
