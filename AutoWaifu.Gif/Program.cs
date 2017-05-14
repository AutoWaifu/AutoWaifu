using AutoWaifu.DataModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoWaifu.Gif
{
    class Program
    {
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine 
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            PendingTask?.Teriminate();

            return true;
        }





        static WaifuTask PendingTask = null;

        static void SaveLastInputFolder(string lastInputFile)
        {
            
        }

        static string GetLastInputFolder()
        {
            return null;
            //return Registry.LocalMachine.GetValue("Software\\AutoWaifuImage\\LastInputFile") as string;
        }

        static bool IsTestRun = false;

        [STAThread]
        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            Console.WriteLine("This is a tool to provide GIF upscaling in absence of a stable implementation of AutoWaifu.exe.");
            Console.WriteLine("The necessary files - waifu2x-caffe and ffmpeg.exe - should have been included with this software.");
            Console.WriteLine("Download the latest version at http://autowaifu.azurewebsites.net.");

            Console.WriteLine();

            Console.WriteLine("This software will generate lots of files during operation and may take up lots of disk space.");

            Console.WriteLine();

            Console.WriteLine("This tool will operate waifu2x-caffe using CPU upscaling.");

            Console.WriteLine();

            Console.WriteLine("Output files will be stored in an 'output' folder with this program. Temporary files will be stored in a 'tmp' folder with this program. This folder may not be auto-cleaned - make sure to delete temporary files after processing completes.");

            Console.WriteLine();

            Console.WriteLine("Press ENTER to select your gifs. (or type 'args' to do a detailed test-run)");


            bool outputArgs = Console.ReadLine().ToLower().Trim() == "args";
            IsTestRun = outputArgs;

            bool hasFiles = true;
            if (!File.Exists("ffmpeg.exe"))
            {
                MessageBox.Show("Could not find 'ffmpeg.exe'!");
                hasFiles = false;
            }

            if (!Directory.Exists("waifu2x-caffe") || !File.Exists("waifu2x-caffe/waifu2x-caffe-cui.exe"))
            {
                MessageBox.Show("Could not find 'waifu2x-caffe/waifu2x-caffe-cui.exe'!");
                hasFiles = false;
            }

            if (!hasFiles)
                return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = GetLastInputFolder();
            ofd.Multiselect = true;
            ofd.CheckFileExists = true;
            ofd.Filter = "GIF Files|*.gif";

            if (ofd.ShowDialog() != DialogResult.OK || ofd.FileNames.Length == 0)
                return;

            SaveLastInputFolder(Path.GetDirectoryName(ofd.FileNames[0]));

            if (!Directory.Exists("output"))
                Directory.CreateDirectory("output");
            if (!Directory.Exists("tmp"))
                Directory.CreateDirectory("tmp");
            if (!Directory.Exists("tmp/input"))
                Directory.CreateDirectory("tmp/input");
            if (!Directory.Exists("tmp/output"))
                Directory.CreateDirectory("tmp/output");

            AppSettings.Main.ConversionMode = AppSettings.WaifuConvertMode.CPU;
            AppSettings.Main.MaxParallel = 1;
            AppSettings.Main.Priority = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            AppSettings.Main.Waifu2xCaffeDir = "./waifu2x-caffe";
            AppSettings.Main.UseScaleInsteadOfSize = true;
            AppSettings.Main.TempDir = "tmp";
            AppSettings.Main.OutputDir = "output";

            foreach (var gifFile in ofd.FileNames)
            {
                var startTime = DateTime.Now;
                Console.WriteLine("Starting upconversion for {0} at {1}", Path.GetFileName(gifFile), startTime);
                UpscaleGif(gifFile, Path.Combine(AppSettings.Main.TempDirOutput, Path.GetFileName(gifFile)), skipUpscaleMerge: outputArgs);

                var endTime = DateTime.Now;
                var taskDuration = endTime - startTime;
                Console.WriteLine("Conversion for {0} completed at {1}, took {2} hours", Path.GetFileName(gifFile), endTime, taskDuration.TotalHours);
            }

            Console.ReadLine();
        }

        static void ResizeEvenDims(string[] files)
        {
            foreach (var file in files)
            {
                using (var img = Bitmap.FromFile(file))
                {
                    int newWidth = img.Width + (img.Width % 2);
                    int newHeight = img.Height + (img.Height % 2);

                    if (newWidth == img.Width)
                        continue;
                    if (newHeight == img.Height)
                        continue;

                    using (var newImg = new Bitmap(newWidth, newHeight))
                    {
                        using (Graphics g = Graphics.FromImage(newImg))
                        {
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                            using (var wrapMode = new ImageAttributes())
                            {
                                var dstRect = new Rectangle(0, 0, newWidth, newHeight);

                                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                                g.DrawImage(img, dstRect, 0, 0, newWidth, newHeight, GraphicsUnit.Pixel, wrapMode);
                            }
                        }

                        newImg.Save(file);
                    }
                }
            }
        }

        static void UpscaleGif(string gifFile, string outputFile, bool skipUpscaleMerge)
        {
            WaifuTask waifuTask = new WaifuTask
            {
                ConvertMode = AppSettings.WaifuConvertMode.CPU,
                InputFile = gifFile,
                OutputFile = outputFile
            };

            waifuTask.IsTestRun = skipUpscaleMerge;

            PendingTask = waifuTask;

            if (!IsTestRun)
            {
                float scale = -1;
                string scaleString;
                do
                {
                    Console.Write("Enter the scale factor for your GIF upscaling: ");
                    scaleString = Console.ReadLine();
                    Console.WriteLine();

                } while (scale < 0 && !float.TryParse(scaleString, out scale));

                AppSettings.Main.Scale = scale;
            }

            var upscaleTask = waifuTask.StartSpecific(gifFile, outputFile);

            Console.WriteLine("Close this window at any time to cancel this and all other tasks and close waifu2x-caffe.");
            while (!upscaleTask.IsCompleted)
            {
                Task.Delay(100).Wait();
            }

            PendingTask = null;
        }
    }
}
