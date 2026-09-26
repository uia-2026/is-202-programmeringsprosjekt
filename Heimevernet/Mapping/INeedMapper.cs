using Heimevernet.Models;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers;

public interface INeedMapper
{
    Need ToEntity(NeedCreateViewModel model, int userId);
    NeedViewModel ToViewModel(Need need);
    NeedSummaryViewModel ToSummaryViewModel(Need need);
    IEnumerable<NeedSummaryViewModel> ToSummaryViewModels(IEnumerable<Need> needs);
    IEnumerable<NeedViewModel> ToViewModels(IEnumerable<Need> needs);
}
