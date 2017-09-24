using Ionic.Zip;
using Ionic.Zlib;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoWaifu.UpdatesManager
{
    class Program
    {

        static string ProjectName => ConfigurationManager.AppSettings["ProjectName"];



        static IEnumerable<string> FilterUpdateFiles(IEnumerable<string> files)
        {
            string[] bannedExtensions = new string[]
            {
            };

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);

                if (bannedExtensions.Contains(ext))
                    continue;

                yield return file;
            }
        }

        static string MakeUpdateInfoText(string version, string packageName)
        {
            string baseUrl = ConfigurationManager.AppSettings["HttpDeployBaseFolder"];

            var outputLines = new[]
            {
                version,
                (baseUrl + '/' + packageName).Replace("//", string.Empty)
            };

            return string.Join("\n", outputLines);
        }


        [STAThread]
        static void Main(string[] args)
        {
            string updatesLocation;




            CommonOpenFileDialog ofd = new CommonOpenFileDialog();
            ofd.IsFolderPicker = true;
            //ofd.InitialDirectory = Directory.GetCurrentDirectory();

            if (ofd.ShowDialog() != CommonFileDialogResult.Ok)
                return;




            //FolderBrowserDialog fbd = new FolderBrowserDialog();
            //fbd.SelectedPath = Directory.GetCurrentDirectory();

            //if (fbd.ShowDialog() != DialogResult.OK)
            //    return;




            updatesLocation = ofd.FileName;
            //updatesLocation = fbd.SelectedPath;

            var deploymentFiles = Directory.EnumerateFiles(updatesLocation, "*", SearchOption.AllDirectories);

            deploymentFiles = FilterUpdateFiles(deploymentFiles);


            string updateVersion = null;

            while (updateVersion == null)
            {
                Console.Write("This update is version: ");
                updateVersion = Console.ReadLine();

                if (updateVersion.Trim().Length == 0)
                    updateVersion = null;
            }





            string outputPackageName = $"{ProjectName}_{updateVersion}.zip";


            Console.WriteLine("Making package...");

            using (var zip = new ZipFile(outputPackageName))
            {
                long totalBytesTransferred = 0;

                var lastLogTime = DateTime.Now;
                var logInterval = TimeSpan.FromSeconds(2);

                zip.SaveProgress += (s, e) =>
                {
                    if (e.EventType != ZipProgressEventType.Saving_EntryBytesRead)
                        return;

                    totalBytesTransferred += e.BytesTransferred;

                    var now = DateTime.Now;
                    if (now - lastLogTime < logInterval)
                        return;

                    Console.WriteLine($"{totalBytesTransferred / 1e3}KB written");
                };
                
                //  TODO - Specify storage directory, trim files relative to package data location

                foreach (var file in deploymentFiles)
                    zip.AddFile(file);

                zip.Save();
            }



            Console.WriteLine("Wrote package to {0}", outputPackageName);




            Console.ReadLine();
        }
    }
}
