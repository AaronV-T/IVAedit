using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class VideoManipulator
  {
    public event Action<float> OnProgress;
    private double currentVideoDurationInMS = -1;
    private int totalSteps = -1, currentStep = -1;

    public void AdjustVideoSpeed(string outputPath, string videoFilepath, float newPlaybackRate, float newFramerate = 0, bool alsoChangeAudio = false)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(videoFilepath))
        throw new ArgumentNullException(nameof(videoFilepath));

      if (alsoChangeAudio && !MediaFileInfo.FileHasAudio(videoFilepath))
        alsoChangeAudio = false;

      if (alsoChangeAudio) {
        if (newPlaybackRate < 0)
          throw new ArgumentOutOfRangeException(nameof(newPlaybackRate));
        if (newPlaybackRate < 0.5f && !(new List<float> { 0.25f, 0.125f, 0.0625f, 0.03125f, 0.015625f, 0.0078125f, 0.00390625f }).Contains(newPlaybackRate))
          throw new ArgumentException("Playback rates lower than 0.5 must be: 0.25, 0.125, 0.0625, 0.03125, 0.015625, 0.0078125, or 0.00390625.");
        if (newPlaybackRate > 2 && !(new List<float> { 4, 8, 16, 32, 64, 128, 256 }).Contains(newPlaybackRate))
          throw new ArgumentException("Playback rates higher than 2 must be: 4, 8, 16, 32, 64, 128, or 256.");
      }

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      float setptsVal = 1 / newPlaybackRate;

      string frameRateArg = string.Empty;
      if (newFramerate > 0)
        frameRateArg = $"-r {newFramerate} ";

      string args;
      if (alsoChangeAudio)
      {
        string audioArg;
        if (newPlaybackRate < 0.5f)
        {
          audioArg = "atempo=0.5";
          float temp = newPlaybackRate;
          while (temp < 0.5f)
          {
            temp /= 0.5f;
            audioArg += ",atempo=0.5";
          }
        }
        else if (newPlaybackRate > 2)
        {
          audioArg = "atempo=2.0";
          float temp = newPlaybackRate;
          while (temp > 2)
          {
            temp /= 2;
            audioArg += ",atempo=2.0";
          }
        }
        else
          audioArg = $"atempo={newPlaybackRate}";

        args = $"-i \"{videoFilepath}\" {frameRateArg}-filter_complex \"[0:v]setpts={setptsVal}*PTS[v];[0:a]{audioArg}[a]\" -map \"[v]\" -map \"[a]\" \"{outputPath}\"";
      }
      else
        args = $"-i \"{videoFilepath}\" {frameRateArg}-filter:v \"setpts={setptsVal}*PTS\" -an \"{outputPath}\"";

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run(args);

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public void CombineVideos(string outputPath, string videoPath1, string videoPath2, bool combineHorizontally)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(videoPath1))
        throw new ArgumentNullException(nameof(videoPath1));
      if (string.IsNullOrEmpty(videoPath2))
        throw new ArgumentNullException(nameof(videoPath2));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      string stackArg = null;
      if (combineHorizontally)
        stackArg = "hstack";
      else
        stackArg = "vstack";

      bool video1HasAudio = MediaFileInfo.FileHasAudio(videoPath1);
      bool video2HasAudio = MediaFileInfo.FileHasAudio(videoPath2);
      string args = null;
      if (video1HasAudio && video2HasAudio)
        args = $"-i \"{videoPath1}\" -i \"{videoPath2}\" -filter_complex \"[0:v][1:v]{stackArg}=inputs=2[v];[0:a][1:a]amerge[a]\" -map \"[v]\" -map \"[a]\" -ac 2 \"{outputPath}\"";
      else if (!video1HasAudio && !video2HasAudio)
        args = $"-i \"{videoPath1}\" -i \"{videoPath2}\" -filter_complex \"[0:v][1:v]{stackArg}=inputs=2[v]\" -map \"[v]\" \"{outputPath}\"";
      else
        throw new NotImplementedException();

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run(args);

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

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

      string outputPath = outputPathWithoutExtension + MediaTypeHelper.GetFileExtensionForAudioCodec(new MediaFileInfo(videoPath).AudioStreams[0].CodecName);

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

    public void MakeVideoFromGif(string outputPath, string gifPath)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(gifPath))
        throw new ArgumentNullException(nameof(gifPath));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      // movflags – This option optimizes the structure of the MP4 file so the browser can load it as quickly as possible.
      // pix_fmt – MP4 videos store pixels in different formats. We include this option to specify a specific format which has maximum compatibility across all browsers.
      // vf – MP4 videos using H.264 need to have a dimensions that are divisible by 2. This option ensures that’s the case.
      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.Run($"-i \"{gifPath}\" -movflags faststart -pix_fmt yuv420p -vf \"scale = trunc(iw / 2) * 2:trunc(ih / 2) * 2\" \"{outputPath}\"");
    }

    public void MakeImagesFromVideo(string outputDirectory, string videoPath, string fps)
    {
      if (string.IsNullOrEmpty(outputDirectory))
        throw new ArgumentNullException(nameof(outputDirectory));
      if (string.IsNullOrEmpty(videoPath))
        throw new ArgumentNullException(nameof(videoPath));
      if (string.IsNullOrEmpty(fps))
        throw new ArgumentNullException(nameof(fps));

      if (outputDirectory[outputDirectory.Length - 1] != '\\')
        outputDirectory += "\\";

      if (System.IO.Directory.Exists(outputDirectory))
        System.IO.Directory.Delete(outputDirectory, true);

      System.IO.Directory.CreateDirectory(outputDirectory);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{videoPath}\" -vf fps={fps} \"{outputDirectory}%d.png\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public void ResizeVideo(string outputPath, string videoPath, int width, int height)
    {
      if (width <= 0 && height <= 0)
        throw new ArgumentException($"width or height must be greater than 0 to resize a video.");

      if (width == 0)
        width = -2;
      if (height == 0)
        height = -2;

      ResizeVideoHelper(outputPath, videoPath, width.ToString(), height.ToString());
    }

    public void ScaleVideo(string outputPath, string videoPath, float scaleFactor)
    {
      if (scaleFactor <= 0)
        throw new ArgumentOutOfRangeException(nameof(scaleFactor));

      ResizeVideoHelper(outputPath, videoPath, $"trunc(iw*{scaleFactor}/2)*2", $"trunc(ih*{scaleFactor}/2)*2");
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

      string args = $"-i \"{filePath}\" {startArg}{endArg}-async 1 \"{outputPath}\"";

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.Run(args);
    }

    public void Test(string path)
    {
      using (System.IO.StreamWriter sw = new System.IO.StreamWriter("testoutput.txt"))
      {
        sw.WriteLine(new FFProbeProcessRunner().Run($"-v error -show_format -show_streams -print_format json {path}"));
      }

      System.Diagnostics.Process.Start("testoutput.txt");
    }

    private void DurationMessageReceived(double duration)
    {
      currentVideoDurationInMS = duration;
    }

    private void ResizeVideoHelper(string outputPath, string videoPath, string widthArg, string heightArg)
    {
      if (string.IsNullOrWhiteSpace(widthArg))
        throw new ArgumentNullException(nameof(widthArg));
      if (string.IsNullOrWhiteSpace(heightArg))
        throw new ArgumentNullException(nameof(heightArg));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{videoPath}\" -vf scale={widthArg}:{heightArg} \"{outputPath}\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;

      Console.WriteLine($"-i \"{videoPath}\" -vf \"scale={widthArg}:{heightArg}\" \"{outputPath}\"");
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
