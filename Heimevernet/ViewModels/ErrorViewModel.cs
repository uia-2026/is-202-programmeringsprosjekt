namespace Heimevernet.ViewModels;

/// <summary>
/// View model used by the error page to display request information.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
