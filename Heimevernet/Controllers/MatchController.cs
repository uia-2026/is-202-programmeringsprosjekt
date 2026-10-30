using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for the Match flow.
/// Propose (#40): lets a Need's owner pick a candidate Resource and propose it as a match.
/// My Matches (#64): role-specific list of matches, where the Resource Provider can accept or reject.
/// </summary>
public class MatchController(IMatchService matchService, INeedService needService) : Controller
{
    private const string ResourceProviderRole = "ResourceProvider"; // NYTT I STEG 8

    private readonly IMatchService _matchService = matchService;
    private readonly INeedService _needService = needService;

    /// <summary>
    /// Shows candidate resources for a Need owned by the current user.
    /// </summary>
    [Authorize(Roles = "PublicActor")]
    [HttpGet]
    public async Task<IActionResult> Propose(int needId, CancellationToken cancellationToken)
    {
        var need = await _needService.GetByIdAsync(needId, cancellationToken);

        if (need is null)
        {
            return NotFound();
        }

        var canPropose = await _matchService.CanProposeAsync(needId, User.GetUserId(), cancellationToken);

        if (!canPropose)
        {
            return Forbid();
        }

        var candidates = await _matchService.GetCandidateResourcesAsync(needId, cancellationToken);

        var model = new ProposeMatchViewModel
        {
            NeedId = needId,
            NeedTitle = need.Title,
            Quantity = 1,
            Candidates = candidates
        };

        return View(model);
    }

    /// <summary>
    /// Validates the submitted choice and proposes a Resource for the Need.
    /// </summary>
    [Authorize(Roles = "PublicActor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Propose(int needId, int resourceId, int quantity, CancellationToken cancellationToken)
    {
        var (result, _, maxAllowedQuantity) = await _matchService.ProposeAsync(needId, resourceId, quantity, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case ProposeMatchResult.Success:
                TempData["Success"] =
                    $"Resource proposed for this need with quantity {quantity}. Waiting for the provider to accept.";

                return RedirectToAction(
                    "Details", "Need", new { id = needId });

            case ProposeMatchResult.InvalidQuantity:
                TempData["Error"] = "Requested quantity exceeds what is still available for this need or resource.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            case ProposeMatchResult.NeedNotFound:
                return NotFound();

            case ProposeMatchResult.UserIsNotNeedOwner:
                return Forbid();

            case ProposeMatchResult.NeedNotOpen:
                TempData["Error"] =
                    "This need is no longer open for proposals.";

                return RedirectToAction(
                    "Details", "Need", new { id = needId });

            case ProposeMatchResult.ResourceNotFound:
                TempData["Error"] = "That resource no longer exists.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            case ProposeMatchResult.ResourceNotAvailable:
                TempData["Error"] = "That resource is no longer available.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            case ProposeMatchResult.MatchAlreadyExists:
                TempData["Error"] =
                    "This resource has already been proposed for this need.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            case ProposeMatchResult.ResourceNotSuitable:
                TempData["Error"] =
                    "This resource is not suitable for the need based on category, location, or availability.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            case ProposeMatchResult.ExceedsRemaining:
                TempData["Error"] =
                    $"The requested quantity exceeds the remaining available quantity. Maximum allowed: {maxAllowedQuantity ?? 0}.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            case ProposeMatchResult.ConcurrentChange:
                TempData["Error"] =
                    "The need or resource was modified by another request. Please try again.";

                return RedirectToAction(
                    nameof(Propose), new { needId });

            default:
                return Problem(
                    detail: "An unexpected result occurred while proposing the match.");
        }
    }

    /// <summary>
    /// "My Matches": Public Actors see matches on their needs,
    /// Resource Providers see matches on their resources.
    /// </summary>
    [Authorize(Roles = "PublicActor," + ResourceProviderRole)]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var isResourceProvider = User.IsInRole(ResourceProviderRole);

        var model = await _matchService.GetMyMatchesAsync(User.GetUserId(), isResourceProvider, cancellationToken);

        return View(model);
    }

    /// <summary>
    /// Resource Provider accepts a proposed match.
    /// </summary>
    [Authorize(Roles = ResourceProviderRole)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id, CancellationToken cancellationToken)
    {
        var result = await _matchService.AcceptAsync(id, User.GetUserId(), cancellationToken);

        return HandleDecision(result, "Match accepted.");
    }

    /// <summary>
    /// Resource Provider rejects a proposed match.
    /// </summary>
    [Authorize(Roles = ResourceProviderRole)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        var result = await _matchService.RejectAsync(id, User.GetUserId(), cancellationToken);

        return HandleDecision(result, "Match rejected.");
    }

    private IActionResult HandleDecision(MatchDecisionResult result, string successMessage)
    {
        switch (result)
        {
            case MatchDecisionResult.Success:
                TempData["Success"] = successMessage;
                break;

            case MatchDecisionResult.MatchNotFound:
                return NotFound();

            case MatchDecisionResult.UserIsNotResourceOwner:
                return Forbid();

            case MatchDecisionResult.MatchNotUnderReview:
                TempData["Error"] = "This match has already been decided.";
                break;

            case MatchDecisionResult.InsufficientQuantity:
                TempData["Error"] = "There is not enough quantity left on the need or resource to accept this match.";
                break;

            case MatchDecisionResult.NeedNotOpen:
                TempData["Error"] = "The need is no longer open for matches.";
                break;

            case MatchDecisionResult.ResourceNotSuitable:
                TempData["Error"] = "The resource is no longer suitable for the need based on category, location, or availability.";
                break;

            case MatchDecisionResult.ResourceNotAvailable:
                TempData["Error"] = "The resource is no longer available.";
                break;

            case MatchDecisionResult.ConcurrentChange:
                TempData["Error"] = "The match was changed by another request. Please try again.";
                break;

            default:
                return Problem(detail: "An unexpected result occurred while deciding the match.");
        }

        return RedirectToAction(nameof(Index));
    }
}