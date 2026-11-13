using Heimevernet.Extensions;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Attachment;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Heimevernet.DTOs.Resources;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Resource-related web pages (list, create, details).
/// Uses IResourceService for business operations and ICategoryRepository to populate category lists.
/// </summary>
/// <param name="resourceService">Business logic for resources.</param>
/// <param name="categoryRepository">Loads categories for the forms and filters.</param>
/// <param name="attachmentService">Uploads, lists and deletes photos.</param>
/// <param name="regionResolver">Finds the region from the chosen map location.</param>
/// <param name="logger">Logs errors when the list cannot be loaded.</param>
public class ResourceController(
    IResourceService resourceService,
    ICategoryRepository categoryRepository,
    IAttachmentService attachmentService,
    IRegionResolver regionResolver,
    ILogger<ResourceController> logger) : Controller
{
    private readonly IResourceService _resourceService = resourceService;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly IRegionResolver _regionResolver = regionResolver;
    private readonly ILogger<ResourceController> _logger = logger;
    private const int UnprocessableEntityStatusCode = 422;
    /// <summary>
    /// Lists resources (#37).
    /// Resource Providers always see only their own resources; any ownerId in the request is ignored.
    /// Public Actors see every resource by default and can filter by owner with <paramref name="ownerId"/>.
    /// All filtering happens in the database.
    /// </summary>
    /// <remarks><c>GET /Resource/Index</c>. Requires a signed-in user (fallback policy).</remarks>
    /// <param name="filter">Filter and sort options from the query string.</param>
    /// <param name="ownerId">Owner to filter by (Public Actors only).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The Index view with a <see cref="ResourceIndexViewModel"/>. On errors, the view shows an error message.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
    ResourceFilterDto filter,
    int? ownerId,
    CancellationToken cancellationToken)
    {
        try
        {
            if (User.IsInRole("ResourceProvider"))
            {
                filter.OwnerUserId = User.GetUserId();
            }
            else
            {
                var selectedOwnerId = filter.ShowOnlyMine ? User.GetUserId() : ownerId;
                filter.OwnerUserId = selectedOwnerId;
                filter.ShowOnlyMine = selectedOwnerId == User.GetUserId() && filter.ShowOnlyMine;

                ViewBag.Owners = await _resourceService.GetOwnersAsync(cancellationToken);
                ViewBag.SelectedOwnerId = selectedOwnerId;
                ViewBag.CurrentUserId = User.GetUserId();
            }

            var resources = await _resourceService.GetFilteredAsync(filter, cancellationToken);
            var categories = await _categoryRepository.GetAllAsync();
            var regions = await _resourceService.GetDistinctRegionsAsync(cancellationToken);

            var model = new ResourceIndexViewModel
            {
                Filter = filter,
                Items = resources,
                AvailableCategories = categories,
                AvailableRegions = regions
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the Resource list for filter {@Filter}", filter);
            ViewBag.LoadError = "Something went wrong while loading resources. Please try again.";
            return View(new ResourceIndexViewModel { Filter = filter });
        }
    }



    /// <summary>
    /// Shows the form for registering a new resource.
    /// </summary>
    /// <remarks><c>GET /Resource/Create</c>. Requires the PublicActor or ResourceProvider role.</remarks>
    /// <returns>The Create view with categories filled in.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    public async Task<IActionResult> Create()
    {
        var model = new ResourceCreateViewModel
        {
            AvailableFrom = DateTime.Now,
            AvailableCategories =
                await _categoryRepository.GetAllAsync()
        };

        ViewData["IsEdit"] = false;
        return View("Create", model);
    }

    /// <summary>
    /// Validates and saves a new resource, then tries to attach the optional photo.
    /// The resource is registered even if the photo upload fails.
    /// </summary>
    /// <returns>Redirects to the new resource's Details page, or redisplays the form on validation errors.</returns>
    /// <remarks>
    /// <c>POST /Resource/Create</c>. Requires the PublicActor or ResourceProvider role and an anti-forgery token.
    /// Request size is limited to 6 MB.
    /// </remarks>
    /// <param name="model">The submitted form, with an optional image.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    [HttpPost]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)] // 5 MB photo plus form overhead
    public async Task<IActionResult> Create(
        ResourceCreateViewModel model,
        CancellationToken cancellationToken)
    {
        await TryResolveRegionAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _categoryRepository.GetAllAsync();
            ViewData["IsEdit"] = false;
            return View("Create", model);
        }

        var userId = User.GetUserId();
        var id = await _resourceService.CreateAsync(model, userId, cancellationToken);

        TempData["Success"] = $"The resource \"{model.Title}\" has been registered.";

        if (model.Image is { Length: > 0 })
        {
            try
            {
                await using var stream = model.Image.OpenReadStream();
                var result = await _attachmentService.UploadAsync(
                    id, stream, model.Image.FileName, model.Image.Length, userId, cancellationToken);

                if (!result.Success)
                    TempData["Warning"] = $"Resource registered, but the photo was not saved: {result.Error}";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                TempData["Warning"] = "Resource registered, but the photo could not be saved. You can add it again below.";
            }
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Shows the edit form for a resource, using the same view as Create.
    /// </summary>
    /// <remarks><c>GET /Resource/Edit/{id}</c>. Requires the PublicActor or ResourceProvider role, and the user must own the resource.</remarks>
    /// <param name="id">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The Create view in edit mode, 404 Not Found, or 403 Forbidden.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);

        if (resource is null)
            return NotFound();

        if (resource.UserId != User.GetUserId())
            return Forbid();

        var model = new ResourceCreateViewModel
        {
            CategoryId = resource.CategoryId,
            Title = resource.Title,
            Description = resource.Description,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude,
            Region = resource.Region,
            AvailableFrom = resource.AvailableFrom,
            AvailableTo = resource.AvailableTo,
            ContactPoint = resource.ContactPoint,
            QuantityOffered = resource.QuantityOffered,
            AvailableCategories = await _categoryRepository.GetAllAsync()
        };

        ViewData["IsEdit"] = true;
        ViewData["EditResourceId"] = id;
        return View("Create", model);
    }

    /// <summary>
    /// Saves changes to a resource owned by the user.
    /// </summary>
    /// <remarks><c>POST /Resource/Edit/{id}</c>. Requires the PublicActor or ResourceProvider role and an anti-forgery token.</remarks>
    /// <param name="id">ID of the resource.</param>
    /// <param name="model">The submitted form.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to <see cref="Details"/> on success, the form again with errors for validation errors or conflicts,
    /// 404 Not Found, or 403 Forbidden.
    /// </returns>
    [HttpPost]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, ResourceCreateViewModel model, CancellationToken cancellationToken)
    {
        await TryResolveRegionAsync(model, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _categoryRepository.GetAllAsync();
            ViewData["EditResourceId"] = id;
            ViewData["IsEdit"] = true;
            return View("Create", model);
        }

        var (result, message) = await _resourceService.UpdateAsync(id, model, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case OwnedRecordMutationResult.Success:
                TempData["Success"] = "Resource updated.";
                return RedirectToAction(nameof(Details), new { id });

            case OwnedRecordMutationResult.NotFound:
                return NotFound();

            case OwnedRecordMutationResult.Forbidden:
                return Forbid();

            case OwnedRecordMutationResult.Conflict:
                ModelState.AddModelError(string.Empty, message);
                model.AvailableCategories = await _categoryRepository.GetAllAsync();
                ViewData["EditResourceId"] = id;
                ViewData["IsEdit"] = true;
                return View("Create", model);

            case OwnedRecordMutationResult.ValidationError:
                ModelState.AddModelError(nameof(model.CategoryId), message);
                model.AvailableCategories = await _categoryRepository.GetAllAsync();
                ViewData["EditResourceId"] = id;
                ViewData["IsEdit"] = true;
                return View("Create", model);

            default:
                return Problem(detail: "Unexpected update result.");
        }
    }

    /// <summary>
    /// Deletes a resource owned by the user. Resources with matches cannot be deleted.
    /// </summary>
    /// <remarks><c>POST /Resource/Delete/{id}</c>. Requires the PublicActor or ResourceProvider role and an anti-forgery token.</remarks>
    /// <param name="id">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to the list on success, a redirect to <see cref="Details"/> with an error on conflict,
    /// 404 Not Found, or 403 Forbidden.
    /// </returns>
    [HttpPost]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _resourceService.DeleteAsync(id, User.GetUserId(), cancellationToken);

        switch (result)
        {
            case OwnedRecordMutationResult.Success:
                TempData["Success"] = "Resource deleted.";
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
    /// JSON API version of <see cref="Edit(int, ResourceCreateViewModel, CancellationToken)"/>.
    /// </summary>
    /// <remarks><c>PUT /api/resources/{id}</c>. Requires the PublicActor or ResourceProvider role.</remarks>
    /// <param name="id">ID of the resource.</param>
    /// <param name="model">The new values as JSON in the request body.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>
    /// 200 OK, 404 Not Found, 403 Forbidden or 409 Conflict with a <c>{ message }</c> body,
    /// or 422 Unprocessable Entity with validation problems.
    /// </returns>
    [HttpPut("/api/resources/{id:int}")]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    public async Task<IActionResult> UpdateApi(int id, [FromBody] ResourceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(modelStateDictionary: ModelState, statusCode: UnprocessableEntityStatusCode);
        }

        var (result, message) = await _resourceService.UpdateAsync(id, model, User.GetUserId(), cancellationToken);
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
    /// <remarks><c>DELETE /api/resources/{id}</c>. Requires the PublicActor or ResourceProvider role.</remarks>
    /// <param name="id">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>200 OK, 404 Not Found, 403 Forbidden or 409 Conflict with a <c>{ message }</c> body.</returns>
    [HttpDelete("/api/resources/{id:int}")]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    public async Task<IActionResult> DeleteApi(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _resourceService.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return ToApiResult(result, message);
    }

    /// <summary>
    /// Shows one resource with its photos.
    /// </summary>
    /// <remarks><c>GET /Resource/Details/{id}</c>. Requires a signed-in user (fallback policy).</remarks>
    /// <param name="id">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The Details view with a <see cref="ResourceViewModel"/>, or 404 Not Found.</returns>
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);
        if (resource == null)
            return NotFound();

        resource.Attachments = await _attachmentService.GetForResourceAsync(id, cancellationToken);
        resource.IsOwner = resource.UserId == User.GetUserId(); // display only, the service enforces ownership

        return View(resource);
    }

    /// <summary>
    /// Print-friendly list of available Resources, filterable by region (#41).
    /// Only Resources currently marked Available are listed - a Public Actor printing
    /// this wants what can actually be requested right now, not resources already in use.
    /// </summary>
    /// <remarks><c>GET /Resource/PrintByRegion?region={region}</c>. Requires the PublicActor role.</remarks>
    /// <param name="region">Region to show, or empty for all regions.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The PrintByRegion view with a <see cref="PrintByRegionViewModel"/>.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> PrintByRegion(string? region, CancellationToken cancellationToken)
    {
        var availableResources = await _resourceService.GetAvailableAsync(cancellationToken);

        var availableRegions = availableResources
            .Select(r => r.Region)
            .Distinct()
            .OrderBy(r => r)
            .ToList();

        var model = new PrintByRegionViewModel
        {
            SelectedRegion = region,
            AvailableRegions = availableRegions,
            Resources = FilterByRegion(availableResources, region)
        };

        return View(model);
    }

    /// <summary>Downloads the same list as <see cref="PrintByRegion"/> as a CSV file (#94).</summary>
    /// <remarks><c>GET /Resource/ExportCsv?region={region}</c>. Requires the PublicActor role.</remarks>
    /// <param name="region">Region to export, or empty for all regions.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A <c>text/csv</c> file download.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> ExportCsv(string? region, CancellationToken cancellationToken)
    {
        var availableResources = await _resourceService.GetAvailableAsync(cancellationToken);
        var csv = ResourceExporter.ToCsv(FilterByRegion(availableResources, region));

        return File(csv, "text/csv; charset=utf-8", ExportFileName(region, "csv"));
    }

    /// <summary>Downloads the same list as <see cref="PrintByRegion"/> as a GeoJSON file (#94).</summary>
    /// <remarks><c>GET /Resource/ExportGeoJson?region={region}</c>. Requires the PublicActor role.</remarks>
    /// <param name="region">Region to export, or empty for all regions.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>An <c>application/geo+json</c> file download.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> ExportGeoJson(string? region, CancellationToken cancellationToken)
    {
        var availableResources = await _resourceService.GetAvailableAsync(cancellationToken);
        var geoJson = ResourceExporter.ToGeoJson(FilterByRegion(availableResources, region));

        return File(geoJson, "application/geo+json", ExportFileName(region, "geojson"));
    }

    /// <summary>Downloads the same list as <see cref="PrintByRegion"/> as a GeoParquet file.</summary>
    /// <remarks><c>GET /Resource/ExportGeoParquet?region={region}</c>. Requires the PublicActor role.</remarks>
    /// <param name="region">Region to export, or empty for all regions.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>An <c>application/vnd.apache.parquet</c> file download.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> ExportGeoParquet(string? region, CancellationToken cancellationToken)
    {
        var availableResources = await _resourceService.GetAvailableAsync(cancellationToken);
        var geoParquet = ResourceExporter.ToGeoParquet(FilterByRegion(availableResources, region));

        return File(geoParquet, "application/vnd.apache.parquet", ExportFileName(region, "geoparquet"));
    }

    private static List<ResourceViewModel> FilterByRegion(
        IEnumerable<ResourceViewModel> resources,
        string? region)
    {
        var filtered = string.IsNullOrWhiteSpace(region)
            ? resources
            : resources.Where(r => r.Region == region);

        return [.. filtered.OrderBy(r => r.Title)];
    }

    private static string ExportFileName(string? region, string extension)
    {
        if (string.IsNullOrWhiteSpace(region))
            return $"available-resources.{extension}";

        // The region comes from the query string, so keep only safe characters for the file name.
        var safeRegion = new string([.. region.Select(c => char.IsLetterOrDigit(c) ? c : '-')]);
        return $"available-resources-{safeRegion}.{extension}";
    }

    /// <summary>Uploads a photo to a resource owned by the current user.</summary>
    /// <remarks>
    /// <c>POST /Resource/UploadAttachment</c>. Requires the PublicActor or ResourceProvider role and an anti-forgery token.
    /// Request size is limited to 6 MB. Ownership is checked in <see cref="IAttachmentService.UploadAsync"/>.
    /// </remarks>
    /// <param name="model">The resource ID and the image file.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A redirect to <see cref="Details"/> with a success or error message.</returns>
    [HttpPost]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachment(
        AttachmentUploadViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Choose an image to upload.";
            return RedirectToAction(nameof(Details), new { id = model.ResourceId });
        }

        await using var stream = model.File.OpenReadStream();
        var result = await _attachmentService.UploadAsync(
            model.ResourceId, stream, model.File.FileName, model.File.Length, User.GetUserId(), cancellationToken);

        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Photo uploaded." : result.Error;
        return RedirectToAction(nameof(Details), new { id = model.ResourceId });
    }

    /// <summary>Deletes a photo from a resource owned by the current user.</summary>
    /// <remarks>
    /// <c>POST /Resource/DeleteAttachment</c>. Requires the PublicActor or ResourceProvider role and an anti-forgery token.
    /// Ownership is checked in <see cref="IAttachmentService.DeleteAsync"/>.
    /// </remarks>
    /// <param name="attachmentId">ID of the photo to delete.</param>
    /// <param name="resourceId">ID of the resource, used for the redirect.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A redirect to <see cref="Details"/> with a success or error message.</returns>
    [HttpPost]
    [Authorize(Roles = "PublicActor,ResourceProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(
        int attachmentId,
        int resourceId,
        CancellationToken cancellationToken)
    {
        var result = await _attachmentService.DeleteAsync(attachmentId, User.GetUserId(), cancellationToken);

        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Photo removed." : result.Error;
        return RedirectToAction(nameof(Details), new { id = resourceId });
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

    private async Task TryResolveRegionAsync(ResourceCreateViewModel model, CancellationToken cancellationToken)
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
}