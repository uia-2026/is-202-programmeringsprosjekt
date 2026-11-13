using Heimevernet.Extensions;
using System.Text.Json;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Heimevernet.DTOs.Needs;


namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Need-related web pages (list, create, details).
/// Uses INeedService for business operations and ICategoryRepository to populate category lists.
/// </summary>
/// <param name="needService">Business logic for needs.</param>
/// <param name="categoryRepository">Loads categories for the forms and filters.</param>
/// <param name="resourceService">Finds nearby resources for the Details page.</param>
/// <param name="regionResolver">Finds the region from the chosen map location.</param>
/// <param name="matchService">Checks if the user may propose a match.</param>
/// <param name="logger">Logs errors when the list cannot be loaded.</param>
public class NeedController(
    INeedService needService,
    ICategoryRepository categoryRepository,
    IResourceService resourceService,
    IRegionResolver regionResolver,
    IMatchService matchService,
    ILogger<NeedController> logger) : Controller
{
    private readonly INeedService _needService = needService;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IResourceService _resourceService = resourceService;
    private readonly IRegionResolver _regionResolver = regionResolver;
    private readonly IMatchService _matchService = matchService;
    private readonly ILogger<NeedController> _logger = logger;

    private const double NearbyResourcesRadiusKm = 50;
    private const int NearbyResourcesMaxResults = 5;
    private const int UnprocessableEntityStatusCode = 422;

    /// <summary>
    /// Displays the list of needs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>An <see cref="IActionResult"/> that renders the Index view.</returns>
    /// <remarks><c>GET /Need/Index</c>. Requires a signed-in user (fallback policy). If loading fails, the view shows an error message instead of throwing.</remarks>
    /// <param name="filter">Filter and sort options from the query string.</param>
    [HttpGet]
    public async Task<IActionResult> Index(NeedFilterDto filter, CancellationToken cancellationToken)
    {
        try
        {
            var needs = await _needService.GetFilteredAsync(filter, cancellationToken);
            var categories = await _categoryRepository.GetAllAsync();
            var regions = await _needService.GetDistinctRegionsAsync(cancellationToken);

            var model = new NeedIndexViewModel
            {
                Filter = filter,
                Items = needs,
                AvailableCategories = categories,
                AvailableRegions = regions
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the Need list for filter {@Filter}", filter);
            ViewBag.LoadError = "Something went wrong while loading needs. Please try again.";
            return View(new NeedIndexViewModel { Filter = filter });
        }
    }



    /// <summary>
    /// Shows the create need form populated with available categories.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> that renders the Create view.</returns>
    /// <remarks><c>GET /Need/Create</c>. Requires the PublicActor role.</remarks>
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
    /// <remarks>
    /// <c>POST /Need/Create</c>. Requires the PublicActor role and an anti-forgery token.
    /// The region is found from the map location before validation.
    /// </remarks>
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

    /// <summary>
    /// Shows the edit form for a need, using the same view as Create.
    /// </summary>
    /// <remarks><c>GET /Need/Edit/{id}</c>. Requires the PublicActor role, and the user must own the need.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The Create view in edit mode, 404 Not Found, or 403 Forbidden.</returns>
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

    /// <summary>
    /// Saves changes to a need owned by the user.
    /// </summary>
    /// <remarks><c>POST /Need/Edit/{id}</c>. Requires the PublicActor role and an anti-forgery token.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="model">The submitted form.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to <see cref="Details"/> on success, the form again with errors for validation errors or conflicts,
    /// 404 Not Found, or 403 Forbidden.
    /// </returns>
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

    /// <summary>
    /// Deletes a need owned by the user. Needs with matches cannot be deleted.
    /// </summary>
    /// <remarks><c>POST /Need/Delete/{id}</c>. Requires the PublicActor role and an anti-forgery token.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to the list on success, a redirect to <see cref="Details"/> with an error on conflict,
    /// 404 Not Found, or 403 Forbidden.
    /// </returns>
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

    /// <summary>
    /// JSON API version of <see cref="Edit(int, NeedCreateViewModel, CancellationToken)"/>.
    /// </summary>
    /// <remarks><c>PUT /api/needs/{id}</c>. Requires the PublicActor role.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="model">The new values as JSON in the request body.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// 200 OK, 404 Not Found, 403 Forbidden or 409 Conflict with a <c>{ message }</c> body,
    /// or 422 Unprocessable Entity with validation problems.
    /// </returns>
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

    /// <summary>
    /// JSON API version of <see cref="Delete"/>.
    /// </summary>
    /// <remarks><c>DELETE /api/needs/{id}</c>. Requires the PublicActor role.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>200 OK, 404 Not Found, 403 Forbidden or 409 Conflict with a <c>{ message }</c> body.</returns>
    [HttpDelete("/api/needs/{id:int}")]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> DeleteApi(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _needService.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return ToApiResult(result, message);
    }

    /// <summary>
    /// Marks an Assigned Need as Resolved from the Need Details page (#97).
    /// Uses the existing NeedService.ResolveAsync, which validates ownership and status.
    /// </summary>
    /// <remarks><c>POST /Need/Resolve/{id}</c>. Requires the PublicActor role and an anti-forgery token.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A redirect to <see cref="Details"/> with a message, 404 Not Found, or 403 Forbidden.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Resolve(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _needService.ResolveAsync(id, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case OwnedRecordMutationResult.Success:
                TempData["Success"] = message;
                return RedirectToAction(nameof(Details), new { id });

            case OwnedRecordMutationResult.NotFound:
                return NotFound();

            case OwnedRecordMutationResult.Forbidden:
                return Forbid();

            case OwnedRecordMutationResult.Conflict:
                TempData["Error"] = message;
                return RedirectToAction(nameof(Details), new { id });

            default:
                return Problem(detail: "Unexpected resolve result.");
        }
    }

    /// <summary>
    /// JSON API version of <see cref="Resolve"/>.
    /// </summary>
    /// <remarks><c>POST /api/needs/{id}/resolve</c>. Requires the PublicActor role and an anti-forgery token.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>200 OK, 404 Not Found, 403 Forbidden or 409 Conflict with a <c>{ message }</c> body.</returns>
    [HttpPost("/api/needs/{id:int}/resolve")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> ResolveApi(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _needService.ResolveAsync(id, User.GetUserId(), cancellationToken);
        return ToApiResult(result, message);
    }

    /// <summary>
    /// Shows a single need's details, plus a distance-sorted list of the nearest
    /// available resources (within <see cref="NearbyResourcesRadiusKm"/> km), capped at
    /// <see cref="NearbyResourcesMaxResults"/> results. This is a plain "what's physically
    /// closest" list — no category/priority scoring, that belongs to a future matching feature.
    /// </summary>
    /// <remarks><c>GET /Need/Details/{id}</c>. Requires the PublicActor or ResourceProvider role.</remarks>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The Details view with a <see cref="NeedDetailsViewModel"/>, or 404 Not Found.</returns>
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