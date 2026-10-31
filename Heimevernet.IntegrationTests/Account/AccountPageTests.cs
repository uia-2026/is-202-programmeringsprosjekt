using Heimevernet.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests verifying routing, HTTP status codes, and authorization rules
/// for public and protected account pages.
/// </summary>
public sealed class AccountPageTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public AccountPageTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that unauthenticated users can access publicly accessible endpoints
    /// and receive successful HTTP status codes (200 OK).
    /// </summary>
    /// <param name="path">The relative URL path under test.</param>
    [Theory]
    [InlineData("/Account/Register")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/AccessDenied")]
    public async Task PublicPages_ReturnSuccess(string path)
    {
        // Create an unauthenticated HTTP client connected to the test server.
        using var client = _factory.CreateClient();

        // Send a GET request to the public endpoint.
        using var response = await client.GetAsync(path);

        // Assert that the page rendered successfully (200 OK).
        Assert.True(
            response.IsSuccessStatusCode,
            $"{path} returned {(int)response.StatusCode}");
    }

    /// <summary>
    /// Verifies that unauthenticated (anonymous) users attempting to access protected endpoints
    /// are intercepted and redirected to the login page (302 Found).
    /// </summary>
    /// <param name="path">The relative URL path under test.</param>
    [Theory]
    [InlineData("/Account/Index")]
    [InlineData("/Account/SetupTwoFactor")]
    public async Task ProtectedPages_AnonymousUser_RedirectToLogin(
        string path)
    {
        // Disable automatic HTTP redirects so the test can inspect the initial 302 response and Location header.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Attempt to access the protected endpoint anonymously.
        using var response = await client.GetAsync(path);

        // Assert that the server responded with an HTTP redirect (302 Found or 301 Moved Permanently).
        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        // Assert that the redirect location points to the login route.
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }
}