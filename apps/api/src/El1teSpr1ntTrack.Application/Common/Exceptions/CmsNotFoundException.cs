namespace El1teSpr1ntTrack.Application.Common.Exceptions;

public sealed class CmsNotFoundException(string resourceName, Guid id)
    : Exception($"{resourceName} '{id}' was not found.");
