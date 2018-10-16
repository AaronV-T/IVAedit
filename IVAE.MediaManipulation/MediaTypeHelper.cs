using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public enum MediaType { AUDIO = 0, IMAGE = 1, VIDEO = 2, NONE = 3 }

  public static class MediaTypeHelper
  {
    public static IReadOnlyCollection<string> AudioExtensions = new HashSet<string> { ".mp3", ".ogg", ".wav", ".wma" };
    public static IReadOnlyCollection<string> ImageExtensions = new HashSet<string> { ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tif", ".tiff" };
    public static IReadOnlyCollection<string> VideoExtensions = new HashSet<string> { ".avi", ".gifv", ".mp4", ".webm", ".wmv" };

    public static string GetFileExtensionForAudioCodec(string audioCodec)
    {
      audioCodec = audioCodec.Trim().ToLower();

      if (audioCodec == "aac")
        return ".aac";
      if (audioCodec == "mp3")
        return ".mp3";
      else if (audioCodec == "vorbis")
        return ".ogg";
      else if (audioCodec == "wav")
        return ".wav";
      else
        throw new NotImplementedException($"Unsupported codec: {audioCodec}");
    }

    public static MediaType GetMediaTypeFromFileName(string fileName)
    {
      string fileExtension = System.IO.Path.GetExtension(fileName).ToLower();

      if (AudioExtensions.Contains(fileExtension))
        return MediaType.AUDIO;
      else if (ImageExtensions.Contains(fileExtension))
        return MediaType.IMAGE;
      else if (VideoExtensions.Contains(fileExtension))
        return MediaType.VIDEO;
      else
        return MediaType.NONE;
    }
  }
}
