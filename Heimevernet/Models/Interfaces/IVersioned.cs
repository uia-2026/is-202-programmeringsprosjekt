namespace Heimevernet.Models.Interfaces;

/// <summary>
/// Marks an entity that uses optimistic concurrency. <see cref="Heimevernet.Data.AppDbContext"/> increases
/// <see cref="Version"/> on every save, and the save fails if the row was changed by someone else first.
/// </summary>
public interface IVersioned
{
    /// <summary>
    /// Row version used as the concurrency token. Increased by one each time the entity is saved.
    /// </summary>
    int Version { get; set; }
}