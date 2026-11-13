namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Result of editing, deleting or resolving a need or resource the user owns.
/// Controllers map these to a redirect with a message or an HTTP status code.
/// </summary>
public enum OwnedRecordMutationResult
{
    /// <summary>The change was saved.</summary>
    Success,
    /// <summary>The record does not exist.</summary>
    NotFound,
    /// <summary>The user does not own the record.</summary>
    Forbidden,
    /// <summary>The change is not allowed in the current state, or someone else changed the record at the same time.</summary>
    Conflict,
    /// <summary>The submitted data is not valid, e.g. an unknown category.</summary>
    ValidationError
}