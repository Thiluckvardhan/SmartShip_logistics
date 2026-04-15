using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using SmartShip.Core.Email;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Models;
using SmartShip.IdentityService.Repositories;
using SmartShip.IdentityService.Services;
using Xunit;

namespace SmartShip.Services.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsError()
    {
        var repository = new Mock<IUserRepository>();
        var emailService = new Mock<IEmailService>();

        repository
            .Setup(r => r.EmailExistsAsync("existing@example.com", null))
            .ReturnsAsync(true);

        var service = CreateAuthService(repository.Object, emailService.Object);

        var result = await service.RegisterAsync(new RegisterDto("Jane", "existing@example.com", "Password@123", "1234567890"));

        Assert.False(result.Ok);
        Assert.Equal("Email already registered.", result.Message);
        Assert.Null(result.Data);
        repository.Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAndVerifyLoginOtpAsync_WhenOtpIsValid_ReturnsTokens()
    {
        var repository = new Mock<IUserRepository>();
        var emailService = new Mock<IEmailService>();

        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            Name = "Test User",
            Email = "user@example.com",
            PasswordHash = new PasswordHasher<User>().HashPassword(new User(), "Password@123"),
            UserRoles =
            [
                new UserRole
                {
                    RoleId = 1,
                    Role = new Role { RoleId = 1, RoleName = "Customer" }
                }
            ]
        };

        string sentOtp = string.Empty;

        repository
            .Setup(r => r.GetUserByEmailAsync("user@example.com", true))
            .ReturnsAsync(user);

        emailService
            .Setup(s => s.SendOtpEmailAsync(user.Email, It.IsAny<string>()))
            .Callback<string, string>((_, otp) => sentOtp = otp)
            .ReturnsAsync(true);

        repository
            .Setup(r => r.GetUserByIdAsync(userId, true))
            .ReturnsAsync(user);

        repository
            .Setup(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        repository
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var service = CreateAuthService(repository.Object, emailService.Object);

        var loginResult = await service.LoginAsync(new LoginDto("user@example.com", "Password@123"));

        Assert.True(loginResult.Ok);
        Assert.NotNull(loginResult.Data);
        var challengeId = GetProperty<string>(loginResult.Data!, "challengeId");
        Assert.False(string.IsNullOrWhiteSpace(challengeId));
        Assert.False(string.IsNullOrWhiteSpace(sentOtp));

        var verifyResult = await service.VerifyLoginOtpAsync(new VerifyLoginOtpDto(challengeId, sentOtp));

        Assert.True(verifyResult.Ok);
        Assert.NotNull(verifyResult.Data);
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(verifyResult.Data!, "accessToken")));
        Assert.False(string.IsNullOrWhiteSpace(GetProperty<string>(verifyResult.Data!, "refreshToken")));

        repository.Verify(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    private static AuthService CreateAuthService(IUserRepository repository, IEmailService emailService)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-is-a-super-secret-key-for-unit-tests-only",
                ["Jwt:Issuer"] = "smartship-tests",
                ["Jwt:Audience"] = "smartship-clients"
            })
            .Build();

        return new AuthService(repository, config, emailService, new MemoryCache(new MemoryCacheOptions()));
    }

    private static T GetProperty<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(name);
        Assert.NotNull(property);

        var value = property.GetValue(source);
        Assert.NotNull(value);

        return (T)value;
    }
}
