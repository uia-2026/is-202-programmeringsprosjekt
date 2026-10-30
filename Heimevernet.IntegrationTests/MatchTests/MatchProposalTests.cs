using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.MatchTests;

/// <summary>
/// Integration tests verifying Match proposal workflows, authorization checks,
/// duplicate prevention rules, status validation, and database state persistence.
/// </summary>
public sealed class MatchProposalTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;
    private readonly TestDataFactory _data;

    public MatchProposalTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new TestDataFactory(factory.Services);
    }

    /// <summary>
    /// Verifies that submitting a valid match proposal as the Need owner creates a new Match record
    /// with status UnderReview, updates the target Need status to UnderReview, and redirects to Need details.
    /// </summary>
    [Fact]
    public async Task Propose_ValidRequest_CreatesMatchAndRedirectsToNeedDetails()
    {
        // Arrange: Seed Need owner, Resource provider, entities, and establish authenticated client session.
        var owner = await _data.CreateUserAsync(role: UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await _data.CreateNeedAsync(owner.Id);
        var resourceId = await _data.CreateResourceAsync(provider.Id);

        var client = await AuthenticatedClientHelper.CreateAsync(_factory, owner.Email);

        // Act: POST valid proposal form payload.
        var response = await PostProposalAsync(client, needId, resourceId, 1);

        // Assert: Expect HTTP 302 Redirect pointing to the Need Details endpoint.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Need/Details/{needId}", response.Headers.Location!.OriginalString);

        // Verify Match entity created with correct attributes in database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches
            .SingleOrDefaultAsync(m => m.NeedId == needId && m.ResourceId == resourceId);

        Assert.NotNull(match);
        Assert.Equal(owner.Id, match.MatchedByUserId);
        Assert.Equal(1, match.Quantity);
        Assert.Equal(MatchStatus.UnderReview, match.Status);

        // Verify parent Need entity status updated to UnderReview.
        var need = await db.Needs.FindAsync(needId);

        Assert.NotNull(need);
        Assert.Equal(NeedStatus.UnderReview, need.Status);
    }

    /// <summary>
    /// Verifies idempotency / duplicate prevention: submitting a proposal for a Need-Resource pair
    /// that already has an existing proposal does not create a duplicate Match record.
    /// </summary>
    [Fact]
    public async Task Propose_DuplicateMatch_DoesNotCreateAnotherMatch()
    {
        // Arrange: Seed entities and authenticate as Need owner.
        var owner = await _data.CreateUserAsync(role: UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await _data.CreateNeedAsync(owner.Id);
        var resourceId = await _data.CreateResourceAsync(provider.Id);

        var client = await AuthenticatedClientHelper.CreateAsync(_factory, owner.Email);

        // Act: Submit the identical match proposal twice.
        var firstResponse = await PostProposalAsync(client, needId, resourceId, 1);
        var secondResponse = await PostProposalAsync(client, needId, resourceId, 1);

        // Assert: Both requests receive redirect responses.
        Assert.Equal(HttpStatusCode.Redirect, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, secondResponse.StatusCode);

        // Verify that exactly 1 match record exists in the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var matchCount = await db.Matches
            .CountAsync(m => m.NeedId == needId && m.ResourceId == resourceId);

        Assert.Equal(1, matchCount);
    }

    /// <summary>
    /// Verifies model validation rules: submitting a proposal with an invalid quantity (0)
    /// fails validation, redirects back to the proposal page, and creates no Match record.
    /// </summary>
    [Fact]
    public async Task Propose_InvalidQuantity_DoesNotCreateMatch()
    {
        // Arrange: Seed entities and authenticate client.
        var owner = await _data.CreateUserAsync(role: UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await _data.CreateNeedAsync(owner.Id);
        var resourceId = await _data.CreateResourceAsync(provider.Id);

        var client = await AuthenticatedClientHelper.CreateAsync(_factory, owner.Email);

        // Act: Submit proposal with zero quantity.
        var response = await PostProposalAsync(client, needId, resourceId, 0);

        // Assert: Expect redirect back to proposal form route.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Match/Propose", response.Headers.Location!.OriginalString);

        // Verify no Match was written to the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Matches.AnyAsync(
            m => m.NeedId == needId && m.ResourceId == resourceId));
    }

    /// <summary>
    /// Verifies that a non-owner cannot POST a match proposal for another user's Need.
    /// The request is redirected to AccessDenied, and no Match entity is persisted.
    /// </summary>
    [Fact]
    public async Task Propose_UserIsNotNeedOwner_DoesNotCreateMatch()
    {
        // Arrange: Seed actual Need owner, a separate non-owner actor, and a Resource provider.
        var owner = await _data.CreateUserAsync(role: UserRole.PublicActor);
        var anotherActor = await _data.CreateUserAsync(role: UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await _data.CreateNeedAsync(owner.Id);
        var resourceId = await _data.CreateResourceAsync(provider.Id);

        var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            anotherActor.Email);

        // Act: Post proposal form as the non-owner actor (extracting token from GET /Account/Login).
        var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Match/Propose?needId={needId}",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["needId"] = needId.ToString(),
                ["resourceId"] = resourceId.ToString(),
                ["quantity"] = "1"
            });

        // Assert: Forbid() is converted into an HTTP 302 Redirect to AccessDenied by Cookie Auth.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.OriginalString);

        // Verify database remains clean with no match created.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Matches.AnyAsync(
            m => m.NeedId == needId && m.ResourceId == resourceId));
    }

    /// <summary>
    /// Verifies business status rules: proposing a match against a Resource that is marked Busy
    /// is rejected, redirecting back to the form and preventing Match creation.
    /// </summary>
    [Fact]
    public async Task Propose_ResourceIsBusy_DoesNotCreateMatch()
    {
        // Arrange: Seed owner, provider, entities, and update Resource status to Busy in database.
        var owner = await _data.CreateUserAsync(role: UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await _data.CreateNeedAsync(owner.Id);
        var resourceId = await _data.CreateResourceAsync(provider.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var resource = await db.Resources.FindAsync(resourceId);

            Assert.NotNull(resource);
            resource.Status = ResourceStatus.Busy;

            await db.SaveChangesAsync();
        }

        var client = await AuthenticatedClientHelper.CreateAsync(_factory, owner.Email);

        // Act: Attempt to propose match with Busy resource.
        var response = await PostProposalAsync(client, needId, resourceId, 1);

        // Assert: Expect redirect back to proposal form due to resource status conflict.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Match/Propose", response.Headers.Location!.OriginalString);

        // Verify state persistence in an isolated verification DbContext scope.
        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await verificationDb.Matches.AnyAsync(
            m => m.NeedId == needId && m.ResourceId == resourceId));
    }

    /// <summary>
    /// Helper method issuing a POST request to <c>/Match/Propose</c> with anti-forgery tokens included.
    /// </summary>
    private static Task<HttpResponseMessage> PostProposalAsync(
        HttpClient client,
        int needId,
        int resourceId,
        int quantity)
    {
        var url = $"/Match/Propose?needId={needId}";

        var formData = new Dictionary<string, string>
        {
            ["needId"] = needId.ToString(),
            ["resourceId"] = resourceId.ToString(),
            ["quantity"] = quantity.ToString()
        };

        return AntiforgeryHelper.PostFormAsync(
            client,
            url,
            url,
            formData);
    }
}