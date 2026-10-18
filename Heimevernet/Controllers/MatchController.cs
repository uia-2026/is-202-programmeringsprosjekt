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
public class MatchController : Controller
{
    private readonly IMatchService _matchService;
    private readonly INeedService _needService;

    public MatchController(IMatchService matchService, INeedService needService)
    {
        _matchService = matchService;
        _needService = needService;
    }

    /// <summary>
    /// Shows the candidate-resource picker for a given Need.
    /// Only the Need's owner may access this page.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
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
            Candidates = candidates
        };

        return View(model);
    }

    /// <summary>
    /// Handles the submitted choice of Resource and creates the Match.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Propose(int needId, int resourceId, CancellationToken cancellationToken)
    {
        var (result, _) = await _matchService.ProposeAsync(needId, resourceId, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case ProposeMatchResult.Success:
                TempData["Success"] = "Resource proposed for this need. Waiting for the provider to accept.";
                return RedirectToAction("Details", "Need", new { id = needId });

            case ProposeMatchResult.NeedNotFound:
                return NotFound();

            case ProposeMatchResult.NotNeedOwner:
                return Forbid();

            case ProposeMatchResult.ResourceNotFound:
                TempData["Error"] = "That resource no longer exists.";
                return RedirectToAction(nameof(Propose), new { needId });

            case ProposeMatchResult.ResourceNotAvailable:
                TempData["Error"] = "That resource is no longer available.";
                return RedirectToAction(nameof(Propose), new { needId });

            case ProposeMatchResult.DuplicateMatch:
                TempData["Error"] = "This resource has already been proposed for this need.";
                return RedirectToAction(nameof(Propose), new { needId });

            default:
                return BadRequest();
        }
    }
}