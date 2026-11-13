namespace Heimevernet.Exceptions;

/// <summary>
/// Thrown when a save fails because another user changed the same row first
/// (optimistic concurrency check on <see cref="Heimevernet.Models.Interfaces.IVersioned"/> entities).
/// </summary>
/// <param name="inner">The original EF Core concurrency exception.</param>
public sealed class ConcurrencyConflictException(Exception inner)
    : Exception("The data was changed by someone else.", inner);