using Heimevernet.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using OtpNet;
using Xunit;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests verifying two-factor authentication (2FA) enforcement,
/// TOTP code verification, and multi-stage login session handling.
/// </summary>
public sealed class TwoFactorAuthenticationTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public TwoFactorAuthenticationTests(
        HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that logging in with valid password credentials on a 2FA-enabled account 
    /// redirects to the 2FA verification challenge rather than immediately establishing an authenticated session.
    /// </summary>
    [Fact]
    public async Task Login_TwoFactorEnabled_RedirectsToVerification()
    {
        // Seed a user in the isolated test database with 2FA pre-enabled.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(twoFactorEnabled: true);

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // Submit valid primary credentials (email/password).
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email,
                ["Password"] = TestCredentials.Password
            });

        // Assert that the initial login redirects to the 2FA challenge path.
        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        Assert.Contains(
            "/Account/VerifyTwoFactor",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Verify that correct primary password alone DOES NOT grant access to protected pages.
        using var protectedResponse = await client.GetAsync("/Account/Index");

        Assert.Equal(
            System.Net.HttpStatusCode.Redirect,
            protectedResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that submitting a valid TOTP code during the 2FA challenge grants 
    /// full authentication and allows access to protected endpoints.
    /// </summary>
    [Fact]
    public async Task VerifyTwoFactor_ValidCode_AuthenticatesUser()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(twoFactorEnabled: true);

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // 1. Initiate primary login (sets up pending session state/TempData).
        using var loginResponse = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email,
                ["Password"] = TestCredentials.Password
            });

        Assert.True(
            loginResponse.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        // 2. Obtain an antiforgery token from the 2FA verification page.
        // GET consumes the pending TempData value, but controller preserves it via TempData.Keep().
        var token = await AntiforgeryHelper.GetTokenAsync(
            client,
            "/Account/VerifyTwoFactor");

        // 3. Generate a valid TOTP code using OtpNet and the user's secret.
        var code = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret!))
            .ComputeTotp();

        // 4. Submit the 2FA verification form with the valid code.
        using var response = await client.PostAsync(
            "/Account/VerifyTwoFactor",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["UserId"] = user.Id.ToString(),
                    ["Code"] = code
                }));

        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        // 5. Confirm that full authenticated access is now granted.
        using var protectedResponse = await client.GetAsync("/Account/Index");

        Assert.True(
            protectedResponse.IsSuccessStatusCode,
            $"Expected authenticated access, got {protectedResponse.StatusCode}");
    }

    /// <summary>
    /// Verifies that submitting an invalid TOTP code during 2FA verification fails 
    /// and keeps the user in an unauthenticated state.
    /// </summary>
    [Fact]
    public async Task VerifyTwoFactor_InvalidCode_DoesNotAuthenticate()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(twoFactorEnabled: true);

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // 1. Submit primary credentials.
        using var loginResponse = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email,
                ["Password"] = TestCredentials.Password
            });

        var token = await AntiforgeryHelper.GetTokenAsync(
            client,
            "/Account/VerifyTwoFactor");

        // 2. Generate a guaranteed invalid TOTP code candidate.
        var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecret!));
        var invalidCode = FindInvalidCode(totp);

        // 3. Submit the verification form with the bad code.
        using var response = await client.PostAsync(
            "/Account/VerifyTwoFactor",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["UserId"] = user.Id.ToString(),
                    ["Code"] = invalidCode
                }));

        // Expect the controller to re-render the form (200 OK) with validation errors or redirect.
        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.OK
                or System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        // 4. Verify that access to protected pages remains denied.
        using var protectedResponse = await client.GetAsync("/Account/Index");

        Assert.Equal(
            System.Net.HttpStatusCode.Redirect,
            protectedResponse.StatusCode);
    }

    /// <summary>
    /// Helper method to find a 6-digit TOTP string that falls outside the current time window for the given secret.
    /// </summary>
    private static string FindInvalidCode(Totp totp)
    {
        for (var i = 0; i <= 999999; i++)
        {
            var candidate = i.ToString("D6");

            if (!totp.VerifyTotp(
                    candidate,
                    out _,
                    new VerificationWindow(previous: 2, future: 2)))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not find an invalid TOTP code.");
    }
}