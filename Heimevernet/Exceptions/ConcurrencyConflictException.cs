namespace Heimevernet.Exceptions;

public sealed class ConcurrencyConflictException(Exception inner)
    : Exception("The data was changed by someone else.", inner);