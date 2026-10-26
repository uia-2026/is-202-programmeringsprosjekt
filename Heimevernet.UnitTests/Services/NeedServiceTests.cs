using Heimevernet.Mappers;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Moq;

namespace Heimevernet.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="Heimevernet.Services.NeedService"/>.
/// These tests verify orchestration between the repository, mapper and unit of work.
/// </summary>
public class NeedServiceTests
{
    private readonly Mock<INeedRepository> _needRepoMock;
    private readonly Mock<INeedMapper> _mapperMock;
    private readonly Mock<IMapMarkerMapper> _mapMarkerMapperMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly NeedService _sut;

    public NeedServiceTests()
    {
        _needRepoMock = new Mock<INeedRepository>();
        _mapperMock = new Mock<INeedMapper>();
        _mapMarkerMapperMock = new Mock<IMapMarkerMapper>();
        _uowMock = new Mock<IUnitOfWork>();
        _sut = new NeedService(_needRepoMock.Object, _mapperMock.Object, _mapMarkerMapperMock.Object, _uowMock.Object);
    }

    /// <summary>
    /// Ensures CreateAsync maps the create model, saves the entity and commits the unit of work.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Saves_Entity_And_Commits()
    {
        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "Water",
            Region = "Agder",
            ContactPoint = "contact",
            Priority = NeedPriority.Medium
        };
        var mappedNeed = new Need { Id = 1, Title = "Water" };
        _mapperMock.Setup(m => m.ToEntity(model, 42)).Returns(mappedNeed);

        await _sut.CreateAsync(model, 42);

        _needRepoMock.Verify(r => r.AddAsync(mappedNeed, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that when the repository throws, the unit of work is not committed.
    /// </summary>
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
        var mappedNeed = new Need { Id = 1, Title = "Title" };
        _mapperMock.Setup(m => m.ToEntity(model, 1)).Returns(mappedNeed);
        _needRepoMock.Setup(r => r.AddAsync(It.IsAny<Need>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(model, 1));

        _uowMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    /// <summary>
    /// Ensures CreateAsync calls the mapper and forwards the mapped entity to the repository.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Calls_Mapper_And_Forwards_Result_To_Repository()
    {
        var model = new NeedCreateViewModel
        {
            CategoryId = 5,
            Title = "Food",
            Region = "Region",
            ContactPoint = "cp"
        };
        var mappedNeed = new Need { Id = 99, Title = "Mapped" };
        _mapperMock.Setup(m => m.ToEntity(model, 99)).Returns(mappedNeed);

        await _sut.CreateAsync(model, 99);

        _mapperMock.Verify(m => m.ToEntity(model, 99), Times.Once);
        _needRepoMock.Verify(r => r.AddAsync(mappedNeed, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies GetAllAsync returns mapped view models for all needs.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns_Mapped_ViewModels()
    {
        var needs = new List<Need>
        {
            new Need { Id = 1, Title = "A", Category = new Category { Name = "C1" }, Region = "R1", ContactPoint = "CP1" },
            new Need { Id = 2, Title = "B", Category = new Category { Name = "C2" }, Region = "R2", ContactPoint = "CP2" }
        };
        var expected = new List<NeedViewModel>
        {
            new NeedViewModel { Id = 1, Title = "A", CategoryName = "C1" },
            new NeedViewModel { Id = 2, Title = "B", CategoryName = "C2" }
        };
        _needRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(needs);
        _mapperMock.Setup(m => m.ToViewModels(needs)).Returns(expected);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Title);
        Assert.Equal("C1", result[0].CategoryName);
        _mapperMock.Verify(m => m.ToViewModels(needs), Times.Once);
    }

    /// <summary>
    /// Verifies GetAllAsync returns an empty collection when no needs exist.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Needs()
    {
        var emptyNeeds = Array.Empty<Need>();
        var emptyViewModels = Array.Empty<NeedViewModel>();
        _needRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emptyNeeds);
        _mapperMock.Setup(m => m.ToViewModels(It.IsAny<IEnumerable<Need>>())).Returns(emptyViewModels);

        var result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies GetSummariesAsync returns mapped summary view models.
    /// </summary>
    [Fact]
    public async Task GetSummariesAsync_Returns_Mapped_Summaries()
    {
        var needs = new List<Need>
        {
            new Need { Id = 1, Title = "A", Category = new Category { Name = "C1" }, Region = "R1", Priority = NeedPriority.Low, Status = NeedStatus.New },
            new Need { Id = 2, Title = "B", Category = new Category { Name = "C2" }, Region = "R2", Priority = NeedPriority.Medium, Status = NeedStatus.Resolved }
        };
        var expected = new List<NeedSummaryViewModel>
        {
            new NeedSummaryViewModel { Id = 1, Title = "A", CategoryName = "C1", Priority = NeedPriority.Low, Status = NeedStatus.New },
            new NeedSummaryViewModel { Id = 2, Title = "B", CategoryName = "C2", Priority = NeedPriority.Medium, Status = NeedStatus.Resolved }
        };
        _needRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(needs);
        _mapperMock.Setup(m => m.ToSummaryViewModels(needs)).Returns(expected);

        var result = (await _sut.GetSummariesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Title);
        Assert.Equal("C1", result[0].CategoryName);
        Assert.Equal(NeedPriority.Medium, result[1].Priority);
        _mapperMock.Verify(m => m.ToSummaryViewModels(needs), Times.Once);
    }

    /// <summary>
    /// Verifies GetByIdAsync returns the mapped view model when the entity exists.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Returns_Mapped_ViewModel_When_Found()
    {
        var need = new Need
        {
            Id = 5,
            Title = "Found",
            Category = new Category { Name = "Cat" },
            Region = "R",
            ContactPoint = "CP"
        };
        var expected = new NeedViewModel { Id = 5, Title = "Found", CategoryName = "Cat" };
        _needRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(need);
        _mapperMock.Setup(m => m.ToViewModel(need)).Returns(expected);

        var result = await _sut.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
        Assert.Equal("Found", result.Title);
        Assert.Equal("Cat", result.CategoryName);
        _mapperMock.Verify(m => m.ToViewModel(need), Times.Once);
    }

    /// <summary>
    /// Verifies GetByIdAsync returns null when the entity is not found.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_NotFound()
    {
        _needRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Need?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
        _mapperMock.Verify(m => m.ToViewModel(It.IsAny<Need>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_Forwards_CancellationToken_To_Repository()
    {
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;
        var needs = new List<Need>();
        var viewModels = new List<NeedViewModel>();
        _needRepoMock.Setup(r => r.GetAllAsync(It.Is<CancellationToken>(t => t == expectedToken))).ReturnsAsync(needs);
        _mapperMock.Setup(m => m.ToViewModels(needs)).Returns(viewModels);

        var result = await _sut.GetAllAsync(expectedToken);

        Assert.Empty(result);
        _needRepoMock.Verify(r => r.GetAllAsync(It.Is<CancellationToken>(t => t == expectedToken)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Owner_Can_Edit_When_Allowed()
    {
        var need = new Need
        {
            Id = 10,
            UserId = 42,
            CategoryId = 1,
            Status = NeedStatus.New,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Deadline = DateTime.UtcNow.AddDays(1),
            Matches = []
        };

        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "Updated",
            Region = "Agder",
            ContactPoint = "cp",
            Priority = NeedPriority.Low,
            Deadline = need.Deadline
        };

        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);

        await _sut.UpdateAsync(10, model, 42);

        _mapperMock.Verify(m => m.ApplyEditableFields(need, model), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Forbidden_For_NonOwner()
    {
        var need = new Need { Id = 10, UserId = 1, Status = NeedStatus.New, Matches = [] };
        var model = new NeedCreateViewModel { CategoryId = 1, Title = "T", Region = "R", ContactPoint = "C" };
        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);

        var (result, _) = await _sut.UpdateAsync(10, model, 2);

        Assert.Equal(OwnedRecordMutationResult.Forbidden, result);
        _mapperMock.Verify(m => m.ApplyEditableFields(It.IsAny<Need>(), It.IsAny<NeedCreateViewModel>()), Times.Never);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Conflict_For_Resolved_Need()
    {
        var need = new Need { Id = 10, UserId = 42, Status = NeedStatus.Resolved, Matches = [] };
        var model = new NeedCreateViewModel { CategoryId = 1, Title = "T", Region = "R", ContactPoint = "C" };
        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);

        var (result, _) = await _sut.UpdateAsync(10, model, 42);

        Assert.Equal(OwnedRecordMutationResult.Conflict, result);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Conflict_When_Protected_Field_Changes_With_Pending_Match()
    {
        var need = new Need
        {
            Id = 10,
            UserId = 42,
            Status = NeedStatus.New,
            CategoryId = 1,
            Region = "Agder",
            Deadline = DateTime.UtcNow.AddDays(2),
            Matches = [new Heimevernet.Models.Match { Status = MatchStatus.UnderReview }]
        };
        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "T",
            Region = "Agder",
            ContactPoint = "C",
            Deadline = need.Deadline?.AddDays(2)
        };
        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);

        var (result, _) = await _sut.UpdateAsync(10, model, 42);

        Assert.Equal(OwnedRecordMutationResult.Conflict, result);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Owner_Can_Delete_When_Allowed()
    {
        var need = new Need { Id = 10, UserId = 42, Matches = [] };
        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);

        var (result, _) = await _sut.DeleteAsync(10, 42);

        Assert.Equal(OwnedRecordMutationResult.Success, result);
        _needRepoMock.Verify(r => r.Remove(need), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_Conflict_When_Pending_Or_Assigned_Match_Exists()
    {
        var need = new Need
        {
            Id = 10,
            UserId = 42,
            Matches = [new Heimevernet.Models.Match { Status = MatchStatus.Assigned }]
        };

        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);

        var (result, _) = await _sut.DeleteAsync(10, 42);

        Assert.Equal(OwnedRecordMutationResult.Conflict, result);
        _needRepoMock.Verify(r => r.Remove(It.IsAny<Need>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Does_Not_Modify_Owner_Status_Or_CreatedAt()
    {
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var need = new Need
        {
            Id = 10,
            UserId = 42,
            Status = NeedStatus.UnderReview,
            CreatedAt = createdAt,
            CategoryId = 1,
            Matches = []
        };
        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "Updated",
            Region = "Agder",
            ContactPoint = "cp",
            Priority = NeedPriority.Low
        };

        _needRepoMock.Setup(r => r.GetByIdForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(need);
        _mapperMock.Setup(m => m.ApplyEditableFields(need, model)).Callback(() => need.Title = "Updated");

        await _sut.UpdateAsync(10, model, 42);

        Assert.Equal(42, need.UserId);
        Assert.Equal(NeedStatus.UnderReview, need.Status);
        Assert.Equal(createdAt, need.CreatedAt);
    }
}
