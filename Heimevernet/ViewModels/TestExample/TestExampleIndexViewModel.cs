using Heimevernet.ViewModels.Resource;

namespace Heimevernet.ViewModels.TestExample;

public sealed class TestExampleIndexViewModel
{
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];

    public bool IsEmpty => Resources.Count == 0;

    public int Count => Resources.Count;
}
