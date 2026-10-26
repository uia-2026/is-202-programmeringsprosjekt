namespace Heimevernet.Services.Interfaces;

public enum OwnedRecordMutationResult
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError
}
