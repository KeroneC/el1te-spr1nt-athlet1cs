namespace El1teSpr1ntTrack.Core.DTOs;

public sealed record ApiResponseDto<T>(bool Succeeded, T? Data, string? Message);
