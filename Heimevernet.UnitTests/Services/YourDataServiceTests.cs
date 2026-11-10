using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Moq;

namespace Heimevernet.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="YourDataService"/> covering the overview and export.
/// </summary>
public sealed class YourDataServiceTests
{
    private const int UserId = 42;

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IAuditRepository> _auditRepositoryMock = new();
    private readonly YourDataService _sut;

    public YourDataServiceTests()
    {
        _auditRepositoryMock
            .Setup(r => r.GetByActorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _sut = new YourDataService(
            _userRepositoryMock.Object,
            _auditRepositoryMock.Object);
    }

    [Fact]
    public async Task GetOverviewAsync_UnknownUser_ReturnsNull()
    {
        var result = await _sut.GetOverviewAsync(UserId);

        Assert.Null(result);
        _auditRepositoryMock.Verify(r => r.GetByActorAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOverviewAsync_MapsAccountFieldsFromUser()
    {
        SetupUser(CreateUser(twoFactorEnabled: true));

        var result = await _sut.GetOverviewAsync(UserId);

        Assert.NotNull(result);
        Assert.Equal("kari", result.Username);
        Assert.Equal("kari@example.com", result.Email);
        Assert.Equal(UserRole.ResourceProvider, result.Role);
        Assert.True(result.TwoFactorEnabled);
    }

    [Fact]
    public async Task GetOverviewAsync_QueriesOnlyTheUsersOwnActivity_AndKeepsNewestFirstOrder()
    {
        SetupUser(CreateUser());
        var newest = DateTime.UtcNow;
        _auditRepositoryMock
            .Setup(r => r.GetByActorAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Audit(AuditAction.TwoFactorEnabled, "User", UserId, newest),
                Audit(AuditAction.LoginSucceeded, "User", UserId, newest.AddMinutes(-5))
            ]);

        var result = await _sut.GetOverviewAsync(UserId);

        Assert.NotNull(result);
        Assert.Equal(
            ["Two-factor authentication enabled", "Signed in"],
            result.Activity.Select(a => a.Label));
        Assert.Equal(newest, result.Activity[0].OccurredAt);
        _auditRepositoryMock.Verify(r => r.GetByActorAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOverviewAsync_LabelsListingActivityWithRegion_OrIdWhenListingIsGone()
    {
        var user = CreateUser();
        user.Needs.Add(new Need { Id = 7, Region = "Agder" });
        user.Resources.Add(new Resource { Id = 9, Region = "Rogaland" });
        SetupUser(user);
        _auditRepositoryMock
            .Setup(r => r.GetByActorAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Audit(AuditAction.NeedCreated, "Need", 7),
                Audit(AuditAction.ResourceEdited, "Resource", 9),
                Audit(AuditAction.NeedDeleted, "Need", 123),
                Audit(AuditAction.MatchProposed, "Match", 55)
            ]);

        var result = await _sut.GetOverviewAsync(UserId);

        Assert.NotNull(result);
        Assert.Equal(
            [
                "Need created — Agder",
                "Resource edited — Rogaland",
                "Need deleted — #123",
                "Match proposed — #55"
            ],
            result.Activity.Select(a => a.Label));
    }

    [Fact]
    public async Task ExportAsync_IncludesAccountListingsAndActivity()
    {
        var user = CreateUser();
        user.Needs.Add(new Need { Id = 1, Title = "Water", Region = "Agder", Category = new Category { Name = "Supplies" } });
        user.Resources.Add(new Resource { Id = 2, Title = "Generator", Region = "Agder" });
        SetupUser(user);
        _auditRepositoryMock
            .Setup(r => r.GetByActorAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Audit(AuditAction.NeedCreated, "Need", 1)]);

        var result = await _sut.ExportAsync(UserId);

        Assert.NotNull(result);
        Assert.Equal(UserId, result.Account.Id);
        Assert.Equal("kari@example.com", result.Account.Email);
        var need = Assert.Single(result.Needs);
        Assert.Equal("Water", need.Title);
        Assert.Equal("Supplies", need.Category);
        Assert.Equal("Generator", Assert.Single(result.Resources).Title);
        Assert.Equal(AuditAction.NeedCreated, Assert.Single(result.Activity).Action);
    }

    [Fact]
    public async Task ExportAsync_UnknownUser_ReturnsNull()
    {
        Assert.Null(await _sut.ExportAsync(UserId));
    }

    private void SetupUser(User user) =>
        _userRepositoryMock
            .Setup(r => r.GetWithOwnedDataAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private static User CreateUser(bool twoFactorEnabled = false) => new()
    {
        Id = UserId,
        Username = "kari",
        Email = "kari@example.com",
        Role = UserRole.ResourceProvider,
        ActorType = "Farmer",
        TwoFactorEnabled = twoFactorEnabled,
        PasswordHash = "hash",
        TwoFactorSecret = twoFactorEnabled ? "SECRET" : null
    };

    private static AuditLog Audit(AuditAction action, string entityType, int? entityId, DateTime? occurredAt = null) => new()
    {
        ActorUserId = UserId,
        ActorName = "kari",
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        OccurredAt = occurredAt ?? DateTime.UtcNow
    };
}
