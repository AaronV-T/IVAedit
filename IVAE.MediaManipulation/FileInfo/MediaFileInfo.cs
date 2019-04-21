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

    public bool HasAudio { get { return FileHasAudio(this); } }
    public bool HasVideo { get { return FileHasVideo(this); } }

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
    }

    private MediaFileInfo(FormatInfo format, IReadOnlyList<AudioStreamInfo> audioStreams, IReadOnlyList<VideoStreamInfo> videoStreams)
    {
      Format = format;
      AudioStreams = audioStreams;
      VideoStreams = videoStreams;
    }
  }
}
