# IVAedit

IVAedit (Image, Video, and Audio Edit) is a suite of Windows programs and libraries that offer a multitude of simple commands for editing image, video, and audio files. It is written in C# using .NET Framework.

## Project Descriptions

### IVAE.MediaManipulation

.NET Framework class library that contains all the methods for actually editing media files.

Uses the following 3rd-party libraries and applications:

* FFmpeg
* ImageMagick
* OpenCV
* Tesseract

### IVAE.RedditBot

.NET Framework console application that can be summoned by Reddit users to perform some of the IVAE.MediaManipulation functions.

Uses the following 3rd-party application:

* Youtube DL

### IVAeditGUI

.NET Framework WPF application provides a graphical user interface to all of the IVAE.MediaManipulation functions.
