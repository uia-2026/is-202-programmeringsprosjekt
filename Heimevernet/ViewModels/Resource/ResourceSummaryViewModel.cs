using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

public class ResourceSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public NeedPriority Priority { get; set; }
    public NeedStatus Status { get; set; }
}