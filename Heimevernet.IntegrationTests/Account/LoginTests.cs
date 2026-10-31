using Heimevernet.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests verifying user authentication scenarios via the login page,
/// including validation handling, invalid credentials, and successful session establishment.
/// </summary>
public sealed class LoginTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public LoginTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that submitting an unregistered email address displays a generic error message
    /// rather than leaking whether the user exists.
    /// </summary>
    [Fact]
    public async Task Login_UnknownEmail_ShowsGenericError()
    {
        // Disable automatic redirects to capture the direct HTTP 200 response containing the form view.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Submit the login form with an un-registered email address.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = "unknown@example.com",
                ["Password"] = TestCredentials.Password
            });

        // Assert that the page re-renders (200 OK) with the form.
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Verify the generic error message is present in the rendered HTML.
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body);
    }

    /// <summary>
    /// Verifies that submitting empty form inputs triggers ModelState validation error messages.
    /// </summary>
    [Fact]
    public async Task Login_EmptyEmail_ShowsValidationError()
    {
        using var client = _factory.CreateClient();

        // Submit the login form missing the required Email parameter.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = "",
                ["Password"] = TestCredentials.Password
            });

        // Assert that the page re-renders (200 OK) with model validation errors.
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Check that the validation summary highlights the missing Email field.
        Assert.Contains("Email", body);
    }

    /// <summary>
    /// Verifies that providing an incorrect password fails authentication and does not issue a valid session cookie.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_DoesNotAuthenticate()
    {
        // Seed a valid user in the isolated test database.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Submit valid email with an incorrect password.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email,
                ["Password"] = "Wrong-password-123!"
            });

        // Expect the login view to re-render with an error.
        Assert.Equal(
            System.Net.HttpStatusCode.OK,
            response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body);

        // Confirm that subsequent requests to protected endpoints fail (redirect to login).
        using var protectedResponse = await client.GetAsync("/Account/Index");

        Assert.Equal(
            System.Net.HttpStatusCode.Redirect,
            protectedResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that valid user credentials authenticate the user, issue an auth cookie,
    /// and redirect to the home page.
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_RedirectsAndAuthenticates()
    {
        // Seed user in the test database.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        // Enable cookie handling so the auth cookie set by login is preserved for subsequent requests.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // Post valid credentials to the login route.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email,
                ["Password"] = TestCredentials.Password
            });

        // Assert that successful login returns a 302/Redirect status.
        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/", response.Headers.Location!.OriginalString);

        // Verify that using the client's cookie session grants access to protected routes.
        using var protectedResponse = await client.GetAsync("/Account/Index");

        Assert.True(
            protectedResponse.IsSuccessStatusCode,
            $"Expected authenticated access, got {protectedResponse.StatusCode}");
    }

    /// <summary>
    /// Verifies that an authenticated user can successfully retrieve their profile page (/Account/Index)
    /// and that the page contains user-specific details.
    /// </summary>
    [Fact]
    public async Task Index_AuthenticatedUser_ReturnsSuccess()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

        // Establish authenticated session via login.
        using var loginResponse = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email,
                ["Password"] = TestCredentials.Password
            });

        // Request the protected profile page.
        using var response = await client.GetAsync("/Account/Index");

        // Verify status code 200 OK and check that the username is rendered on the page.
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(user.Username, content);
    }
}