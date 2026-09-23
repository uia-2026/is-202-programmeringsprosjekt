using Heimevernet.ViewModels.Need;

namespace Heimevernet.ViewModels.Home;

public sealed class HomeIndexViewModel
{
    public IEnumerable<NeedSummaryViewModel> Needs { get; set; } = Enumerable.Empty<NeedSummaryViewModel>();
}
