namespace Heimevernet.Exceptions;

/// <summary>
/// Thrown when a save fails because it would break a unique constraint in the database,
/// for example when the same match is proposed twice.
/// </summary>
/// <param name="inner">The original database exception.</param>
public sealed class DuplicateRecordException(Exception inner)
    : Exception("The record already exists.", inner);