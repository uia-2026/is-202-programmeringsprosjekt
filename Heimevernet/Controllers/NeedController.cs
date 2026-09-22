using Heimevernet.Models;
using Heimevernet.Repositories;
using Heimevernet.ViewModels.Need;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers
{
    public class NeedController : Controller
    {
        private readonly INeedRepository _repository;

        public NeedController(INeedRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var allNeeds = await _repository.GetAllAsync();
            return View(allNeeds);
        }

        public async Task<IActionResult> Details(int id)
        {
            var need = await _repository.GetByIdAsync(id);
            if (need == null)
            {
                return NotFound();
            }
            return View(need);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NeedViewModel model)
        {
            if (model.Type == NeedTypes.Other)
            {
                if (string.IsNullOrWhiteSpace(model.OtherType))
                {
                    ModelState.AddModelError(
                        nameof(model.OtherType),
                        "Please specify what type of need this is."
                    );
                }
                else
                {
                    model.Type = model.OtherType.Trim();
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var need = new Need
            {
                Title = model.Title,
                Type = model.Type,
                OtherType = model.OtherType,
                Street = model.Street,
                City = model.City,
                PostalCode = model.PostalCode,
                County = model.County,
                Country = model.Country,
                Deadline = model.Deadline,
                Priority = model.Priority,
                ContactName = model.ContactName,
                ContactRole = model.ContactRole,
                ContactPhone = model.ContactPhone,
                ContactEmail = model.ContactEmail,
                Description = model.Description
            };

            await _repository.AddAsync(need);

            TempData["Success"] = $"The need \"{need.Title}\" has been registered.";

            return RedirectToAction(nameof(Index));
        }
    }
}