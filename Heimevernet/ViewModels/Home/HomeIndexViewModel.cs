using Heimevernet.ViewModels.Resource;


namespace Heimevernet.ViewModels.Home;

/// <summary>
/// View model used by the home/index view to show a list of resources.
/// </summary>
public sealed class HomeIndexViewModel
{
    /// <summary>Collection of resources to display.</summary>
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];

    /// <summary>True when there are no resources.</summary>
    public bool IsEmpty => Resources.Count == 0;

    /// <summary>Number of resources in the collection.</summary>
    public int Count => Resources.Count;
}
