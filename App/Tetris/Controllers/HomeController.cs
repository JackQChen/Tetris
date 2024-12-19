using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
            // 创建一个 100x100 的图像
            using (var image = new Image<Rgba32>(100, 100))
            {
                // 填充背景为白色
                image.Mutate(x => x.Fill(Brushes.Solid(Color.Green), new Rectangle(50, 50, 50, 50)));

                // 绘制一个红色的矩形
                var rectangle = new Rectangle(10, 10, 80, 80);
                image.Mutate(x => x.Fill(Color.Red, rectangle));

                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);  // 将图像保存为 PNG 格式 
                    // 将字节数组转换为Base64字符串
                    ViewData["image"] = Convert.ToBase64String(ms.ToArray());
                }
            }
            return View();
        }

    }
}