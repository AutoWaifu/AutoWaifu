
Official website: http://autowaifu.azurewebsites.net

# AutoWaifu

Automatically convert your large collections of manga/etc to any scale or size with waifu2x!

## How it works
AutoWaifu monitors an input folder for any changes and loads existing files into its list of tasks. When there are tasks, AutoWaifu launches waifu2x-caffe on one of the input images and waits for waifu2x-caffe to finish. The task won't run if there is already a file in the Output folder with the same name. File names and folder structure are preserved.


## TODOs

- Move from IWaifuTask managing all processing to IJob system, tasks return jobs which can be scheduled by the app (In-progress)
- Support for GPU upscaling on non-NVIDIA cards (In-progress)
- Support for alternative waifu2x converters
- Start/stop processing from status server
- Standardize context menu on all item lists
- Smaller log output files
- Faster loading of status server logs when there are many, many logs (will currently load and deserializes all 50MB of a JSON file for each request)
- Profiles
- Search item list
- Sort item list
- Rename files within AutoWaifu
- Detect and respond to file renaming
- Change default tmp folder to %LocalAppData%
- Allow tmp folder to change in settings; if unable to create/access tmp folder, fall back to %LocalAppData%
- If tmp folder is changed, migrate working files to new tmp folder
- Support task suspend
- Remove MP4 conversion limit of 9999 frames
- Filter log by task/group ID
- Duplicate image detection
- Time remaining estimates
- Capture and report image upscale progress
- Capture and report animation split/merge progress
- Animation framerate upconversion
- Fix/simplify embedded data-binding XAML controls
