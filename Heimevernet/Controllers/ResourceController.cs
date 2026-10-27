using Heimevernet.Extensions;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Attachment;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Resource-related web pages (list, create, details).
/// Uses IResourceService for business operations and ICategoryRepository to populate category lists.
/// </summary>
public class ResourceController(
    IResourceService resourceService,
    ICategoryRepository categoryRepository,
    IAttachmentService attachmentService) : Controller
{
    private readonly IResourceService _resourceService = resourceService;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IAttachmentService _attachmentService = attachmentService;
    private const int UnprocessableEntityStatusCode = 422;

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var resources =
            await _resourceService.GetAllAsync(cancellationToken);

        return View(resources);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ResourceCreateViewModel
        {
            AvailableFrom = DateTime.Now,
            AvailableCategories =
                await _categoryRepository.GetAllAsync()
        };

        ViewData["IsEdit"] = true;
        return View("Create", model);
    }

    /// <summary>
    /// Validates and saves a new resource, then tries to attach the optional photo.
    /// The resource is registered even if the photo upload fails.
    /// </summary>
    /// <returns>Redirects to the new resource's Details page, or redisplays the form on validation errors.</returns>
    [HttpPost]
    [Authorize(Roles = "ResourceProvider")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)] // 5 MB photo plus form overhead
    public async Task<IActionResult> Create(
        ResourceCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _categoryRepository.GetAllAsync();
            ViewData["IsEdit"] = true;
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

    [HttpGet]
    [Authorize(Roles = "ResourceProvider")]
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

        ViewData["EditResourceId"] = id;
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "ResourceProvider")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, ResourceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await _categoryRepository.GetAllAsync();
            ViewData["EditResourceId"] = id;
            return View(model);
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

            default:
                return Problem(detail: "Unexpected update result.");
        }
    }

    [HttpPost]
    [Authorize(Roles = "ResourceProvider")]
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

    [HttpPut("/api/resources/{id:int}")]
    [Authorize(Roles = "ResourceProvider")]
    public async Task<IActionResult> UpdateApi(int id, [FromBody] ResourceCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(modelStateDictionary: ModelState, statusCode: UnprocessableEntityStatusCode);
        }

        var (result, message) = await _resourceService.UpdateAsync(id, model, User.GetUserId(), cancellationToken);
        return ToApiResult(result, message);
    }

    [HttpDelete("/api/resources/{id:int}")]
    [Authorize(Roles = "ResourceProvider")]
    public async Task<IActionResult> DeleteApi(int id, CancellationToken cancellationToken)
    {
        var (result, message) = await _resourceService.DeleteAsync(id, User.GetUserId(), cancellationToken);
        return ToApiResult(result, message);
    }

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

        var filtered = string.IsNullOrWhiteSpace(region)
            ? availableResources
            : [.. availableResources.Where(r => r.Region == region)];

        var model = new PrintByRegionViewModel
        {
            SelectedRegion = region,
            AvailableRegions = availableRegions,
            Resources = [.. filtered.OrderBy(r => r.Title)]
        };

        return View(model);
    }

    /// <summary>Uploads a photo to a resource owned by the current user.</summary>
    [HttpPost]
    [Authorize(Roles = "ResourceProvider")]
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
    [HttpPost]
    [Authorize(Roles = "ResourceProvider")]
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
}