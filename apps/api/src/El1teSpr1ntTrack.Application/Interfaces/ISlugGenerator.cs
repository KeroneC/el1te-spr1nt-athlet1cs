namespace El1teSpr1ntTrack.Application.Interfaces;

public interface ISlugGenerator
{
    string Generate(string value);

    Task<string> GenerateUniqueAsync(
        string value,
        Func<string, CancellationToken, Task<bool>> slugExists,
        CancellationToken cancellationToken = default);
}
