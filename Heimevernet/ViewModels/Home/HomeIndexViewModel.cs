using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.ViewModels.Home;

public sealed class HomeIndexViewModel
{
    public IEnumerable<NeedSummaryViewModel> Needs { get; set; } = Enumerable.Empty<NeedSummaryViewModel>();
    public IEnumerable<ResourceViewModel> Resources { get; set; } = Enumerable.Empty<ResourceViewModel>();
}
