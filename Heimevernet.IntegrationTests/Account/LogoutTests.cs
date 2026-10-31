using Heimevernet.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests verifying user logout functionality, ensuring POST requests correctly terminate 
/// cookie sessions while HTTP GET requests do not perform state-changing logout actions.
/// </summary>
public sealed class LogoutTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public LogoutTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that submitting a valid POST request to the logout endpoint revokes the user's auth session cookie
    /// and prevents subsequent access to protected endpoints.
    /// </summary>
    [Fact]
    public async Task Logout_Post_EndsAuthenticatedSession()
    {
        // Seed a user in the isolated test database.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        // Configure client to handle cookies (so session state persists across calls)
        // and disable auto-redirects (to inspect status codes and Location headers directly).
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // 1. Log in to establish an active authentication session.
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

        // 2. Verify that the session is active by accessing a protected page.
        using var beforeLogout = await client.GetAsync("/Account/SetupTwoFactor");

        Assert.Equal(
            System.Net.HttpStatusCode.OK,
            beforeLogout.StatusCode);

        // 3. Extract the antiforgery token from an authenticated page rendering a form.
        var token = await AntiforgeryHelper.GetTokenAsync(
            client,
            "/Account/SetupTwoFactor");

        // 4. Submit a POST request to /Account/Logout including the antiforgery token.
        using var logoutResponse = await client.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token
                }));

        // Assert that the controller responds with a redirect back to Home/Login.
        Assert.True(
            logoutResponse.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        // 5. Attempt to access a protected page post-logout to confirm session revocation.
        using var afterLogout = await client.GetAsync("/Account/Index");

        // Expect a 302 Found redirecting unauthenticated users to the Login view.
        Assert.Equal(
            System.Net.HttpStatusCode.Redirect,
            afterLogout.StatusCode);

        Assert.Contains(
            "/Account/Login",
            afterLogout.Headers.Location?.OriginalString ?? string.Empty);
    }

    /// <summary>
    /// Verifies that issuing an HTTP GET request to /Account/Logout does not terminate the user's session,
    /// adhering to HTTP verb safety rules (state-changing operations must require POST).
    /// </summary>
    [Fact]
    public async Task Logout_Get_DoesNotEndAuthenticatedSession()
    {
        // Seed test user.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // 1. Establish an authenticated session via login.
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

        // 2. Issue an HTTP GET request to /Account/Logout.
        using var response = await client.GetAsync("/Account/Logout");

        // 3. Confirm authentication remains intact by attempting to access a protected page.
        using var protectedResponse = await client.GetAsync("/Account/SetupTwoFactor");

        // Should return 200 OK since GET /Account/Logout must not modify authentication state.
        Assert.Equal(
            System.Net.HttpStatusCode.OK,
            protectedResponse.StatusCode);
    }
}