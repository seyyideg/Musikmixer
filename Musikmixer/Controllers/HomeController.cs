using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Logging;
using Musikmixer.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using RZP;
using TagLib;

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
            _dirUploads = @"Uploads/";
            _dirMixes = @"Mixes/";
            _dirConverted = @"Converted/";
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Mixes()
        {
            string[] mixes = Directory.GetFiles(_dirMixes);
            List<AllMixes> mixList = new List<AllMixes>();
            int i = 1;
            foreach (string mix in mixes)
            {
                mixList.Add(new AllMixes
                {
                    MixID = i++,
                    MixTitle = Path.GetFileNameWithoutExtension(mix),
                    MixPath = "~/Mixes/" + Path.GetFileName(mix)
                });
            }
            
            return View(mixList);
        }
        public FileResult DownloadMix(string Name)
        {
            string FileNameWithPath = Path.Combine(_dirMixes, Name);
            byte[] bytes = System.IO.File.ReadAllBytes(FileNameWithPath + ".mp3");
            return File(bytes, "application/octet-stream", Name);
        }
        public IActionResult MultipleFiles(IEnumerable<IFormFile> files, string titel)
        {
            _dirUploads = titel + "/" + _dirUploads;
            _dirConverted = _dirUploads + @"Converted/";
            if (!Directory.Exists(_dirUploads))
            {
                Directory.CreateDirectory(_dirUploads);
            }
            if (!Directory.Exists(_dirMixes))
            {
                Directory.CreateDirectory(_dirMixes);
            }
            if (!Directory.Exists(_dirConverted))
            {
                Directory.CreateDirectory(_dirConverted);
            }
            // uploaded alle files
            foreach (var file in files)
            {
                using (var fileStream = new FileStream(Path.Combine(_dirUploads, file.FileName), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
            }
            string[] _files = Directory.EnumerateFiles(_dirUploads).ToArray();

            try
            {
                ConvertFiles(_files);
            }
            catch (Exception)
            {
                ViewBag.Error = "format wird nicht Unterschtüzt";
                return View("Index");
            }
            finally
            {
                StitchFiles(titel);
            }
            return RedirectToAction("Mixes");
        }
        public void ConvertFiles(string[] files)
        {
            //Converted alle files zu .wav
            int i = 0;
            foreach (var file in files)
            {
                var infile = file;
                var outfile = _dirConverted + $"/fileconverted{i++}";
                FileInfo fileInfo = new FileInfo(infile);
                //Liest die Audiodatei mit dem Reader der alles Encoded;
                using (var reader = new MediaFoundationReader(infile))
                {
                    //Schreibt das gelesene auf eine neue .wav Datei
                    WaveFileWriter.CreateWaveFile(outfile, reader);
                    fileInfo.Delete();
                }

            }
            string[] _files = Directory.EnumerateFiles(_dirConverted).ToArray();
            DetectBPM(_files);
        }
        public void DetectBPM(string[] files)
        {
            //Detected und setzt im ID3-Tag die BPM der einzelne files
            string filename;
            double bpm;

            foreach (var file in Directory.EnumerateFiles(_dirConverted))
            {
                var fileinfo = new FileInfo(file);
                filename = fileinfo.Name;
                var outfile = _dirConverted + filename + ".mp3";
                //BPMDetector zusätzliches plugin zur Naudio Library zum messen der BPM
                BPMDetector bPMDetector = new BPMDetector(file);
                bpm = bPMDetector.getBPM();
                //Konvertiert die Dateien zu MP3 um den Tag setzten zu können
                using (var reader = new MediaFoundationReader(file))
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, outfile);
                    fileinfo.Delete();
                }
                //Setzt die BPM in den Id3-Tag
                var tfile = TagLib.File.Create(outfile);
                tfile.Tag.BeatsPerMinute = Convert.ToUInt32(bpm);
                tfile.Save();

                Debug.WriteLine("Filename: " + outfile + ", File BPM: " + bpm);
            }
        }
        public void StitchFiles(string title)
        {
            //Eröffnet einen neuen FileStream zum schreiben einer neuen Datei
            using (var output = new FileStream(Path.Combine(_dirMixes, title + ".mp3"), FileMode.Create, FileAccess.Write))
                foreach (string file in Directory.EnumerateFiles(_dirConverted))
                {
                    {
                        //Liest die Mp3 Dateien
                        Mp3FileReader reader = new Mp3FileReader(file);
                        //Setzt Id3 Tags für die Datei beim ersten durchlauf 
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
            //bool startFade = false;
            //TimeSpan timeSpan = new TimeSpan(00, 00, 12);
            //MemoryStream memRest = new MemoryStream(256000);
            //Debug.WriteLine("song current time: " + reader.CurrentTime);
            //if (reader.TotalTime - reader.CurrentTime < timeSpan && startFade != true)
            //fade.BeginFadeOut(12000);

            //Debug.WriteLine("<----beep beep fadeout should start here---->");
            //startFade = true;
            //ISampleProvider sampleProvider = reader.ToSampleProvider();
            //var fade = new FadeInOutSampleProvider(sampleProvider);
            //{

            //    while ((frame = reader.ReadNextFrame()) != null)
            //    {
            //        byte[] rest = frame.RawData;

            //        memRest.Write(frame.RawData, 0, frame.RawData.Length);
            //    }
            //}

            //if (memRest.Length.ToString() != "" && reader.ReadNextFrame() != null)
            //{
            //    output.Write(memRest.ToArray(), 0, memRest.ToArray().Length);
            //    output.Write(frame.RawData, 0, frame.RawData.Length);
            //    startFade = false;
            //}
            //else if (reader.ReadNextFrame() != null)
            //{
            //}

            //byte[] buffer = new byte[1024];
            //WaveFileWriter waveFileWriter = null;

            //foreach (string sourceFile in Directory.EnumerateFiles(_dirConverted))
            //{
            //    using (MediaFoundationReader reader = new MediaFoundationReader(sourceFile))
            //    {
            //        ISampleProvider sampleProvider = reader.ToSampleProvider();
            //        var fade = new FadeInOutSampleProvider(sampleProvider);
            //        fade.BeginFadeOut(12000);
            //        var player = new WaveOutEvent();
            //        player.pos
            //        player.Init(fade);
            //        player.Play();
            //        while (player.PlaybackState == PlaybackState.Playing)
            //        {
            //            WaveFileWriter.CreateWaveFile(Path.Combine(_dirMixes, "mix.wav"), new SampleToWaveProvider(fade));
            //        }

            //    }
            //}


        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
