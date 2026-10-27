using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for the Match: Propose flow (#40).
/// Lets a Need's owner pick a candidate Resource and propose it as a match.
/// </summary>
public class MatchController(IMatchService matchService, INeedService needService) : Controller
{
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
        var (result, _) = await _matchService.ProposeAsync(needId, resourceId, quantity, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case ProposeMatchResult.Success:
                TempData["Success"] =
                    $"Resource proposed for this need with quantity {quantity}. Waiting for the provider to accept.";

                return RedirectToAction(
                    "Details", "Need", new { id = needId });

            case ProposeMatchResult.InvalidQuantity:
                TempData["Error"] = "Quantity must be between 1 and 10000.";

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
}