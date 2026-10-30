
using System.Net;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;

namespace Heimevernet.IntegrationTests.NeedTests;

public class EditNeedTests(
    HeimevernetWebApplicationFactory factory)
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory = factory;

    [Fact]
    public async Task Edit_Get_Owner_ReturnsEditView()
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var needId = await data.CreateNeedAsync(user.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            user.Email);

        // Act
        using var response = await client.GetAsync($"/Need/Edit/{needId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Edit", content);
    }

    [Fact]
    public async Task Edit_Get_AnotherPublicActor_IsForbidden()
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync();
        var anotherUser = await data.CreateUserAsync();
        var needId = await data.CreateNeedAsync(owner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            anotherUser.Email);

        // Act
        using var response = await client.GetAsync($"/Need/Edit/{needId}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Edit_Get_ResourceProvider_IsForbidden()
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        var needId = await data.CreateNeedAsync(owner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            provider.Email);

        // Act
        using var response = await client.GetAsync($"/Need/Edit/{needId}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Edit_Get_NonexistentNeed_ReturnsNotFound()
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            user.Email);

        // Act
        using var response = await client.GetAsync("/Need/Edit/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_AnonymousUser_RedirectsToLogin()
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync();
        var needId = await data.CreateNeedAsync(owner.Id);

        using var client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act
        using var response = await client.GetAsync($"/Need/Edit/{needId}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Login", response.Headers.Location?.OriginalString);
    }
}
