using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.ViewModels.Home;

/// <summary>
/// View model for the home/index page. Aggregates summary lists of needs and resources
/// shown on the landing page.
/// </summary>
public sealed class HomeIndexViewModel
{
    public IEnumerable<NeedSummaryViewModel> Needs { get; set; } = Enumerable.Empty<NeedSummaryViewModel>();
    public IEnumerable<ResourceViewModel> Resources { get; set; } = Enumerable.Empty<ResourceViewModel>();
}
