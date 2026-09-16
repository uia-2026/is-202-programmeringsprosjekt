using Heimevernet.ViewModels.Resource;


namespace Heimevernet.ViewModels.Home;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];

    public bool IsEmpty => Resources.Count == 0;

    public int Count => Resources.Count;
}
