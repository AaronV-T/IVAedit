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
    public static void Test(string path)
    {
      using (VideoFileReader vfr = new VideoFileReader())
      {
        vfr.Open(path);

        Console.WriteLine($"{vfr.FrameRate.ToDouble()} {vfr.FrameCount}");

        for (int i = 0; i < vfr.FrameCount; i++)
        {
          System.Drawing.Bitmap frame = vfr.ReadVideoFrame();
        }

        vfr.Close();
      }
    }
  }
}
