using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Service contract for operations related to Needs.
/// Provides methods for creating, querying and retrieving detailed needs.
/// </summary>
public interface INeedService
{
    /// <summary>
    /// Creates a new Need from the provided create model for the specified user.
    /// </summary>
    /// <param name="model">The create view model containing need details.</param>
    /// <param name="userId">The id of the user creating the need.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task CreateAsync(NeedCreateViewModel model, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all needs mapped to view models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A sequence of <see cref="NeedViewModel"/>.</returns>
    Task<IEnumerable<NeedViewModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns summary view models for all needs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A sequence of <see cref="NeedSummaryViewModel"/>.</returns>
    Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single need by id and returns it mapped to a view model, or null when not found.
    /// </summary>
    /// <param name="id">The id of the need to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The mapped <see cref="NeedViewModel"/> or null.</returns>
    Task<NeedViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
