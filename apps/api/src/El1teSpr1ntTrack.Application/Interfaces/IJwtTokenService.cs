using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Core.Entities;

namespace El1teSpr1ntTrack.Application.Interfaces;

public interface IJwtTokenService
{
    AuthTokenResult GenerateToken(User user);
}
