using Heimevernet.Models;

namespace Heimevernet.Domain.Rules;

public static class MatchRules
{
    /// <summary>
    /// Checks whether a resource satisfies the basic matching criteria.
    /// </summary>
    public static bool IsSuitable(Need need, Resource resource, DateTime now) =>
        resource.CategoryId == need.CategoryId
        && (resource.AvailableTo is null || resource.AvailableTo >= now)
        && (need.Deadline is null || resource.AvailableFrom <= need.Deadline);
}