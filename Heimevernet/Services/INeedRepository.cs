using Heimevernet.Models;

namespace Heimevernet.Services
{
    /// <summary>
    /// Simple repository abstraction for storing and retrieving <see cref="Need"/> entities.
    /// Implementations may be in-memory or backed by a database.
    /// </summary>
    public interface INeedRepository
    {
        /// <summary>Returns all needs ordered for display.</summary>
        IEnumerable<Need> GetAll();

        /// <summary>Attempts to find a need by id.</summary>
        /// <param name="id">Identifier of the need.</param>
        /// <returns>The need when found; otherwise null.</returns>
        Need? GetById(int id);

        /// <summary>Adds a new need to the store. Caller is responsible for validation.</summary>
        /// <param name="need">Need instance to add.</param>
        void Add(Need need);
    }
}
