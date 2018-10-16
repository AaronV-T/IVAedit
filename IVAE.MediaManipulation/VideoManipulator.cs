using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;

namespace IVAE.MediaManipulation
{
  public class VideoManipulator
  {
    public event Action<float> OnProgress;
    private double currentVideoDurationInMS = -1;
    private int totalSteps = -1, currentStep = -1;

    public void CropVideo(string outputPath, string videoPath, int x, int y, int width, int height)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(videoPath))
        throw new ArgumentNullException(nameof(videoPath));
      if (x < 0)
        throw new ArgumentOutOfRangeException(nameof(x));
      if (y < 0)
        throw new ArgumentOutOfRangeException(nameof(y));
      if (width <= 0)
        throw new ArgumentOutOfRangeException(nameof(width));
      if (height <= 0)
        throw new ArgumentOutOfRangeException(nameof(height));
      if ((x == 0 || y == 0) && x != y)
        throw new ArgumentException("x and y must both be zero or both be positive.");

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      string xArg = string.Empty;
      if (x > 0)
        xArg = $":{x}";

      string yArg = string.Empty;
      if (y > 0)
        yArg = $":{y}";

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{videoPath}\" -vf \"crop={width}:{height}{xArg}{yArg}\" -codec:a copy \"{outputPath}\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public string ExtractAudioFromVideo(string outputPathWithoutExtension, string videoPath)
    {
      if (string.IsNullOrEmpty(outputPathWithoutExtension))
        throw new ArgumentNullException(nameof(outputPathWithoutExtension));
      if (string.IsNullOrEmpty(videoPath))
        throw new ArgumentNullException(nameof(videoPath));

      string audioCodec = new FFProbeProcessRunner().Run($"-v error -select_streams a:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 {videoPath}");
      string outputPath = outputPathWithoutExtension + MediaTypeHelper.GetFileExtensionForAudioCodec(audioCodec);

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{videoPath}\" -vn -codec:a copy \"{outputPath}\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;

      return outputPath;
    }

    public void MakeGifvFromGif(string outputPath, string gifPath)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(gifPath))
        throw new ArgumentNullException(nameof(gifPath));

      string outputPathMP4 = $@"{System.IO.Path.GetDirectoryName(outputPath)}\{System.IO.Path.GetFileNameWithoutExtension(outputPath)}.mp4";

      if (System.IO.File.Exists(outputPathMP4))
        System.IO.File.Delete(outputPathMP4);

      // movflags – This option optimizes the structure of the MP4 file so the browser can load it as quickly as possible.
      // pix_fmt – MP4 videos store pixels in different formats. We include this option to specify a specific format which has maximum compatibility across all browsers.
      // vf – MP4 videos using H.264 need to have a dimensions that are divisible by 2. This option ensures that’s the case.
      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.Run($"-i \"{gifPath}\" -movflags faststart -pix_fmt yuv420p -vf \"scale = trunc(iw / 2) * 2:trunc(ih / 2) * 2\" \"{outputPathMP4}\"");

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      System.IO.File.Move(outputPathMP4, outputPath);
    }

    public void StabilizeVideo(string outputPath, string videoPath)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(videoPath))
        throw new ArgumentNullException(nameof(videoPath));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{videoPath}\" -vf deshake \"{outputPath}\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public void Trim(string outputPath, string filePath, string startTime, string endTime)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(filePath))
        throw new ArgumentNullException(nameof(filePath));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);
      
      string startArg = string.Empty;
      if (!string.IsNullOrEmpty(startTime))
        startArg = $"-ss {startTime} ";

      string endArg = string.Empty;
      if (!string.IsNullOrEmpty(endTime))
        endArg = $"-to {endTime} ";

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.Run($"-i \"{filePath}\" {startArg}{endArg}-codec copy \"{outputPath}\"");
    }

    public void Test(string path)
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

    private void DurationMessageReceived(double duration)
    {
      currentVideoDurationInMS = duration;
    }

    private void TimeMessageReceived(double time)
    {
      if (currentVideoDurationInMS <= 0 || currentStep <= 0 || totalSteps <= 0)
        return;

      float baseProgress = (currentStep - 1) / (float)totalSteps;
      float currentStepProgress = (float)(time / currentVideoDurationInMS);
      float totalProgress = baseProgress + (currentStepProgress / totalSteps);

      OnProgress?.Invoke(totalProgress);
    }
  }
}
