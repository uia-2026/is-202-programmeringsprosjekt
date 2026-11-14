namespace Heimevernet.ViewModels;

/// <summary>
/// View model used by the error page to display request information.
/// </summary>
public class ErrorViewModel
{
    /// <summary>ID of the failed request, so it can be found in the logs.</summary>
    public string? RequestId { get; set; }

    /// <summary><see langword="true"/> when there is a <see cref="RequestId"/> to show.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}