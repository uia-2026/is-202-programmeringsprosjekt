using Heimevernet.Mappers;
using Heimevernet.Models;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.UnitTests.Mapping;

public class NeedMapperTests
{
    private readonly NeedMapper _sut = new();

    [Fact]
    public void ToEntity_Maps_CreateViewModel_And_Trims_Strings()
    {
        var deadline = new DateTime(2026, 12, 31, 12, 0, 0, DateTimeKind.Utc);
        var model = new NeedCreateViewModel
        {
            CategoryId = 5,
            Title = "  Water Supply  ",
            Description = "  Need clean water  ",
            Latitude = 58.1467,
            Longitude = 7.9956,
            Region = "  Kristiansand  ",
            Priority = NeedPriority.Urgent,
            Deadline = deadline,
            ContactPoint = "  contact@example.com  "
        };

        var result = _sut.ToEntity(model, 42);

        Assert.Equal(42, result.UserId);
        Assert.Equal(5, result.CategoryId);
        Assert.Equal("Water Supply", result.Title);
        Assert.Equal("Need clean water", result.Description);
        Assert.Equal(58.1467, result.Latitude);
        Assert.Equal(7.9956, result.Longitude);
        Assert.Equal("Kristiansand", result.Region);
        Assert.Equal(NeedPriority.Urgent, result.Priority);
        Assert.Equal(deadline, result.Deadline);
        Assert.Equal("contact@example.com", result.ContactPoint);
    }

    [Fact]
    public void ToEntity_Preserves_Nullable_Fields()
    {
        var model = new NeedCreateViewModel
        {
            CategoryId = 1,
            Title = "Title",
            Description = null,
            Latitude = 0,
            Longitude = 0,
            Region = "Region",
            Priority = NeedPriority.Planned,
            Deadline = null,
            ContactPoint = "contact"
        };

        var result = _sut.ToEntity(model, 1);

        Assert.Null(result.Description);
        Assert.Null(result.Deadline);
    }

    [Fact]
    public void ToViewModel_Maps_Entity_To_ViewModel()
    {
        var createdAt = new DateTime(2026, 01, 01, 10, 0, 0, DateTimeKind.Utc);
        var deadline = new DateTime(2026, 06, 01, 12, 0, 0, DateTimeKind.Utc);
        var need = new Need
        {
            Id = 123,
            Category = new Category { Id = 2, Name = "Medical" },
            Title = "Need Title",
            Description = "Detailed description",
            Latitude = 10.5,
            Longitude = 20.5,
            Region = "Oslo",
            Priority = NeedPriority.Urgent,
            Deadline = deadline,
            ContactPoint = "john@example.com",
            Status = NeedStatus.Assigned,
            CreatedAt = createdAt
        };

        var result = _sut.ToViewModel(need);

        Assert.Equal(123, result.Id);
        Assert.Equal("Need Title", result.Title);
        Assert.Equal("Detailed description", result.Description);
        Assert.Equal("Medical", result.CategoryName);
        Assert.Equal(10.5, result.Latitude);
        Assert.Equal(20.5, result.Longitude);
        Assert.Equal("Oslo", result.Region);
        Assert.Equal(NeedPriority.Urgent, result.Priority);
        Assert.Equal(deadline, result.Deadline);
        Assert.Equal("john@example.com", result.ContactPoint);
        Assert.Equal(NeedStatus.Assigned, result.Status);
        Assert.Equal(createdAt, result.CreatedAt);
    }

    [Fact]
    public void ToSummaryViewModel_Maps_Entity_To_Summary()
    {
        var need = new Need
        {
            Id = 7,
            Title = "Summary Test",
            Category = new Category { Name = "Transport" },
            Region = "Bergen",
            Priority = NeedPriority.Planned,
            Status = NeedStatus.New
        };

        var result = _sut.ToSummaryViewModel(need);

        Assert.Equal(7, result.Id);
        Assert.Equal("Summary Test", result.Title);
        Assert.Equal("Transport", result.CategoryName);
        Assert.Equal("Bergen", result.Region);
        Assert.Equal(NeedPriority.Planned, result.Priority);
        Assert.Equal(NeedStatus.New, result.Status);
    }

    [Fact]
    public void ToViewModels_Maps_Collection()
    {
        var needs = new[]
        {
            new Need { Id = 10, Title = "X", Category = new Category { Name = "CatX" }, Region = "RX", ContactPoint = "CPX" },
            new Need { Id = 20, Title = "Y", Category = new Category { Name = "CatY" }, Region = "RY", ContactPoint = "CPY" }
        };

        var results = _sut.ToViewModels(needs).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(10, results[0].Id);
        Assert.Equal("CatX", results[0].CategoryName);
        Assert.Equal(20, results[1].Id);
        Assert.Equal("CatY", results[1].CategoryName);
    }

    [Fact]
    public void ToSummaryViewModels_Maps_Collection()
    {
        var needs = new[]
        {
            new Need { Id = 1, Title = "A", Category = new Category { Name = "C1" }, Region = "R1", Priority = NeedPriority.Planned, Status = NeedStatus.New },
            new Need { Id = 2, Title = "B", Category = new Category { Name = "C2" }, Region = "R2", Priority = NeedPriority.Urgent, Status = NeedStatus.Assigned }
        };

        var results = _sut.ToSummaryViewModels(needs).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("C1", results[0].CategoryName);
        Assert.Equal("C2", results[1].CategoryName);
        Assert.Equal(NeedPriority.Urgent, results[1].Priority);
    }

    [Fact]
    public void Empty_Collections_Return_Empty()
    {
        Assert.Empty(_sut.ToViewModels(Array.Empty<Need>()));
        Assert.Empty(_sut.ToSummaryViewModels(Array.Empty<Need>()));
    }
}
