namespace Heimevernet.Exceptions;

public sealed class DuplicateRecordException(Exception inner)
    : Exception("The record already exists.", inner);