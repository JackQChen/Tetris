using Microsoft.AspNetCore.Mvc;

namespace TetrisWeb.Controllers
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
            return View();
        }

        public IActionResult GetImage()
        {
            var imageBytes = Program.MainForm.Paint();
            return File(imageBytes, "image/png");
        }

    }
}