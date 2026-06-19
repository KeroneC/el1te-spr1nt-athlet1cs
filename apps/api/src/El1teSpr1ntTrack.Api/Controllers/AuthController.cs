using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace El1teSpr1ntTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
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

    [HttpGet("me")]
    public IActionResult Me()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Current-user profile lookup will require authenticated requests."
        });
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
}
