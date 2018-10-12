using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;

namespace IVAE.MediaManipulation
{
  public class AudioManipulator
  {
    public event Action<float> OnProgress;
    private double currentFileDurationInMS = -1;
    private int totalSteps = -1, currentStep = -1;

    public void NormalizeVolume(string outputPath, string videoPath)
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
      totalSteps = 2;

      // First pass.
      currentStep = 1;
      string output = fpr.Run($"-i \"{videoPath}\" -af loudnorm=I=-23:LRA=7:tp=-2:print_format=json -f null -");

      int startIndex = output.LastIndexOf("{");
      int length = output.LastIndexOf("}") - startIndex + 1;
      Dictionary<string, string> loudnormOutput = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(output.Substring(startIndex, length));

      // Second pass.
      currentStep = 2;
      fpr.Run($"-i \"{videoPath}\" " +
        $"-af loudnorm=I=-23:LRA=7:tp=-2:" +
        $"measured_I={loudnormOutput["input_i"]}:" +
        $"measured_LRA={loudnormOutput["input_lra"]}:" +
        $"measured_tp={loudnormOutput["input_tp"]}:" +
        $"measured_thresh={loudnormOutput["input_thresh"]}:" +
        $"offset={loudnormOutput["target_offset"]}:" +
        $"linear=true:print_format=json -vcodec copy {outputPath}");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    private void DurationMessageReceived(double duration)
    {
      currentFileDurationInMS = duration;
    }

    private void TimeMessageReceived(double time)
    {
      if (currentFileDurationInMS <= 0 || currentStep <= 0 || totalSteps <= 0)
        return;

      float baseProgress = (currentStep - 1) / (float)totalSteps;
      float currentStepProgress = (float)(time / currentFileDurationInMS);
      float totalProgress = baseProgress + (currentStepProgress / totalSteps);

      OnProgress?.Invoke(totalProgress);
    }
  }
}
