using Heimevernet.Models;
using Heimevernet.ViewModels;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers;

public class NeedMapper : INeedMapper
{
    public Need ToEntity(NeedCreateViewModel model, int userId)
    {
        return new Need
        {
            UserId = userId,
            CategoryId = model.CategoryId!.Value,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Region = model.Region.Trim(),
            Priority = model.Priority,
            Deadline = model.Deadline,
            ContactPoint = model.ContactPoint.Trim()
        };
    }

    public NeedViewModel ToViewModel(Need need)
    {
        return new NeedViewModel
        {
            Id = need.Id,
            Title = need.Title,
            Description = need.Description,
            CategoryName = need.Category.Name,
            Latitude = need.Latitude,
            Longitude = need.Longitude,
            Region = need.Region,
            Priority = need.Priority,
            Deadline = need.Deadline,
            ContactPoint = need.ContactPoint,
            Status = need.Status,
            CreatedAt = need.CreatedAt
        };
    }

    public NeedSummaryViewModel ToSummaryViewModel(Need need)
    {
        return new NeedSummaryViewModel
        {
            Id = need.Id,
            Title = need.Title,
            CategoryName = need.Category.Name,
            Region = need.Region,
            Priority = need.Priority,
            Status = need.Status
        };
    }

    public IEnumerable<NeedSummaryViewModel> ToSummaryViewModels(
        IEnumerable<Need> needs)
    {
        return needs.Select(ToSummaryViewModel);
    }

    public IEnumerable<NeedViewModel> ToViewModels(
        IEnumerable<Need> needs)
    {
        return needs.Select(ToViewModel);
    }
}