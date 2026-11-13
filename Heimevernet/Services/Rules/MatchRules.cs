using Heimevernet.Models;

namespace Heimevernet.Domain.Rules;

/// <summary>
/// Shared rules for deciding if a resource can be matched to a need.
/// Used both when proposing and when accepting a match.
/// </summary>
public static class MatchRules
{
    /// <summary>
    /// Checks whether a resource satisfies the basic matching criteria.
    /// </summary>
    /// <param name="need">The need to fill.</param>
    /// <param name="resource">The resource that is offered.</param>
    /// <param name="now">The current UTC time, passed in so the rule is easy to test.</param>
    /// <returns>
    /// <see langword="true"/> when the category is the same, the resource has not expired,
    /// and it becomes available before the need's deadline.
    /// </returns>
    public static bool IsSuitable(Need need, Resource resource, DateTime now) =>
        resource.CategoryId == need.CategoryId
        && (resource.AvailableTo is null || resource.AvailableTo >= now)
        && (need.Deadline is null || resource.AvailableFrom <= need.Deadline);
}