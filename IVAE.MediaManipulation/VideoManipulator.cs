using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;

namespace IVAE.MediaManipulation
{
  public static class VideoManipulator
  {
    public static void MakeGifvFromGif(string outputPath, string gifPath)
    {
      string outputPathMP4 = $@"{System.IO.Path.GetDirectoryName(outputPath)}\{System.IO.Path.GetFileNameWithoutExtension(outputPath)}.mp4";

      if (System.IO.File.Exists(outputPathMP4))
        System.IO.File.Delete(outputPathMP4);

      // movflags – This option optimizes the structure of the MP4 file so the browser can load it as quickly as possible.
      // pix_fmt – MP4 videos store pixels in different formats. We include this option to specify a specific format which has maximum compatibility across all browsers.
      // vf – MP4 videos using H.264 need to have a dimensions that are divisible by 2.This option ensures that’s the case.
      FFmpegProcessRunner.Run($"-i \"{gifPath}\" -movflags faststart -pix_fmt yuv420p -vf \"scale = trunc(iw / 2) * 2:trunc(ih / 2) * 2\" \"{outputPathMP4}\"");

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      System.IO.File.Move(outputPathMP4, outputPath);
    }

    public static void StabilizeVideo(string outputPath, string videoPath)
    {
      //if (System.IO.File.Exists(outputPath))
        //System.IO.File.Delete(outputPath);

      FFmpegProcessRunner.Run($"-i \"{videoPath}\" -vf deshake \"{outputPath}\"");
    }

    public static void Test(string path)
    {
      using (VideoFileReader vfr = new VideoFileReader())
      {
        vfr.Open(path);

        for (int i = 0; i < vfr.FrameCount; i++)
        {
          //System.Drawing.Bitmap frame = vfr.ReadVideoFrame();
        }

        Console.WriteLine($"{vfr.BitRate} {vfr.FrameRate.ToDouble()} {vfr.FrameCount} {vfr.CodecName}");

        vfr.Close();
      }
    }
  }
}
