using Heimevernet.Models;
using Heimevernet.Services;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers
{
    /// <summary>
    /// Controller responsible for CRUD operations for <see cref="Heimevernet.Models.Need"/> entities.
    /// </summary>
    public class NeedController : Controller
    {
        private readonly INeedRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="NeedController"/> class.
        /// </summary>
        /// <param name="repository">Repository used to store and retrieve needs.</param>
        public NeedController(INeedRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Displays a list of all reported needs.</summary>
        /// <returns>A view containing the list of needs.</returns>
        public IActionResult Index()
        {
            var allNeeds = _repository.GetAll();
            return View(allNeeds);
        }

        /// <summary>Shows the details for a single need.</summary>
        /// <param name="id">Identifier of the need to display.</param>
        /// <returns>Details view when found or NotFound result.</returns>
        public IActionResult Details(int id)
        {
            var need = _repository.GetById(id);

            if (need == null)
            {
                return NotFound();
            }

            return View(need);
        }

        /// <summary>Displays the form to create a new need.</summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>Handles POST of a new need. Validates input and stores the entity in the repository.</summary>
        /// <param name="need">Bound need instance from the form.</param>
        /// <returns>Redirects to Index on success, otherwise redisplays the Create view.</returns>
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