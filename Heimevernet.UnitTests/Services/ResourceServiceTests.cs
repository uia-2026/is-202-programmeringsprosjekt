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
    private readonly Mock<IResourceMapper> _mapperMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly ResourceService _sut;

    public ResourceServiceTests()
    {
        _repoMock = new Mock<IResourceRepository>();
        _mapperMock = new Mock<IResourceMapper>();
        _uowMock = new Mock<IUnitOfWork>();
        _sut = new ResourceService(_repoMock.Object, _mapperMock.Object, _uowMock.Object);
    }

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

    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Resources()
    {
        var emptyResources = new List<Resource>();
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emptyResources);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.Empty(result);
        _mapperMock.Verify(m => m.ToViewModel(It.IsAny<Resource>()), Times.Never);
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

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_NotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Resource?)null);

        var result = await _sut.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
        _mapperMock.Verify(m => m.ToViewModel(It.IsAny<Resource>()), Times.Never);
    }

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
}
