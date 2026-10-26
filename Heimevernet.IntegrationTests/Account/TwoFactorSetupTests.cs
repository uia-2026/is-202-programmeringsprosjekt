using Heimevernet.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Xunit;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests verifying the two-factor authentication (2FA) setup workflow,
/// including secret key extraction, TOTP verification, and database state persistence.
/// </summary>
public sealed class TwoFactorSetupTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public TwoFactorSetupTests(HeimevernetWebApplicationFactory factory) => _factory = factory;

    /// <summary>
    /// Verifies that an authenticated user can initiate 2FA setup, receive a Base32 secret key,
    /// submit a valid TOTP verification code, and successfully enable 2FA on their database record.
    /// </summary>
    [Fact]
    public async Task SetupTwoFactor_ValidCode_Enables2FA()
    {
        // Seed a test user in the database with 2FA initially disabled.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(twoFactorEnabled: false);

        // Configure client to preserve authentication cookies and avoid following redirects automatically.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // 1. Log in to establish an active authenticated cookie session.
        await AntiforgeryHelper.PostFormAsync(client, "/Account/Login", "/Account/Login", new()
        {
            ["Email"] = user.Email,
            ["Password"] = TestCredentials.Password
        });

        // 2. Perform a GET request to /Account/SetupTwoFactor to generate and initialize the secret in TempData.
        var setupToken = await AntiforgeryHelper.GetTokenAsync(client, "/Account/SetupTwoFactor");
        var setupHtml = await (await client.GetAsync("/Account/SetupTwoFactor")).Content.ReadAsStringAsync();

        // Extract the generated Base32 secret key from the rendered view HTML and compute a valid TOTP code.
        var secret = ExtractSecretFromHtml(setupHtml);
        var validCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

        // 3. Post the TOTP verification code and antiforgery token back to complete 2FA setup.
        using var response = await client.PostAsync("/Account/SetupTwoFactor", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = setupToken,
            ["Code"] = validCode
        }));

        // Assert that the controller redirects (302 Found) upon successful setup completion.
        Assert.True(response.StatusCode is System.Net.HttpStatusCode.Redirect or System.Net.HttpStatusCode.Found);

        // Verify state persistence in the database using a fresh service scope.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Heimevernet.Data.AppDbContext>();
        var updatedUser = await db.Users.FindAsync(user.Id);

        Assert.NotNull(updatedUser);
        Assert.True(updatedUser.TwoFactorEnabled);
    }

    /// <summary>
    /// Utility helper method to extract the Base32 TOTP secret key rendered in the SetupTwoFactor HTML view.
    /// </summary>
    /// <param name="html">The rendered HTML string of the 2FA setup page.</param>
    /// <returns>The extracted Base32 secret string.</returns>
    private static string ExtractSecretFromHtml(string html)
    {
        // Matches standard RFC 4648 Base32 secret strings (26–32 characters consisting of A-Z and 2-7).
        var match = System.Text.RegularExpressions.Regex.Match(html, @"[A-Z2-7]{26,32}");
        return match.Success ? match.Value : throw new InvalidOperationException("Secret not found in setup page HTML.");
    }
}