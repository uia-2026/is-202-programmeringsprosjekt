using Heimevernet.Models;
using Heimevernet.ViewModels;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers;

/// <summary>
/// Maps between Need domain entities and Need view models.
/// Provides conversions for detailed and summary view models and creation models.
/// </summary>
public class NeedMapper : INeedMapper
{
    /// <summary>
    /// Maps a creation view model to a domain entity.
    /// </summary>
    /// <remarks>
    /// Method documentation placeholder; parameter and return tags to be completed.
    /// </remarks>
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

    /// <summary>
    /// Maps a <see cref="NeedCreateViewModel"/> to a <see cref="Need"/> entity.
    /// </summary>
    /// <param name="model">Create view model to map from.</param>
    /// <param name="userId">Id of the user creating the need.</param>
    /// <returns>A populated <see cref="Need"/> entity.</returns>

    /// <summary>
    /// Maps a domain entity to a detailed view model.
    /// </summary>
    /// <remarks>
    /// Method documentation placeholder; parameter and return tags to be completed.
    /// </remarks>
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

    /// <summary>
    /// Maps a <see cref="Need"/> entity to a detailed <see cref="NeedViewModel"/>.
    /// </summary>
    /// <param name="need">Entity to map.</param>
    /// <returns>A <see cref="NeedViewModel"/>.</returns>

    /// <summary>
    /// Maps a domain entity to a summary view model.
    /// </summary>
    /// <remarks>
    /// Method documentation placeholder; parameter and return tags to be completed.
    /// </remarks>
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

    /// <summary>
    /// Maps a <see cref="Need"/> entity to a <see cref="NeedSummaryViewModel"/>.
    /// </summary>
    /// <param name="need">Entity to map.</param>
    /// <returns>A <see cref="NeedSummaryViewModel"/>.</returns>

    /// <summary>
    /// Maps a sequence of domain entities to summary view models.
    /// </summary>
    /// <remarks>
    /// Method documentation placeholder; parameter and return tags to be completed.
    /// </remarks>
    public IEnumerable<NeedSummaryViewModel> ToSummaryViewModels(
        IEnumerable<Need> needs)
    {
        return needs.Select(ToSummaryViewModel);
    }

    /// <summary>
    /// Maps a sequence of <see cref="Need"/> entities to summary view models.
    /// </summary>
    /// <param name="needs">Sequence of needs to map.</param>
    /// <returns>Sequence of <see cref="NeedSummaryViewModel"/>.</returns>

    /// <summary>
    /// Maps a sequence of domain entities to detailed view models.
    /// </summary>
    /// <remarks>
    /// Method documentation placeholder; parameter and return tags to be completed.
    /// </remarks>
    public IEnumerable<NeedViewModel> ToViewModels(
        IEnumerable<Need> needs)
    {
        return needs.Select(ToViewModel);
    }

    /// <summary>
    /// Maps a sequence of <see cref="Need"/> entities to detailed view models.
    /// </summary>
    /// <param name="needs">Sequence of needs to map.</param>
    /// <returns>Sequence of <see cref="NeedViewModel"/>.</returns>
}