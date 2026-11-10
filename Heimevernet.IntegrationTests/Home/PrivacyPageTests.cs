using Heimevernet.IntegrationTests.Infrastructure;

namespace Heimevernet.IntegrationTests.Home;

/// <summary>
/// Integration tests verifying that the privacy policy is publicly reachable.
/// </summary>
public sealed class PrivacyPageTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public PrivacyPageTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that an anonymous visitor can open /Privacy without being
    /// redirected to the login page.
    /// </summary>
    [Fact]
    public async Task Privacy_AnonymousUser_ReturnsSuccess()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/Privacy");
        var html = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"/Privacy returned {(int)response.StatusCode}");
        Assert.Contains("Privacy &amp; data", html);
    }
}
