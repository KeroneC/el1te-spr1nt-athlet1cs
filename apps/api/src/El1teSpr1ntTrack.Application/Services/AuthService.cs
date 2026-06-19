using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;

namespace El1teSpr1ntTrack.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var errors = ValidateRegisterRequest(request, normalizedEmail);

        if (errors.Count > 0)
        {
            throw new AuthValidationException(errors);
        }

        if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new AuthValidationException(new Dictionary<string, string[]>
            {
                [nameof(RegisterRequestDto.Email)] = ["Email is already registered."]
            });
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Parent,
            IsActive = true
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return CreateAuthResponse(user);
    }

    private static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static Dictionary<string, string[]> ValidateRegisterRequest(
        RegisterRequestDto request,
        string normalizedEmail)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errors[nameof(RegisterRequestDto.FirstName)] = ["First name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errors[nameof(RegisterRequestDto.LastName)] = ["Last name is required."];
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            errors[nameof(RegisterRequestDto.Email)] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors[nameof(RegisterRequestDto.Password)] = ["Password is required."];
        }
        else if (request.Password.Length < 8)
        {
            errors[nameof(RegisterRequestDto.Password)] = ["Password must be at least 8 characters long."];
        }

        if (request.Password != request.ConfirmPassword)
        {
            errors[nameof(RegisterRequestDto.ConfirmPassword)] = ["Password and confirmation password do not match."];
        }

        return errors;
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var token = jwtTokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            User = new UserSummaryDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role
            }
        };
    }
}
