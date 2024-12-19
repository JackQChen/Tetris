using Microsoft.AspNetCore.Mvc;

namespace Tetris.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["image"] = Program.MainForm.Image;
            return View();
        }

    }
}