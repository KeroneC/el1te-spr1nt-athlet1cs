using System.Security.Claims;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IAdminAuthenticationService adminAuthenticationService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AuthValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails(ToModelState(exception.Errors))
            {
                Title = "Registration request is invalid.",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("authentication")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidCredentialsException exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Login failed.",
                Detail = exception.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("admin/login")]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> AdminLogin(AdminLoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await adminAuthenticationService.LoginAsync(request, ClientPartition(), cancellationToken));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Login failed.", Detail = "Email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("admin/mfa/verify")]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> VerifyAdminMfa(AdminMfaVerifyRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await adminAuthenticationService.VerifyMfaAsync(request, ClientPartition(), cancellationToken));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Verification failed.", Detail = "The verification code is invalid or expired.",
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("admin/password-reset/request")]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> RequestAdminPasswordReset(
        AdminPasswordResetRequestDto request,
        CancellationToken cancellationToken)
    {
        await adminAuthenticationService.RequestPasswordResetAsync(request, ClientPartition(), cancellationToken);
        return Accepted(new GenericAcceptedDto("If an eligible account exists, a password reset message has been sent."));
    }

    [AllowAnonymous]
    [HttpPost("admin/password-reset/inspect")]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> InspectAdminPasswordReset(
        AdminPasswordResetInspectDto request,
        CancellationToken cancellationToken) =>
        Ok(await adminAuthenticationService.InspectPasswordResetAsync(request.Token, cancellationToken));

    [AllowAnonymous]
    [HttpPost("admin/password-reset/complete")]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> CompleteAdminPasswordReset(
        AdminPasswordResetCompleteDto request,
        CancellationToken cancellationToken)
    {
        await adminAuthenticationService.CompletePasswordResetAsync(request, ClientPartition(), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var currentUser = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return currentUser is null ? NotFound() : Ok(currentUser);
    }

    private static ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();

        foreach (var errorGroup in errors)
        {
            foreach (var error in errorGroup.Value)
            {
                modelState.AddModelError(errorGroup.Key, error);
            }
        }

        return modelState;
    }

    private string ClientPartition() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
