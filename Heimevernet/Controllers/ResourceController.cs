using Heimevernet.Extensions;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Resource-related web pages (list, create, details).
/// Uses IResourceService for business operations and ICategoryRepository to populate category lists.
/// </summary>
public class ResourceController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAttachmentService _attachmentService;

    public ResourceController(
        IResourceService resourceService,
        ICategoryRepository categoryRepository,
        IAttachmentService attachmentService)
    {
        _resourceService = resourceService;
        _categoryRepository = categoryRepository;
        _attachmentService = attachmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var resources =
            await _resourceService.GetAllAsync(cancellationToken);

        return View(resources);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model = new ResourceCreateViewModel
        {
            AvailableFrom = DateTime.Now,
            AvailableCategories =
                await _categoryRepository.GetAllAsync()
        };

        return View(model);
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
            return View(model);
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
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);
        if (resource == null)
            return NotFound();

        resource.Attachments = await _attachmentService.GetForResourceAsync(id, cancellationToken);
        resource.IsOwner = resource.UserId == User.GetUserId(); // display only, the service enforces ownership

        return View(resource);
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
}