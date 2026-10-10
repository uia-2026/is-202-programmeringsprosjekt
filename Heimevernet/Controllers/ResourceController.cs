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
    /// Handles posted create resource form. Validates and persists the new resource.
    /// </summary>
    /// <param name="model">The create view model submitted by the user.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Redirects to Index on success or returns the view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ResourceCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories =
                await _categoryRepository.GetAllAsync();

            return View(model);
        }

        await _resourceService.CreateAsync(
            model,
            User.GetUserId(),
            cancellationToken);

        TempData["Success"] =
            $"The resource \"{model.Title}\" has been registered.";

        return RedirectToAction(nameof(Index));
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