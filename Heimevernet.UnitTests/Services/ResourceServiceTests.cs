using Heimevernet.Mappers;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.ViewModels.Resource;
using Moq;

namespace Heimevernet.UnitTests.Services;

public class ResourceServiceTests
{
    private readonly Mock<IResourceRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ResourceMapper _realMapper;
    private readonly ResourceService _sut;

    public ResourceServiceTests()
    {
        _repoMock = new Mock<IResourceRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _realMapper = new ResourceMapper();
        _sut = new ResourceService(_repoMock.Object, _realMapper, _uowMock.Object);
    }

    [Fact]
    public async Task AddAsync_Saves_Entity_And_Commits()
    {
        var model = new ResourceCreateViewModel
        {
            CategoryId = 1,
            Title = "Shelter",
            Region = "Agder",
            ContactPoint = "contact",
            AvailableFrom = new DateTime(2026, 05, 01)
        };

        await _sut.AddAsync(model, 42, CancellationToken.None);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddAsync_Does_Not_Commit_When_Repository_Throws()
    {
        var model = new ResourceCreateViewModel { CategoryId = 1, Title = "Title", Region = "R", ContactPoint = "C", AvailableFrom = DateTime.UtcNow };
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddAsync(model, 1, CancellationToken.None));

        _uowMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task AddAsync_Maps_Trimmed_Values_Before_Saving()
    {
        var from = new DateTime(2026, 06, 01, 08, 00, 00, DateTimeKind.Utc);
        var to = new DateTime(2026, 06, 10, 18, 00, 00, DateTimeKind.Utc);
        var model = new ResourceCreateViewModel
        {
            CategoryId = 5,
            Title = "  Truck  ",
            Description = "  desc  ",
            Latitude = 10,
            Longitude = 20,
            Region = "  Region  ",
            AvailableFrom = from,
            AvailableTo = to,
            ContactPoint = "  cp  "
        };
        Resource? captured = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((res, _) => captured = res)
            .Returns(Task.CompletedTask);

        await _sut.AddAsync(model, 99, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(99, captured!.UserId);
        Assert.Equal(5, captured.CategoryId);
        Assert.Equal("Truck", captured.Title);
        Assert.Equal("desc", captured.Description);
        Assert.Equal("Region", captured.Region);
        Assert.Equal("cp", captured.ContactPoint);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Mapped_ViewModels()
    {
        var resources = new List<Resource>
        {
            new Resource { Id = 1, Title = "Truck", Category = new Category { Name = "Logistics" }, Region = "Oslo", ContactPoint = "cp1", AvailableFrom = new DateTime(2026, 01, 01), Status = ResourceStatus.Available, CreatedAt = DateTime.UtcNow },
            new Resource { Id = 2, Title = "Medkit", Category = new Category { Name = "Medical" }, Region = "Bergen", ContactPoint = "cp2", AvailableFrom = new DateTime(2026, 02, 01), Status = ResourceStatus.Busy, CreatedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(resources);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Truck", result[0].Title);
        Assert.Equal("Logistics", result[0].CategoryName);
        Assert.Equal("Medkit", result[1].Title);
        Assert.Equal("Medical", result[1].CategoryName);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Resources()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Resource>());

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
    }

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
            AvailableFrom = DateTime.UtcNow,
            Status = ResourceStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var result = await _sut.GetByIdAsync(5, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal("Found", result.Title);
        Assert.Equal("Cat", result.CategoryName);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_NotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

}
