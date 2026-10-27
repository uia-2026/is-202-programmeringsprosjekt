using Heimevernet.Extensions;
using System.Text.Json;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Need-related web pages (list, create, details).
/// Uses INeedService for business operations and ICategoryRepository to populate category lists.
/// </summary>
public class NeedController(
    INeedService needService,
    ICategoryRepository categoryRepository,
    IResourceService resourceService,
    IRegionResolver regionResolver,
    IMatchService matchService) : Controller
{
    private readonly INeedService _needService = needService;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IResourceService _resourceService = resourceService;
    private readonly IRegionResolver _regionResolver = regionResolver;
    private readonly IMatchService _matchService = matchService;

    private const double NearbyResourcesRadiusKm = 50;
    private const int NearbyResourcesMaxResults = 5;
    private const int UnprocessableEntityStatusCode = 422;

    /// <summary>
    /// Displays the list of needs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>An <see cref="IActionResult"/> that renders the Index view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var needs = await _needService.GetAllAsync(cancellationToken);

        return View(needs);
    }

    /// <summary>
    /// Shows the create need form populated with available categories.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> that renders the Create view.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Create()
    {
        var model = new NeedCreateViewModel
        {
            AvailableCategories =
                await _categoryRepository.GetAllAsync()
        };

        ViewData["IsEdit"] = false;
        return View("Create", model);
    }

    /// <summary>
    /// Handles posted create need form. Validates and persists the new need.
    /// </summary>
    /// <param name="model">The create view model submitted by the user.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Redirects to Index on success or returns the view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Create(
        NeedCreateViewModel model,
        CancellationToken cancellationToken)
    {
        await TryResolveRegionAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.AvailableCategories =
                await _categoryRepository.GetAllAsync();

            ViewData["IsEdit"] = false;
            return View("Create", model);
        }

        var createdId = await _needService.CreateAsync(model, User.GetUserId(), cancellationToken);

        TempData["Success"] =
            $"The need \"{model.Title}\" has been registered.";

        return RedirectToAction(nameof(Details), new { id = createdId });
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var need = await _needService.GetByIdAsync(id, cancellationToken);

        if (need is null)
        {
            return NotFound();
        }

        if (need.UserId != User.GetUserId())
        {
            return Forbid();
        }

        var model = new NeedCreateViewModel
        {
            CategoryId = need.CategoryId,
            Title = need.Title,
            Description = need.Description,
            Latitude = need.Latitude,
            Longitude = need.Longitude,
            Region = need.Region,
            Priority = need.Priority,
            Deadline = need.Deadline,
            ContactPoint = need.ContactPoint,
            QuantityNeeded = need.QuantityNeeded,
            AvailableCategories = await _categoryRepository.GetAllAsync()
        };

        ViewData["IsEdit"] = true;
        ViewData["EditNeedId"] = id;
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Edit(int id, NeedCreateViewModel model, CancellationToken cancellationToken)
    {
        var existingNeed = await _needService.GetByIdAsync(id, cancellationToken);
        AllowUnchangedDeadline(existingNeed?.Deadline, model.Deadline);
        await TryResolveRegionAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _categoryRepository.GetAllAsync();
            ViewData["EditNeedId"] = id;
            ViewData["IsEdit"] = true;
            return View("Create", model);
        }

        var (result, message) = await _needService.UpdateAsync(id, model, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case OwnedRecordMutationResult.Success:
                TempData["Success"] = "Need updated.";
                return RedirectToAction(nameof(Details), new { id });

            case OwnedRecordMutationResult.NotFound:
                return NotFound();

            case OwnedRecordMutationResult.Forbidden:
                return Forbid();

            case OwnedRecordMutationResult.Conflict:
                ModelState.AddModelError(string.Empty, message);
                model.AvailableCategories = await _categoryRepository.GetAllAsync();
                ViewData["EditNeedId"] = id;
                ViewData["IsEdit"] = true;
                return View("Create", model);

            case OwnedRecordMutationResult.ValidationError:
                ModelState.AddModelError(nameof(model.CategoryId), message);
                model.AvailableCategories = await _categoryRepository.GetAllAsync();
                ViewData["EditNeedId"] = id;
                ViewData["IsEdit"] = true;
                return View("Create", model);

            default:
                return Problem(detail: "Unexpected update result.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _needService.DeleteAsync(id, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case OwnedRecordMutationResult.Success:
                TempData["Success"] = "Need deleted.";
                return RedirectToAction(nameof(Index));

            case OwnedRecordMutationResult.NotFound:
                return NotFound();

            case OwnedRecordMutationResult.Forbidden:
                return Forbid();

            case OwnedRecordMutationResult.Conflict:
                TempData["Error"] = message;
                return RedirectToAction(nameof(Details), new { id });

            default:
                return Problem(detail: "Unexpected delete result.");
        }
    }

    [HttpPut("/api/needs/{id:int}")]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> UpdateApi(int id, [FromBody] NeedCreateViewModel? model, CancellationToken cancellationToken)
    {
        if (model is null)
        {
            ModelState.AddModelError(string.Empty, "A request body is required.");
            return ValidationProblem(modelStateDictionary: ModelState, statusCode: UnprocessableEntityStatusCode);
        }

        var existingNeed = await _needService.GetByIdAsync(id, cancellationToken);
        AllowUnchangedDeadline(existingNeed?.Deadline, model.Deadline);
        await TryResolveRegionAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(modelStateDictionary: ModelState, statusCode: UnprocessableEntityStatusCode);
        }

        var (result, message) = await _needService.UpdateAsync(id, model, User.GetUserId(), cancellationToken);
        if (result == OwnedRecordMutationResult.ValidationError)
        {
            ModelState.AddModelError(nameof(model.CategoryId), message);
            return ValidationProblem(modelStateDictionary: ModelState, statusCode: UnprocessableEntityStatusCode);
        }

        return ToApiResult(result, message);
    }

    [HttpDelete("/api/needs/{id:int}")]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> DeleteApi(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _needService.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return ToApiResult(result, message);
    }

    /// <summary>
    /// Shows a single need's details, plus a distance-sorted list of the nearest
    /// available resources (within <see cref="NearbyResourcesRadiusKm"/> km), capped at
    /// <see cref="NearbyResourcesMaxResults"/> results. This is a plain "what's physically
    /// closest" list — no category/priority scoring, that belongs to a future matching feature.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var needViewModel = await _needService.GetByIdAsync(id, cancellationToken);

        if (needViewModel is null)
        {
            return NotFound();
        }

        needViewModel.IsOwner = needViewModel.UserId == User.GetUserId();

        var nearbyResources = await _resourceService.GetCompatibleNearbyAsync(
            needViewModel,
            NearbyResourcesRadiusKm,
            NearbyResourcesMaxResults,
            cancellationToken);

        var model = new NeedDetailsViewModel
        {
            Need = needViewModel,
            NearbyResources = nearbyResources,
            NearbyResourcesRadiusKm = NearbyResourcesRadiusKm,
            CanProposeMatch = await _matchService.CanProposeAsync(
                id, User.GetUserId(), cancellationToken)
        };

        return View(model);
    }

    private ObjectResult ToApiResult(OwnedRecordMutationResult result, string message)
    {
        return result switch
        {
            OwnedRecordMutationResult.Success => Ok(new { message }),
            OwnedRecordMutationResult.NotFound => NotFound(new { message }),
            OwnedRecordMutationResult.Forbidden => StatusCode(403, new { message }),
            OwnedRecordMutationResult.Conflict => StatusCode(409, new { message }),
            _ => Problem(detail: "Unexpected mutation result.")
        };
    }

    private async Task TryResolveRegionAsync(NeedCreateViewModel model, CancellationToken cancellationToken)
    {
        if (model.Latitude == 0 && model.Longitude == 0)
        {
            return;
        }

        var res = await _regionResolver.TryResolveRegionAsync(model.Latitude, model.Longitude, cancellationToken);

        if (res.Success)
        {
            model.Region = res.IsInNorway
                ? (!string.IsNullOrEmpty(res.Region) ? res.Region! : "Unknown county")
                : "International";

            ModelState.Remove(nameof(model.Region));
            return;
        }

        ModelState.AddModelError(nameof(model.Region), "Please select a location on land (not in the ocean).");
    }

    private void AllowUnchangedDeadline(DateTime? currentDeadline, DateTime? submittedDeadline)
    {
        if (currentDeadline != submittedDeadline
            || !ModelState.TryGetValue(nameof(NeedCreateViewModel.Deadline), out var deadlineState))
        {
            return;
        }

        for (var i = deadlineState.Errors.Count - 1; i >= 0; i--)
        {
            if (deadlineState.Errors[i].ErrorMessage == "Deadline must be in the future.")
                deadlineState.Errors.RemoveAt(i);
        }
    }
}