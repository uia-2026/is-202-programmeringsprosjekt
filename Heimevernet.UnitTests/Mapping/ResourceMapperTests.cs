using Heimevernet.Mappers;
using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.UnitTests.Mapping;

public class ResourceMapperTests
{
    private readonly ResourceMapper _sut = new();

    [Fact]
    public void ToEntity_Maps_CreateViewModel_And_Trims_Strings()
    {
        var availableFrom = new DateTime(2026, 05, 01, 08, 00, 00, DateTimeKind.Utc);
        var availableTo = new DateTime(2026, 05, 10, 18, 00, 00, DateTimeKind.Utc);
        var model = new ResourceCreateViewModel
        {
            CategoryId = 7,
            Title = "  Excavator  ",
            Description = "  Heavy machinery  ",
            Latitude = 60.3913,
            Longitude = 5.3221,
            Region = "  Bergen  ",
            AvailableFrom = availableFrom,
            AvailableTo = availableTo,
            ContactPoint = "  provider@example.com  "
        };

        var result = _sut.ToEntity(model, 99);

        Assert.Equal(99, result.UserId);
        Assert.Equal(7, result.CategoryId);
        Assert.Equal("Excavator", result.Title);
        Assert.Equal("Heavy machinery", result.Description);
        Assert.Equal(60.3913, result.Latitude);
        Assert.Equal(5.3221, result.Longitude);
        Assert.Equal("Bergen", result.Region);
        Assert.Equal(availableFrom, result.AvailableFrom);
        Assert.Equal(availableTo, result.AvailableTo);
        Assert.Equal("provider@example.com", result.ContactPoint);
    }

    [Fact]
    public void ToEntity_Preserves_Null_AvailableTo()
    {
        var model = new ResourceCreateViewModel
        {
            CategoryId = 1,
            Title = "Title",
            Region = "Region",
            AvailableFrom = new DateTime(2026, 01, 01),
            AvailableTo = null,
            ContactPoint = "contact"
        };

        var result = _sut.ToEntity(model, 1);

        Assert.Null(result.AvailableTo);
    }

    [Fact]
    public void ToViewModel_Maps_Entity_To_ViewModel()
    {
        var createdAt = new DateTime(2026, 01, 10, 09, 00, 00, DateTimeKind.Utc);
        var from = new DateTime(2026, 04, 01, 08, 00, 00, DateTimeKind.Utc);
        var to = new DateTime(2026, 04, 15, 18, 00, 00, DateTimeKind.Utc);
        var resource = new Resource
        {
            Id = 42,
            Category = new Category { Id = 2, Name = "Logistics" },
            Title = "Truck",
            Description = "Large truck available",
            Latitude = 59.9139,
            Longitude = 10.7522,
            Region = "Oslo",
            AvailableFrom = from,
            AvailableTo = to,
            ContactPoint = "driver@example.com",
            Status = ResourceStatus.Busy,
            CreatedAt = createdAt
        };

        var result = _sut.ToViewModel(resource);

        Assert.Equal(42, result.Id);
        Assert.Equal("Truck", result.Title);
        Assert.Equal("Large truck available", result.Description);
        Assert.Equal("Logistics", result.CategoryName);
        Assert.Equal(59.9139, result.Latitude);
        Assert.Equal(10.7522, result.Longitude);
        Assert.Equal("Oslo", result.Region);
        Assert.Equal(from, result.AvailableFrom);
        Assert.Equal(to, result.AvailableTo);
        Assert.Equal("driver@example.com", result.ContactPoint);
        Assert.Equal(ResourceStatus.Busy, result.Status);
        Assert.Equal(createdAt, result.CreatedAt);
    }

    [Fact]
    public void ToViewModel_Preserves_Null_AvailableTo()
    {
        var resource = new Resource
        {
            Id = 1,
            Category = new Category { Name = "C" },
            Title = "T",
            Region = "R",
            ContactPoint = "C",
            AvailableFrom = new DateTime(2026, 01, 01),
            AvailableTo = null
        };

        var result = _sut.ToViewModel(resource);

        Assert.Null(result.AvailableTo);
    }

    [Fact]
    public void ToViewModels_Maps_Collection()
    {
        var resources = new[]
        {
            new Resource { Id = 1, Title = "A", Category = new Category { Name = "C1" }, Region = "R1", ContactPoint = "CP1", AvailableFrom = DateTime.UtcNow },
            new Resource { Id = 2, Title = "B", Category = new Category { Name = "C2" }, Region = "R2", ContactPoint = "CP2", AvailableFrom = DateTime.UtcNow }
        };

        var results = _sut.ToViewModels(resources).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("C1", results[0].CategoryName);
        Assert.Equal(2, results[1].Id);
        Assert.Equal("C2", results[1].CategoryName);
    }

    [Fact]
    public void ToViewModels_Empty_Returns_Empty()
    {
        var results = _sut.ToViewModels(Array.Empty<Resource>());

        Assert.Empty(results);
    }
}
