using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IVAE.MediaManipulation
{
  public class MediaFileInfo
  {
    public IReadOnlyList<AudioStreamInfo> AudioStreams;
    public IReadOnlyList<VideoStreamInfo> VideoStreams;
    public FormatInfo Format;

    public double? Duration { get; private set; }
    public int? FrameCount { get; private set; }
    public bool HasAudio { get { return FileHasAudio(this); } }
    public bool HasVideo { get { return FileHasVideo(this); } }
    public bool IsValidMediaFile { get; private set; }

    public static bool FileHasAudio(string path)
    {
      return FileHasAudio(new MediaFileInfo(path));
    }

    public static bool FileHasAudio(MediaFileInfo mfi)
    {
      IReadOnlyList<AudioStreamInfo> audioStreams = mfi.AudioStreams;
      return audioStreams != null && audioStreams.Count > 0;
    }

    public static bool FileHasVideo(string path)
    {
      return FileHasVideo(new MediaFileInfo(path));
    }

    public static bool FileHasVideo(MediaFileInfo mfi)
    {
      IReadOnlyList<VideoStreamInfo> videoStreams = mfi.VideoStreams;
      return videoStreams != null && videoStreams.Count > 0;
    }

    public MediaFileInfo(string path)
    {
      string probeResult = new FFProbeProcessRunner().Run($"-v error -show_format -show_streams -print_format json \"{path}\"");

      if (probeResult.Contains("Invalid data found when processing input"))
        return;

      IsValidMediaFile = true;

      Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(probeResult);

      List<AudioStreamInfo> audioStreams = new List<AudioStreamInfo>();
      List<VideoStreamInfo> videoStreams = new List<VideoStreamInfo>();
      if (dict.ContainsKey("streams"))
      {
        if (!(dict["streams"] is Newtonsoft.Json.Linq.JArray))
          throw new Exception($"'streams' was not a JArray.");

        var streams = ((Newtonsoft.Json.Linq.JArray)dict["streams"]).Cast<Newtonsoft.Json.Linq.JObject>().ToArray();
        foreach (var v in streams)
        {
          Dictionary<string, object> streamDict = v.ToObject<Dictionary<string, object>>();
          if (streamDict["codec_type"].ToString().ToLower() == "audio")
            audioStreams.Add(new AudioStreamInfo(streamDict));
          else if (streamDict["codec_type"].ToString().ToLower() == "video")
            videoStreams.Add(new VideoStreamInfo(streamDict));
        }
      }

      //Dictionary<string, object> format = new Dictionary<string, object>();
      FormatInfo format = null;
      if (dict.ContainsKey("format"))
        format = new FormatInfo(((Newtonsoft.Json.Linq.JObject)dict["format"]).ToObject<Dictionary<string, object>>());

      Format = format;
      AudioStreams = audioStreams;
      VideoStreams = videoStreams;

      foreach(AudioStreamInfo audioStreamInfo in AudioStreams)
      {
        if (audioStreamInfo.Duration != null && (Duration == null || audioStreamInfo.Duration > Duration))
          Duration = audioStreamInfo.Duration;
      }

      foreach (VideoStreamInfo videoStreamInfo in VideoStreams)
      {
        if (videoStreamInfo.Duration != null && (Duration == null || videoStreamInfo.Duration > Duration))
          Duration = videoStreamInfo.Duration;
        if (videoStreamInfo.NbFrames != null && (FrameCount == null || videoStreamInfo.NbFrames > FrameCount))
          FrameCount = (int)videoStreamInfo.NbFrames;
      }

      if (this.HasVideo && this.FrameCount == null)
      {
        string countFrameResult = new FFProbeProcessRunner().Run($"-v error -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of default=nokey=1:noprint_wrappers=1 \"{path}\"");
        int newlineIndex = countFrameResult.IndexOf('\n');
        if (newlineIndex > -1)
          countFrameResult = countFrameResult.Substring(0, newlineIndex);

        if (int.TryParse(countFrameResult, out int readFrameCount))
          FrameCount = readFrameCount;
      }
    }
  }
}
