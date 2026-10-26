using Heimevernet.Mappers;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;
using Moq;

namespace Heimevernet.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="Heimevernet.Services.ResourceService"/>.
/// These tests verify orchestration between the repository, mapper and unit of work.
/// </summary>
public class ResourceServiceTests
{
    private readonly Mock<IResourceRepository> _repoMock;
    private readonly Mock<IResourceMapper> _mapperMock;
    private readonly Mock<IMapMarkerMapper> _mapMarkerMapperMock;
    private readonly Mock<INearbyResourceMapper> _mapNearbyResourceMapperMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ResourceService _sut;

    public ResourceServiceTests()
    {
        _repoMock = new Mock<IResourceRepository>();
        _mapperMock = new Mock<IResourceMapper>();
        _mapMarkerMapperMock = new Mock<IMapMarkerMapper>();
        _mapNearbyResourceMapperMock = new Mock<INearbyResourceMapper>();
        _uowMock = new Mock<IUnitOfWork>();
        _sut = new ResourceService(_repoMock.Object, _mapperMock.Object, _mapMarkerMapperMock.Object, _mapNearbyResourceMapperMock.Object, _uowMock.Object);
    }

    /// <summary>
    /// Ensures CreateAsync maps the create model, saves the entity and commits the unit of work.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Saves_Entity_And_Commits()
    {
        var model = new ResourceCreateViewModel
        {
            CategoryId = 1,
            Title = "Shelter",
            Region = "Agder",
            ContactPoint = "contact",
            AvailableFrom = new DateTime(2026, 05, 01)
        };
        var mapped = new Resource { Id = 1, Title = "Shelter" };
        _mapperMock.Setup(m => m.ToEntity(model, 42)).Returns(mapped);

        await _sut.CreateAsync(model, 42, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(mapped, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that when the repository throws, the unit of work is not committed.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Does_Not_Commit_When_Repository_Throws()
    {
        var model = new ResourceCreateViewModel { CategoryId = 1, Title = "Title", Region = "R", ContactPoint = "C", AvailableFrom = DateTime.UtcNow };
        var mapped = new Resource { Id = 1, Title = "Title" };
        _mapperMock.Setup(m => m.ToEntity(model, 1)).Returns(mapped);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(model, 1, CancellationToken.None));

        _uowMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    /// <summary>
    /// Ensures CreateAsync calls the mapper and forwards the mapped entity to the repository.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Calls_Mapper_And_Forwards_Result_To_Repository()
    {
        var model = new ResourceCreateViewModel
        {
            CategoryId = 5,
            Title = "Truck",
            Region = "Region",
            ContactPoint = "cp",
            AvailableFrom = new DateTime(2026, 06, 01)
        };
        var mapped = new Resource { Id = 99, Title = "Mapped" };
        _mapperMock.Setup(m => m.ToEntity(model, 99)).Returns(mapped);

        await _sut.CreateAsync(model, 99, CancellationToken.None);

        _mapperMock.Verify(m => m.ToEntity(model, 99), Times.Once);
        _repoMock.Verify(r => r.AddAsync(mapped, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies GetAllAsync returns mapped view models for all resources.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns_Mapped_ViewModels()
    {
        var resources = new List<Resource>
        {
            new Resource { Id = 1, Title = "Truck", Category = new Category { Name = "Logistics" }, Region = "Oslo" },
            new Resource { Id = 2, Title = "Medkit", Category = new Category { Name = "Medical" }, Region = "Bergen" }
        };
        var expected = new List<ResourceViewModel>
        {
            new ResourceViewModel { Id = 1, Title = "Truck", CategoryName = "Logistics" },
            new ResourceViewModel { Id = 2, Title = "Medkit", CategoryName = "Medical" }
        };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(resources);
        _mapperMock.Setup(m => m.ToViewModel(resources[0])).Returns(expected[0]);
        _mapperMock.Setup(m => m.ToViewModel(resources[1])).Returns(expected[1]);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Truck", result[0].Title);
        Assert.Equal("Logistics", result[0].CategoryName);
        Assert.Equal("Medkit", result[1].Title);
        Assert.Equal("Medical", result[1].CategoryName);
    }

    /// <summary>
    /// Verifies GetAllAsync returns an empty collection when no resources exist.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Resources()
    {
        var emptyResources = new List<Resource>();
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emptyResources);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
        _mapperMock.Verify(m => m.ToViewModel(It.IsAny<Resource>()), Times.Never);
    }

    /// <summary>
    /// Verifies GetByIdAsync returns the mapped view model when the entity exists.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Returns_Mapped_ViewModel_When_Found()
    {
        var resource = new Resource
        {
            Id = 5,
            Title = "Found",
            Category = new Category { Name = "Cat" },
            Region = "R",
            ContactPoint = "CP",
            AvailableFrom = DateTime.UtcNow
        };
        var expected = new ResourceViewModel { Id = 5, Title = "Found", CategoryName = "Cat" };
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _mapperMock.Setup(m => m.ToViewModel(resource)).Returns(expected);

        var result = await _sut.GetByIdAsync(5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal("Found", result.Title);
        Assert.Equal("Cat", result.CategoryName);
    }

    /// <summary>
    /// Verifies GetByIdAsync returns null when the entity is not found.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_NotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
        _mapperMock.Verify(m => m.ToViewModel(It.IsAny<Resource>()), Times.Never);
    }

    /// <summary>
    /// Ensures the cancellation token is forwarded to the repository GetAllAsync call.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Forwards_CancellationToken_To_Repository()
    {
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;
        var resources = new List<Resource>();
        _repoMock.Setup(r => r.GetAllAsync(It.Is<CancellationToken>(t => t == expectedToken))).ReturnsAsync(resources);

        var result = await _sut.GetAllAsync(expectedToken);

        Assert.Empty(result);
        _repoMock.Verify(r => r.GetAllAsync(It.Is<CancellationToken>(t => t == expectedToken)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Owner_Can_Edit_When_Allowed()
    {
        var resource = new Resource
        {
            Id = 7,
            UserId = 42,
            Status = ResourceStatus.Available,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Matches = []
        };

        var model = new ResourceCreateViewModel
        {
            CategoryId = 1,
            Title = "Updated",
            Region = "Agder",
            ContactPoint = "cp",
            AvailableFrom = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var (result, _) = await _sut.UpdateAsync(7, model, 42);

        Assert.Equal(OwnedRecordMutationResult.Success, result);
        _mapperMock.Verify(m => m.ApplyEditableFields(resource, model), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Forbidden_For_NonOwner()
    {
        var resource = new Resource { Id = 7, UserId = 1, Matches = [] };
        var model = new ResourceCreateViewModel { CategoryId = 1, Title = "T", Region = "R", ContactPoint = "C", AvailableFrom = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var (result, _) = await _sut.UpdateAsync(7, model, 2);

        Assert.Equal(OwnedRecordMutationResult.Forbidden, result);
        _mapperMock.Verify(m => m.ApplyEditableFields(It.IsAny<Resource>(), It.IsAny<ResourceCreateViewModel>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Conflict_When_Protected_Field_Changes_With_Pending_Match()
    {
        var resource = new Resource
        {
            Id = 7,
            UserId = 42,
            CategoryId = 1,
            Region = "Agder",
            AvailableFrom = new DateTime(2026, 01, 01),
            Matches = [new Heimevernet.Models.Match { Status = MatchStatus.UnderReview }]
        };

        var model = new ResourceCreateViewModel
        {
            CategoryId = 2,
            Title = "T",
            Region = "Agder",
            ContactPoint = "C",
            AvailableFrom = resource.AvailableFrom
        };

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var (result, _) = await _sut.UpdateAsync(7, model, 42);

        Assert.Equal(OwnedRecordMutationResult.Conflict, result);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Owner_Can_Delete_When_Allowed()
    {
        var resource = new Resource { Id = 7, UserId = 42, Matches = [] };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var (result, _) = await _sut.DeleteAsync(7, 42);

        Assert.Equal(OwnedRecordMutationResult.Success, result);
        _repoMock.Verify(r => r.Remove(resource), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_Conflict_When_Assigned_Match_Exists()
    {
        var resource = new Resource
        {
            Id = 7,
            UserId = 42,
            Matches = [new Heimevernet.Models.Match { Status = MatchStatus.Assigned }]
        };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var (result, _) = await _sut.DeleteAsync(7, 42);

        Assert.Equal(OwnedRecordMutationResult.Conflict, result);
        _repoMock.Verify(r => r.Remove(It.IsAny<Resource>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Does_Not_Modify_Owner_Status_Or_CreatedAt()
    {
        var createdAt = DateTime.UtcNow.AddDays(-3);
        var resource = new Resource
        {
            Id = 7,
            UserId = 42,
            Status = ResourceStatus.Busy,
            CreatedAt = createdAt,
            Matches = []
        };

        var model = new ResourceCreateViewModel
        {
            CategoryId = 1,
            Title = "Updated",
            Region = "Agder",
            ContactPoint = "cp",
            AvailableFrom = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _mapperMock.Setup(m => m.ApplyEditableFields(resource, model)).Callback(() => resource.Title = "Updated");

        await _sut.UpdateAsync(7, model, 42);

        Assert.Equal(42, resource.UserId);
        Assert.Equal(ResourceStatus.Busy, resource.Status);
        Assert.Equal(createdAt, resource.CreatedAt);
    }
}
