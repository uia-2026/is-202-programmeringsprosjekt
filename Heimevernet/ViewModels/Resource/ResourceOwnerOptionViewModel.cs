namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// One option in the owner filter on the Registered Resources page (#37).
/// </summary>
public class ResourceOwnerOptionViewModel
{
    /// <summary>ID of the owner, used as the option value.</summary>
    public int UserId { get; set; }

    /// <summary>Username of the owner, used as the option text.</summary>
    public string Username { get; set; } = string.Empty;
}