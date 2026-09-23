namespace Heimevernet.ViewModels;

/// <summary>
/// View model used by the error page to show request id information.
/// </summary>
public class ErrorViewModel
{
    /// <summary>Request identifier assigned to the current HTTP request.</summary>
    public string? RequestId { get; set; }

    /// <summary>True when a request id is available and should be displayed.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
