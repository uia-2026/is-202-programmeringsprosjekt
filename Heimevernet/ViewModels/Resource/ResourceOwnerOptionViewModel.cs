namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// One option in the owner filter on the Registered Resources page (#37).
/// </summary>
public class ResourceOwnerOptionViewModel
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;
}
