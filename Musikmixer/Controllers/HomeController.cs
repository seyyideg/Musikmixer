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
using NAudio.Wave;

namespace Musikmixer.Controllers
{
    public class HomeController : Controller
    {
        private IWebHostEnvironment _env;
        private string _dirUploads;
        private string _dirMixes;
        private string _dirConverted;

        public HomeController(IWebHostEnvironment env)
        {
            _env = env;
            _dirUploads = @"Uploads";
            _dirMixes = @"Uploads/Mixes";
            _dirConverted = @"Uploads/Converted";
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult MultipleFiles(IEnumerable<IFormFile> files)
        {
            int i = 0;
            foreach (var file in files)
            {
                using (var fileStream = new FileStream(Path.Combine(_dirUploads, file.FileName), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
            }
            return RedirectToAction("ConvertFiles");
        }
        public IActionResult ConvertFiles()
        {
            int i = 0;
            foreach (var file in Directory.EnumerateFiles(_dirUploads))
            {
                var infile = file;
                var outfile = $"Uploads/Converted/fileconverted{i++}.mp3";

                using (var reader = new MediaFoundationReader(infile))
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, outfile);
                }

            }
            return RedirectToAction("StitchFiles");
        }
        public IActionResult StitchFiles()
        {
            using (var output = new FileStream(Path.Combine(_dirMixes, "mix.mp3"), FileMode.Create, FileAccess.Write))
                foreach (string file in Directory.EnumerateFiles(_dirConverted))
                {
                    {
                        Mp3FileReader reader = new Mp3FileReader(file);
                        if ((output.Position == 0) && (reader.Id3v2Tag != null))
                        {
                            output.Write(reader.Id3v2Tag.RawData, 0, reader.Id3v2Tag.RawData.Length);
                        }
                        Mp3Frame frame;
                        while ((frame = reader.ReadNextFrame()) != null)
                        {
                            output.Write(frame.RawData, 0, frame.RawData.Length);
                        }
                    }

                }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
