using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Helper method instantiating an HTTP client configured with cookie authentication 
/// state for the specified test user.
/// </summary>
public static class AuthenticatedClientHelper
{
    public static async Task<HttpClient> CreateAsync(
        HeimevernetWebApplicationFactory factory,
        string email)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = TestCredentials.Password
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        return client;
    }
}
