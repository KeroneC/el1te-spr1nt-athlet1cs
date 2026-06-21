using El1teSpr1ntTrack.Application.Common;
using El1teSpr1ntTrack.Application.Common.Exceptions;
using El1teSpr1ntTrack.Application.Interfaces;
using El1teSpr1ntTrack.Application.Services;
using El1teSpr1ntTrack.Core.DTOs.Auth;
using El1teSpr1ntTrack.Core.Entities;
using El1teSpr1ntTrack.Core.Enums;
using El1teSpr1ntTrack.Core.Interfaces.Repositories;

namespace El1teSpr1ntTrack.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPassword()
    {
        var repository = new FakeUserRepository();
        var authService = CreateAuthService(repository);
        var request = CreateRegisterRequest();

        var response = await authService.RegisterAsync(request);

        Assert.NotNull(repository.AddedUser);
        Assert.Equal("parent@example.com", repository.AddedUser.Email);
        Assert.Equal(UserRole.Parent, repository.AddedUser.Role);
        Assert.True(repository.AddedUser.IsActive);
        Assert.NotEqual(request.Password, repository.AddedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(request.Password, repository.AddedUser.PasswordHash));
        Assert.Equal("test-access-token", response.AccessToken);
        Assert.Equal(repository.AddedUser.Id, response.User.Id);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            Email = "parent@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPassword123!")
        });

        var authService = CreateAuthService(repository);

        var exception = await Assert.ThrowsAsync<AuthValidationException>(
            () => authService.RegisterAsync(CreateRegisterRequest()));

        Assert.Contains(nameof(RegisterRequestDto.Email), exception.Errors.Keys);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task RegisterAsync_RejectsMismatchedPasswords()
    {
        var authService = CreateAuthService(new FakeUserRepository());
        var request = new RegisterRequestDto
        {
            FirstName = "Taylor",
            LastName = "Parent",
            Email = "Parent@Example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        var exception = await Assert.ThrowsAsync<AuthValidationException>(
            () => authService.RegisterAsync(request));

        Assert.Contains(nameof(RegisterRequestDto.ConfirmPassword), exception.Errors.Keys);
    }

    [Fact]
    public async Task LoginAsync_SucceedsWithCorrectPassword()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            FirstName = "Taylor",
            LastName = "Parent",
            Email = "parent@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            Role = UserRole.Parent,
            IsActive = true
        });

        var authService = CreateAuthService(repository);

        var response = await authService.LoginAsync(new LoginRequestDto
        {
            Email = "Parent@Example.com",
            Password = "StrongPassword123!"
        });

        Assert.Equal("test-access-token", response.AccessToken);
        Assert.Equal("parent@example.com", response.User.Email);
        Assert.Equal(UserRole.Parent, response.User.Role);
    }

    [Fact]
    public async Task LoginAsync_FailsWithWrongPassword()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            Email = "parent@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            IsActive = true
        });

        var authService = CreateAuthService(repository);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => authService.LoginAsync(new LoginRequestDto
            {
                Email = "parent@example.com",
                Password = "WrongPassword123!"
            }));
    }

    [Fact]
    public async Task LoginAsync_FailsForInactiveUser()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new User
        {
            Email = "parent@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongPassword123!"),
            IsActive = false
        });

        var authService = CreateAuthService(repository);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => authService.LoginAsync(new LoginRequestDto
            {
                Email = "parent@example.com",
                Password = "StrongPassword123!"
            }));
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsSafeIdentityIncludingActiveStatus()
    {
        var repository = new FakeUserRepository();
        var user = new User
        {
            FirstName = "Avery", LastName = "Admin", Email = "admin@example.com",
            PasswordHash = "not-returned", Role = UserRole.Admin, IsActive = false
        };
        repository.Users.Add(user);

        var result = await CreateAuthService(repository).GetCurrentUserAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("Avery Admin", result.DisplayName);
        Assert.Equal(UserRole.Admin, result.Role);
        Assert.False(result.IsActive);
    }

    private static AuthService CreateAuthService(FakeUserRepository repository)
    {
        return new AuthService(repository, new FakeJwtTokenService());
    }

    private static RegisterRequestDto CreateRegisterRequest()
    {
        return new RegisterRequestDto
        {
            FirstName = "Taylor",
            LastName = "Parent",
            Email = "Parent@Example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!"
        };
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public AuthTokenResult GenerateToken(User user)
        {
            return new AuthTokenResult("test-access-token", DateTimeOffset.UtcNow.AddMinutes(60));
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public User? AddedUser { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = Users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var exists = Users.Any(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            AddedUser = user;
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}
