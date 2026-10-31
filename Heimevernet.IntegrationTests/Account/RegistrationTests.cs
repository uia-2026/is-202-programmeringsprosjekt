using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests verifying user registration workflows, model state validation,
/// duplicate user handling, and database persistence logic.
/// </summary>
public sealed class RegistrationTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public RegistrationTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that submitting invalid or mismatched registration data (e.g., malformed email,
    /// mismatched passwords) fails model validation, re-renders the form, and prevents database insertion.
    /// </summary>
    [Fact]
    public async Task Register_InvalidInput_DoesNotCreateUser()
    {
        using var client = _factory.CreateClient();

        // Submit form data containing invalid fields (blank username, bad email format, mismatched password).
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Register",
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["Username"] = "",
                ["Email"] = "not-an-email",
                ["Password"] = "x",
                ["ConfirmPassword"] = "different",
                ["Role"] = "PublicActor",
                ["ActorType"] = "Municipality"
            });

        // Assert that the page re-renders (200 OK) to display validation errors.
        Assert.Equal(
            System.Net.HttpStatusCode.OK,
            response.StatusCode);

        // Query the database in an isolated scope to verify no record was persisted.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Users.AnyAsync(
            u => u.Email == "not-an-email"));
    }

    /// <summary>
    /// Verifies that attempting to register with an email address already bound to an existing account
    /// triggers a domain error and prevents duplicate user record creation.
    /// </summary>
    [Fact]
    public async Task Register_ExistingEmail_DoesNotCreateDuplicateUser()
    {
        // Seed an initial user in the database.
        var data = new TestDataFactory(_factory.Services);
        var existingUser = await data.CreateUserAsync();

        using var client = _factory.CreateClient();

        // Submit a registration attempt using the existing user's email address.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Register",
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["Username"] = "another-user",
                ["Email"] = existingUser.Email,
                ["Password"] = TestCredentials.Password,
                ["ConfirmPassword"] = TestCredentials.Password,
                ["Role"] = "PublicActor",
                ["ActorType"] = "Municipality"
            });

        // The controller should return HTTP 200 OK with a custom validation error rather than creating a second account.
        Assert.Equal(
            System.Net.HttpStatusCode.OK,
            response.StatusCode);

        // Verify in the database that the total count for this email remains exactly 1.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(
            1,
            await db.Users.CountAsync(u => u.Email == existingUser.Email));
    }

    /// <summary>
    /// Verifies that submitting valid registration details successfully creates a user record in the database,
    /// hashes the password securely, and redirects to the login route.
    /// </summary>
    [Fact]
    public async Task Register_ValidInput_CreatesUserAndRedirectsToLogin()
    {
        // Generate unique identifiers for non-conflicting test data.
        var email = $"registration-{Guid.NewGuid():N}@example.com";

        // Disable automatic redirects to inspect the initial 302 Found response.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Submit valid registration details.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Register",
            "/Account/Register",
            new Dictionary<string, string>
            {
                ["Username"] = $"registration-{Guid.NewGuid():N}",
                ["Email"] = email,
                ["Password"] = TestCredentials.Password,
                ["ConfirmPassword"] = TestCredentials.Password,
                ["Role"] = "PublicActor",
                ["ActorType"] = "Municipality"
            });

        // Assert that the user is redirected to the login page upon successful account creation.
        Assert.True(
            response.StatusCode is System.Net.HttpStatusCode.Redirect
                or System.Net.HttpStatusCode.Found);

        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Verify user persistence and ensure password is properly hashed (never stored in plain text).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        Assert.NotNull(user);
        Assert.NotEqual(TestCredentials.Password, user.PasswordHash);
    }
}