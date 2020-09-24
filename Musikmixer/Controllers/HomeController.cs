using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Musikmixer.Models;

namespace Musikmixer.Controllers
{
    public class HomeController : Controller
    {
        private IWebHostEnvironment _env;
        private string _dir;
        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
            _dir = _env.ContentRootPath;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SingleFile(IFormFile file)
        {

            using (var fileStream = new FileStream(Path.Combine(_dir, "file.mp3"), FileMode.Create, FileAccess.Write))
            {
                file.CopyTo(fileStream);
            }

            return RedirectToAction("Index");
        }

        public IActionResult MultipleFiles(IEnumerable<IFormFile> files)
        {
            int i = 0;
            foreach (var file in files)
            {
                using (var fileStream = new FileStream(Path.Combine(_dir, $"file{i++}.mp3"), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
