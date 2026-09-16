using Heimevernet.Models;
using Heimevernet.Services;
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

        public IActionResult Index()
        {
            var allNeeds = _repository.GetAll();
            return View(allNeeds);
        }

        public IActionResult Details(int id)
        {
            var need = _repository.GetById(id);

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
        public IActionResult Create(Need need)
        {
            if (need.Type == NeedTypes.Other)
            {
                if (string.IsNullOrWhiteSpace(need.OtherType))
                {
                    ModelState.AddModelError(
                        nameof(need.OtherType),
                        "Please specify what type of need this is."
                    );
                }
                else
                {
                    need.Type = need.OtherType.Trim();
                }
            }

            if (!ModelState.IsValid)
            {
                return View(need);
            }

            _repository.Add(need);

            TempData["Success"] = $"The need \"{need.Title}\" has been registered.";

            return RedirectToAction(nameof(Index));
        }
    }
}