using Heimevernet.Models;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers;

/// <summary>
/// Mapping contract for Need-related conversions between domain entities and view models.
/// Implementations should provide conversions for create models, detailed and summary view models.
/// </summary>
public interface INeedMapper
{
    /// <summary>
    /// Maps a create view model to a domain entity.
    /// </summary>
    /// <param name="model">The create view model containing input data.</param>
    /// <param name="userId">The id of the user creating the entity.</param>
    /// <returns>A mapped <see cref="Need"/> entity.</returns>
    Need ToEntity(NeedCreateViewModel model, int userId);

    /// <summary>
    /// Maps a domain Need to a detailed view model.
    /// </summary>
    /// <param name="need">The domain entity to map.</param>
    /// <returns>A <see cref="NeedViewModel"/>.</returns>
    NeedViewModel ToViewModel(Need need);

    /// <summary>
    /// Maps a domain Need to a summary view model.
    /// </summary>
    /// <param name="need">The domain entity to map.</param>
    /// <returns>A <see cref="NeedSummaryViewModel"/>.</returns>
    NeedSummaryViewModel ToSummaryViewModel(Need need);

    /// <summary>
    /// Maps a sequence of Needs to summary view models.
    /// </summary>
    /// <param name="needs">Sequence of domain entities to map.</param>
    /// <returns>A sequence of <see cref="NeedSummaryViewModel"/>.</returns>
    IEnumerable<NeedSummaryViewModel> ToSummaryViewModels(IEnumerable<Need> needs);

    /// <summary>
    /// Maps a sequence of Needs to detailed view models.
    /// </summary>
    /// <param name="needs">Sequence of domain entities to map.</param>
    /// <returns>A sequence of <see cref="NeedViewModel"/>.</returns>
    IEnumerable<NeedViewModel> ToViewModels(IEnumerable<Need> needs);
}
