using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface ICmsValidationService
{
    IReadOnlyDictionary<string, string[]> Validate(CmsEntityBase entity);
}
