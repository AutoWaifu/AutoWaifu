




# Old docs below

# Todo - rewrite this doc

---

# AutoWaifu

Automatically convert your large collections of manga/etc to any scale or size with waifu2x!

## How it works
AutoWaifu monitors an input folder for any changes and loads existing files into its list of tasks. When there are tasks, AutoWaifu launches waifu2x-caffe on one of the input images and waits for waifu2x-caffe to finish. The task won't run if there is already a file in the Output folder with the same name. File names and folder structure are preserved.

Waifu2x-caffe is used since it comes with preset filters and a simple command-line interface.

Multiple waifu2x-caffe tasks can be ran by changing the `maxParallel` option in config.txt.


## Setup
Download AutoWaifu.zip and extract. For simplicity, make an Input folder and Output folder as well. The Input and Output folders should be in the same directory as AutoImage.exe. ie, if AutoImage.exe is at MyFolder/AutoImage.exe, you should also have MyFolder/Output and MyFolder/Input folders.

After you've set up those folders, move your input files to the Input folder you just made, and run AutoImage.exe. If there are no config/compatibility errors, it should show a list of your input files on the left, a list of processed output files on the right, and a list of current tasks in the center.


## Troubleshooting
The window can sometimes hang after not detecting that waifu2x stopped running. If you don't see any waifu2x processes in Task Manager, and AutoWaifu says it's trying to do work, then force-quit AutoWaifu and run again.

If you have any questions shoot me a message on reddit (/u/autowaifu) or submit an issue here.


## Warning - Limitations!
- Windows-only
- Requires a CUDA/cuDNN-capable NVIDIA card
- cuDNN requirements: Pascal, Kepler, Maxwell, Tegra K1 or Tegra X1 GPU (GTX 400-series or greater)

Adding support for CPU processing should be simple, haven't gotten to it

Adding support for AMD GPUs with OpenCL will be much more difficult :(

-----------------------------

You can find some options in config.txt listed below:

- maxHeight: Maximum height of the output file (preserves ratio)
- maxWidth: Maximum width of the output file (preserves ratio)
- waifuCaffePath: Path to waifuCaffe (required) with CUDA/cuDNN files (available in release package above)
- inDir: Folder to watch and use as input for processing
- outDir: Output folder for upscaled images
- scale: Scale factor (ie 8 = 8x), preserving maxHeight and maxWidth
- priority: CPU priority for each process doing the scaling
- autoScale: Direct output will vary in size based on input - should we auto-scale according to maxWidth/maxHeight?
- superSamples: Will render the image larger but will scale down for smoother detail
- maxParallel: The maximum number of waifu2x processes we want to run
