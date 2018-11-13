using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class AudioManipulator
  {
    public event Action<float> OnProgress;
    private double currentFileDurationInMS = -1;
    private int totalSteps = -1, currentStep = -1;

    public void AdjustAudioSpeed(string outputPath, string audioFilePath, float playbackRateModifier)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(audioFilePath))
        throw new ArgumentNullException(nameof(audioFilePath));
      if (playbackRateModifier < 0)
        throw new ArgumentOutOfRangeException(nameof(playbackRateModifier));
      if (playbackRateModifier < 0.5f && !(new List<float> { 0.25f, 0.125f, 0.0625f, 0.03125f, 0.015625f, 0.0078125f, 0.00390625f }).Contains(playbackRateModifier))
        throw new ArgumentException("Playback rates lower than 0.5 must be: 0.25, 0.125, 0.0625, 0.03125, 0.015625, 0.0078125, or 0.00390625.");
      if (playbackRateModifier > 2 && !(new List<float> { 4, 8, 16, 32, 64, 128, 256 }).Contains(playbackRateModifier))
        throw new ArgumentException("Playback rates higher than 2 must be: 4, 8, 16, 32, 64, 128, or 256.");

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      string audioArg;
      if (playbackRateModifier < 0.5f)
      {
        audioArg = "atempo=0.5";
        float temp = playbackRateModifier;
        while (temp < 0.5f)
        {
          temp /= 0.5f;
          audioArg += ",atempo=0.5";
        }
      }
      else if (playbackRateModifier > 2)
      {
        audioArg = "atempo=2.0";
        float temp = playbackRateModifier;
        while (temp > 2)
        {
          temp /= 2;
          audioArg += ",atempo=2.0";
        }
      }
      else
        audioArg = $"atempo={playbackRateModifier}";

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{audioFilePath}\" -filter:a \"{audioArg}\" -vn \"{outputPath}\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public void AdjustVolume(string outputPath, string filePath, string volume)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(filePath))
        throw new ArgumentNullException(nameof(filePath));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      string output = fpr.Run($"-i \"{filePath}\" -af \"volume={volume}\" \"{outputPath}\"");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public void NormalizeVolume(string outputPath, string filePath)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(filePath))
        throw new ArgumentNullException(nameof(filePath));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 2;

      // First pass.
      currentStep = 1;
      string output = fpr.Run($"-i \"{filePath}\" -af loudnorm=I=-23:LRA=7:tp=-2:print_format=json -f null -");

      int startIndex = output.LastIndexOf("{");
      int length = output.LastIndexOf("}") - startIndex + 1;
      Dictionary<string, string> loudnormOutput = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(output.Substring(startIndex, length));

      // Second pass.
      currentStep = 2;
      string vcodecArg = string.Empty;
      if (MediaTypeHelper.GetMediaTypeFromFileName(filePath) == MediaType.VIDEO)
        vcodecArg = "-vcodec copy ";

      fpr.Run($"-i \"{filePath}\" " +
        $"-af loudnorm=I=-23:LRA=7:tp=-2:" +
        $"measured_I={loudnormOutput["input_i"]}:" +
        $"measured_LRA={loudnormOutput["input_lra"]}:" +
        $"measured_tp={loudnormOutput["input_tp"]}:" +
        $"measured_thresh={loudnormOutput["input_thresh"]}:" +
        $"offset={loudnormOutput["target_offset"]}:" +
        $"linear=true:print_format=json {vcodecArg}{outputPath}");

      fpr.OnDurationMessage -= DurationMessageReceived;
      fpr.OnTimeMessage -= TimeMessageReceived;
    }

    public void ReverseAudio(string outputPath, string audioPath)
    {
      if (string.IsNullOrEmpty(outputPath))
        throw new ArgumentNullException(nameof(outputPath));
      if (string.IsNullOrEmpty(audioPath))
        throw new ArgumentNullException(nameof(audioPath));

      if (System.IO.File.Exists(outputPath))
        System.IO.File.Delete(outputPath);

      FFmpegProcessRunner fpr = new FFmpegProcessRunner();
      fpr.OnDurationMessage += DurationMessageReceived;
      fpr.OnTimeMessage += TimeMessageReceived;
      totalSteps = 1;

      currentStep = 1;
      fpr.Run($"-i \"{audioPath}\" -af areverse \"{outputPath}\"");

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
