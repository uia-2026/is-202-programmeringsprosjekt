using System.Security.Claims;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OtpNet;
using QRCoder;

namespace Heimevernet.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IUnitOfWork unitOfWork,
    IAuditLogger auditLogger) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditLogger _auditLogger = auditLogger;

    public async Task<RegisterResult> RegisterAsync(
        RegisterViewModel model,
        CancellationToken ct)
    {
        if (await _userRepository.GetByEmailAsync(model.Email, ct) != null)
        {
            return new RegisterResult(
                Success: false,
                Error: "An account with this email already exists.");
        }

        var user = new User
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            Role = model.Role,
            ActorType = model.ActorType.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            model.Password);

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        return new RegisterResult(
            Success: true);
    }

    public async Task<LoginResult> LoginAsync(
        LoginViewModel model,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(model.Email, ct);

        if (user == null)
        {
            _auditLogger.Record(
                AuditAction.LoginFailed,
                "User",
                null,
                actorUserId: null,
                actorName: "Unknown",
                details: "Invalid credentials.");
            await _unitOfWork.CommitAsync(ct);

            return new LoginResult(
                Success: false,
                Error: "Invalid email or password.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            model.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            _auditLogger.Record(
                AuditAction.LoginFailed,
                "User",
                user.Id,
                user.Id,
                user.Username,
                "Invalid credentials.");
            await _unitOfWork.CommitAsync(ct);

            return new LoginResult(
                Success: false,
                Error: "Invalid email or password.");
        }

        if (user.TwoFactorEnabled)
        {
            return new LoginResult(
                Success: true,
                RequiresTwoFactor: true,
                UserId: user.Id);
        }

        _auditLogger.Record(AuditAction.LoginSucceeded, "User", user.Id, user.Id, user.Username);
        await _unitOfWork.CommitAsync(ct);

        return new LoginResult(
            Success: true,
            User: user);
    }

    public async Task<TwoFactorSetupViewModel> BeginTwoFactorSetupAsync(int userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new InvalidOperationException("User not found.");

        var secret = GenerateTwoFactorSecret();
        var qrCode = GenerateQrCodeBase64(secret, user.Username);

        return new TwoFactorSetupViewModel
        {
            QrCodeImageBase64 = qrCode,
            ManualEntryKey = secret
        };
    }

    public async Task<TwoFactorVerifyResult> VerifyTwoFactorAsync(
        int userId,
        string code,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return new TwoFactorVerifyResult(
                Success: false,
                Error: "Invalid session. Please log in again.");
        }

        if (!VerifyTwoFactorCode(user.TwoFactorSecret, code))
        {
            _auditLogger.Record(
                AuditAction.LoginFailed,
                "User",
                user.Id,
                user.Id,
                user.Username,
                "Invalid two-factor verification code.");
            await _unitOfWork.CommitAsync(ct);

            return new TwoFactorVerifyResult(
                Success: false,
                Error: "Invalid verification code.");
        }

        _auditLogger.Record(AuditAction.LoginSucceeded, "User", user.Id, user.Id, user.Username);
        await _unitOfWork.CommitAsync(ct);

        return new TwoFactorVerifyResult(
            Success: true,
            User: user);
    }

    public async Task<TwoFactorSetupResult> CompleteTwoFactorSetupAsync(
        int userId,
        string secret,
        string code,
        CancellationToken ct)
    {
        if (!VerifyTwoFactorCode(secret, code))
        {
            return new TwoFactorSetupResult(
                Success: false,
                Error: "Invalid verification code.");
        }

        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user == null)
        {
            return new TwoFactorSetupResult(
                Success: false,
                Error: "User not found.");
        }

        user.TwoFactorSecret = secret;
        user.TwoFactorEnabled = true;

        _auditLogger.Record(AuditAction.TwoFactorEnabled, "User", user.Id);

        await _unitOfWork.CommitAsync(ct);

        return new TwoFactorSetupResult(
            Success: true);
    }

    public async Task<bool> ChangePasswordAsync(
        int userId,
        ChangePasswordViewModel model,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user == null)
            return false;

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            model.CurrentPassword);

        if (verification == PasswordVerificationResult.Failed)
            return false;

        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();

        _auditLogger.Record(
            AuditAction.PasswordChanged,
            "User",
            user.Id,
            user.Id,
            user.Username,
            "Password changed.");

        await _unitOfWork.CommitAsync(ct);

        return true;
    }

    public async Task SignInAsync(User user, HttpContext httpContext)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    public async Task SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static string GenerateTwoFactorSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    private static string GenerateQrCodeBase64(string secret, string username)
    {
        var uri = $"otpauth://totp/Heimevernet:{username}?secret={secret}&issuer=Heimevernet";
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(10); // 10 is the size of each QR code module in pixels
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyTwoFactorCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2)); /// Allows codes from 2 previous and 2 future time steps
    }
}
