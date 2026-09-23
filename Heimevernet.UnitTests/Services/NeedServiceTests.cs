using Heimevernet.Mappers;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.ViewModels.Need;
using Moq;

namespace Heimevernet.UnitTests.Services;

public class NeedServiceTests
{
    private readonly Mock<INeedRepository> _needRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly NeedMapper _realMapper;
    private readonly NeedService _sut;

    public NeedServiceTests()
    {
        _needRepoMock = new Mock<INeedRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _realMapper = new NeedMapper();
        _sut = new NeedService(_needRepoMock.Object, _realMapper, _uowMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Saves_Entity_And_Commits()
    {
        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "Water",
            Region = "Agder",
            ContactPoint = "contact",
            Priority = NeedPriority.Urgent
        };

        await _sut.CreateAsync(model, 42);

        _needRepoMock.Verify(r => r.AddAsync(It.IsAny<Need>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Does_Not_Commit_When_Repository_Throws()
    {
        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "Title",
            Region = "R",
            ContactPoint = "C"
        };
        _needRepoMock.Setup(r => r.AddAsync(It.IsAny<Need>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(model, 1));

        _uowMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Maps_Trimmed_Values_Before_Saving()
    {
        var model = new NeedCreateViewModel
        {
            CategoryId = 5,
            Title = "  Food  ",
            Description = "  desc  ",
            Latitude = 10,
            Longitude = 20,
            Region = "  Region  ",
            Priority = NeedPriority.Planned,
            Deadline = new DateTime(2026, 12, 31),
            ContactPoint = "  cp  "
        };

        Need? captured = null;
        _needRepoMock.Setup(r => r.AddAsync(It.IsAny<Need>()))
            .Callback<Need>(n => captured = n)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(model, 99);

        Assert.NotNull(captured);
        Assert.Equal(99, captured!.UserId);
        Assert.Equal(5, captured.CategoryId);
        Assert.Equal("Food", captured.Title);
        Assert.Equal("desc", captured.Description);
        Assert.Equal("Region", captured.Region);
        Assert.Equal("cp", captured.ContactPoint);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Mapped_ViewModels()
    {
        var needs = new List<Need>
        {
            new Need { Id = 1, Title = "A", Category = new Category { Name = "C1" }, Region = "R1", ContactPoint = "CP1", Priority = NeedPriority.Planned, Status = NeedStatus.New, CreatedAt = DateTime.UtcNow },
            new Need { Id = 2, Title = "B", Category = new Category { Name = "C2" }, Region = "R2", ContactPoint = "CP2", Priority = NeedPriority.Urgent, Status = NeedStatus.Assigned, CreatedAt = DateTime.UtcNow }
        };
        _needRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(needs);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Title);
        Assert.Equal("C1", result[0].CategoryName);
        Assert.Equal("B", result[1].Title);
        Assert.Equal("C2", result[1].CategoryName);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Needs()
    {
        _needRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Need>());

        var result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSummariesAsync_Returns_Mapped_Summaries()
    {
        var needs = new List<Need>
        {
            new Need { Id = 1, Title = "A", Category = new Category { Name = "C1" }, Region = "R1", Priority = NeedPriority.Planned, Status = NeedStatus.New },
            new Need { Id = 2, Title = "B", Category = new Category { Name = "C2" }, Region = "R2", Priority = NeedPriority.Urgent, Status = NeedStatus.Resolved }
        };
        _needRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(needs);

        var result = (await _sut.GetSummariesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Title);
        Assert.Equal("C1", result[0].CategoryName);
        Assert.Equal(NeedPriority.Urgent, result[1].Priority);
        Assert.Equal(NeedStatus.Resolved, result[1].Status);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Mapped_ViewModel_When_Found()
    {
        var need = new Need
        {
            Id = 5,
            Title = "Found",
            Category = new Category { Name = "Cat" },
            Region = "R",
            ContactPoint = "CP",
            Priority = NeedPriority.Planned,
            Status = NeedStatus.New,
            CreatedAt = DateTime.UtcNow
        };
        _needRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(need);

        var result = await _sut.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal("Found", result.Title);
        Assert.Equal("Cat", result.CategoryName);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_NotFound()
    {
        _needRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Need?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
    }
}
