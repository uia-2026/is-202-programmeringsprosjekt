using Heimevernet.ViewModels.Resource;

namespace Heimevernet.ViewModels.TestExample;

/// <summary>
/// View model used by the TestExample views to present sample resource data.
/// </summary>
public sealed class TestExampleIndexViewModel
{
    /// <summary>Collection of resource view models used for display and testing.</summary>
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];

    /// <summary>True when no resources are present.</summary>
    public bool IsEmpty => Resources.Count == 0;

    /// <summary>Number of resources in the view model.</summary>
    public int Count => Resources.Count;
}
