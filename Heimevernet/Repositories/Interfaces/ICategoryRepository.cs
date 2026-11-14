using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access contract for Category entities.
/// Used by controllers and services to retrieve category lists and lookups.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Returns all categories, sorted by name.
    /// </summary>
    /// <returns>The categories.</returns>
    Task<IEnumerable<Category>> GetAllAsync();
    /// <summary>
    /// Finds a category by ID.
    /// </summary>
    /// <param name="id">ID of the category.</param>
    /// <returns>The category, or <see langword="null"/> if it does not exist.</returns>
    Task<Category?> GetByIdAsync(int id);
}