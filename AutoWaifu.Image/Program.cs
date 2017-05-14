using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Image
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This is a tool to provide GIF upscaling in absence of a stable implementation of AutoWaifu.exe.");
            Console.WriteLine("The necessary files - waifu2x-caffe - should have been included with this software.");
            Console.WriteLine("Download the latest version at http://autowaifu.azurewebsites.net.");

            Console.WriteLine();

            Console.WriteLine("This tool will operate waifu2x-caffe using CPU upscaling.");

            Console.WriteLine();

            Console.WriteLine("Output files will be store in an 'output' folder with this program.");

            Console.WriteLine();

            Console.WriteLine("Press ENTER to select your images.");

            Console.ReadLine();


            Console.WriteLine("Do you want to auto-convert PNGs to JPEGs? PNGs will probably be very large.");
            Console.WriteLine("y/n: ");


            throw new NotImplementedException();
        }
    }
}
