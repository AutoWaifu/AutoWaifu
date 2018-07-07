
Official website: http://autowaifu.azurewebsites.net

# AutoWaifu

Automatically convert your large collections of manga/etc to any scale or size with waifu2x!

## How it works
AutoWaifu monitors an input folder for any changes and loads new files into its list of tasks, which stores the upscaled files in an output folder. Tasks can vary depending on the input file and user settings. There are generally 2 types of tasks:

- Image upscale
- Animation upscale

Image tasks use waifu2x-caffe to upscale an image to the desired resolution. This takes into account: Upscale type (percent, megapixels, max-width/height), denoising level, method (CPU, GPU, cuDNN).

Animation tasks consist of extraction, upscaling, and compilation steps. Extraction can use ImageMagick or ffmpeg. Ffmpeg is generally prefered since it has the most stable behavior. The upscaling step defers to multiple image tasks, which can be ran in parallel if the threads setting is high enough. Compilation uses ffmpeg or ImageMagick for GIFs, and only ffmpeg for videos.

### Server/Headless operation
AutoWaifu can host a basic web server that displays its processing state for remote viewing, and also run without displaying an interface (headless mode). These are enabled by running AutoWaifu with the commandline options `-status-server` and `-headless`, respectively. The server is available on port 4444.

## TODOs

##### Interface
- Standardize context menu on all item lists
- Search item list
- Sort item list
- Rename files within AutoWaifu
- Allow tmp folder to change in settings
- Time remaining estimates
- Capture and report image upscale progress
- Capture and report animation split/merge progress
- Profiles
- Start/stop processing from status server

##### Behavior
- Move from IWaifuTask managing all processing to IJob system, tasks return jobs which can be scheduled by the app (In-progress)
- Support for GPU upscaling on non-NVIDIA cards (In-progress)
- Support for alternative waifu2x converters
- Smaller log output files
- Faster loading of status server logs when there are many, many logs (will currently load and deserializes all 50MB of a JSON file for each request)
- Detect and respond to file renaming
- Change default tmp folder to %LocalAppData%
- If tmp folder is inaccessible, revert to %LocalAppData%
- If tmp folder is changed, migrate working files to new tmp folder
- Support task suspend
- Remove MP4 conversion limit of 9999 frames
- Filter log by task/group ID
- Duplicate image detection
- Animation framerate upconversion
- Fix/simplify embedded data-binding XAML controls
