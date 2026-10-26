using System.Security.Claims;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using OtpNet;
using Xunit;

namespace Heimevernet.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="AuthService"/> verifying registration, authentication logic,
/// TOTP two-factor workflows, and claim-based session signing.
/// </summary>
public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFailureResult()
    {
        // Arrange
        var model = CreateValidRegisterViewModel();
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = model.Email });

        // Act
        var result = await _sut.RegisterAsync(model, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("An account with this email already exists.", result.Error);
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ValidModel_HashesPasswordTrimsInputAndSavesUser()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Username = "  john_doe  ",
            Email = "  JOHN@EXAMPLE.COM  ",
            Password = "Password123!",
            Role = UserRole.PublicActor,
            ActorType = "  Municipality  "
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(It.IsAny<User>(), model.Password))
            .Returns("hashed_secret");

        // Act
        var result = await _sut.RegisterAsync(model, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Username == "john_doe" &&
            u.Email == "JOHN@EXAMPLE.COM" &&
            u.ActorType == "Municipality" &&
            u.PasswordHash == "hashed_secret"
        ), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsFailureResult()
    {
        // Arrange
        var model = new LoginViewModel { Email = "unknown@example.com", Password = "Pass" };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(model, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailureResult()
    {
        // Arrange
        var user = new User { Email = "user@example.com", PasswordHash = "correct_hash" };
        var model = new LoginViewModel { Email = user.Email, Password = "WrongPassword" };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, model.Password))
            .Returns(PasswordVerificationResult.Failed);

        // Act
        var result = await _sut.LoginAsync(model, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_TwoFactorDisabled_ReturnsSuccessWithUser()
    {
        // Arrange
        var user = new User { Id = 10, Email = "user@example.com", PasswordHash = "hash", TwoFactorEnabled = false };
        var model = new LoginViewModel { Email = user.Email, Password = "ValidPassword" };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, model.Password))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await _sut.LoginAsync(model, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.RequiresTwoFactor);
        Assert.Equal(user, result.User);
    }

    [Fact]
    public async Task LoginAsync_TwoFactorEnabled_ReturnsRequiresTwoFactorResult()
    {
        // Arrange
        var user = new User { Id = 42, Email = "user@example.com", PasswordHash = "hash", TwoFactorEnabled = true };
        var model = new LoginViewModel { Email = user.Email, Password = "ValidPassword" };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(model.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(user, user.PasswordHash, model.Password))
            .Returns(PasswordVerificationResult.Success);

        // Act
        var result = await _sut.LoginAsync(model, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.RequiresTwoFactor);
        Assert.Equal(42, result.UserId);
        Assert.Null(result.User);
    }

    #endregion

    #region TwoFactor Setup & Verification Tests

    [Fact]
    public async Task BeginTwoFactorSetupAsync_UserNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.BeginTwoFactorSetupAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task BeginTwoFactorSetupAsync_ValidUser_ReturnsQrCodeAndManualKey()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.BeginTwoFactorSetupAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.ManualEntryKey));
        Assert.False(string.IsNullOrEmpty(result.QrCodeImageBase64));
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_UserNotFoundOrSecretMissing_ReturnsFailureResult()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 1, TwoFactorSecret = null });

        // Act
        var result = await _sut.VerifyTwoFactorAsync(1, "123456", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid session. Please log in again.", result.Error);
    }

    [Fact]
    public async Task VerifyTwoFactorAsync_ValidCode_ReturnsSuccessWithUser()
    {
        // Arrange
        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
        var user = new User { Id = 5, TwoFactorSecret = secret };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.VerifyTwoFactorAsync(user.Id, code, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(user, result.User);
    }

    [Fact]
    public async Task CompleteTwoFactorSetupAsync_InvalidCode_ReturnsFailureWithoutCommit()
    {
        // Arrange
        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

        // Act
        var result = await _sut.CompleteTwoFactorSetupAsync(1, secret, "000000", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid verification code.", result.Error);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CompleteTwoFactorSetupAsync_ValidCode_Enables2FAAndSavesSecret()
    {
        // Arrange
        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        var validCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
        var user = new User { Id = 10, TwoFactorEnabled = false, TwoFactorSecret = null };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.CompleteTwoFactorSetupAsync(user.Id, secret, validCode, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal(secret, user.TwoFactorSecret);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    #endregion

    #region SignIn / SignOut Tests

    [Fact]
    public async Task SignInAsync_InvokesAuthenticationServiceWithUserClaims()
    {
        // Arrange
        var user = new User { Id = 99, Username = "test_user", Role = UserRole.PublicActor };

        var authServiceMock = new Mock<IAuthenticationService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(s => s.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };

        // Act
        await _sut.SignInAsync(user, httpContext);

        // Assert
        authServiceMock.Verify(a => a.SignInAsync(
            httpContext,
            "Cookies",
            It.Is<ClaimsPrincipal>(cp =>
                cp.HasClaim(ClaimTypes.NameIdentifier, "99") &&
                cp.HasClaim(ClaimTypes.Name, "test_user") &&
                cp.HasClaim(ClaimTypes.Role, UserRole.PublicActor.ToString())
            ),
            It.IsAny<AuthenticationProperties>()
        ), Times.Once);
    }

    [Fact]
    public async Task SignOutAsync_InvokesAuthenticationServiceSignOut()
    {
        // Arrange
        var authServiceMock = new Mock<IAuthenticationService>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(s => s.GetService(typeof(IAuthenticationService)))
            .Returns(authServiceMock.Object);

        var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };

        // Act
        await _sut.SignOutAsync(httpContext);

        // Assert
        authServiceMock.Verify(a => a.SignOutAsync(
            httpContext,
            "Cookies",
            It.IsAny<AuthenticationProperties>()
        ), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static RegisterViewModel CreateValidRegisterViewModel() => new()
    {
        Username = "validuser",
        Email = "test@example.com",
        Password = "Password123!",
        Role = UserRole.PublicActor,
        ActorType = "Municipality"
    };

    #endregion
}