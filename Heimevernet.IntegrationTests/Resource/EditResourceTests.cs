using System.Net;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Heimevernet.IntegrationTests.ResourceTests;

/// <summary>
/// Integration tests verifying authorization, ownership rules, and response handling 
/// for the Resource edit GET view endpoint (<c>/Resource/Edit/{id}</c>).
/// </summary>
public class EditResourceTests(
    HeimevernetWebApplicationFactory factory)
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory = factory;

    /// <summary>
    /// Verifies that resource owners across allowed roles (PublicActor, ResourceProvider)
    /// can successfully access their own resource edit view (HTTP 200 OK).
    /// </summary>
    /// <param name="role">The user role of the resource owner being tested.</param>
    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task Edit_Get_Owner_ReturnsEditView(UserRole role)
    {
        // Arrange: Seed resource owner and a target resource, then build an authenticated client session.
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync(role: role);

        var resourceId = await data.CreateResourceAsync(owner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            owner.Email);

        // Act: Request the edit page GET endpoint as the resource owner.
        using var response = await client.GetAsync(
            $"/Resource/Edit/{resourceId}");

        // Assert: Expect HTTP 200 OK and verify the returned HTML contains the edit view title or context.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Edit", content);
    }

    /// <summary>
    /// Verifies that an authenticated user who is NOT the owner of the resource is forbidden
    /// from accessing the edit page and is redirected to Access Denied.
    /// </summary>
    /// <param name="ownerRole">The role assigned to the resource owner entity.</param>
    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task Edit_Get_AnotherUser_IsForbidden(UserRole ownerRole)
    {
        // Arrange: Seed resource owner, target resource, and a separate non-owner user, then authenticate as non-owner.
        var data = new TestDataFactory(_factory.Services);

        var owner = await data.CreateUserAsync(role: ownerRole);
        var anotherUser = await data.CreateUserAsync(
            role: ownerRole == UserRole.PublicActor
                ? UserRole.ResourceProvider
                : UserRole.PublicActor);

        var resourceId = await data.CreateResourceAsync(owner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            anotherUser.Email);

        // Act: Attempt to access the resource edit page as the non-owner user.
        using var response = await client.GetAsync(
            $"/Resource/Edit/{resourceId}");

        // Assert: Expect 302 Redirect to the AccessDenied path.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "AccessDenied",
            response.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// Verifies that requesting the edit page for a non-existent resource ID returns HTTP 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Edit_Get_NonexistentResource_ReturnsNotFound()
    {
        // Arrange: Seed an authenticated user session and target invalid ID 999999.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(
            role: UserRole.PublicActor);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            user.Email);

        // Act: Send request for a resource ID that does not exist in the database.
        using var response = await client.GetAsync(
            "/Resource/Edit/999999");

        // Assert: Expect HTTP 404 Not Found response.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that unauthenticated anonymous requests attempting to access the edit view
    /// are intercepted and redirected to the login page.
    /// </summary>
    [Fact]
    public async Task Edit_Get_AnonymousUser_RedirectsToLogin()
    {
        // Arrange: Create an unauthenticated client with auto-redirects disabled.
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act: Attempt an anonymous GET request to the resource edit endpoint.
        using var response = await client.GetAsync(
            "/Resource/Edit/1");

        // Assert: Expect 302 Redirect pointing to the Login route.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Login",
            response.Headers.Location?.OriginalString);
    }
}