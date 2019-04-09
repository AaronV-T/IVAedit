using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot
{
  public interface IVAECommand
  {
    string Execute(string filePath);
  }

  public static class IVAECommandFactory
  {
    public static List<IVAECommand> CreateCommands(string text)
    {
      List<IVAECommand> commands = new List<IVAECommand>();

      List<string> splitText = text.Split().ToList();

      if (splitText.Count == 0)
        return null;

      splitText.RemoveAt(0); // Remove bot username mention.

      while (splitText.Count > 0)
      {
        string commandText = splitText[0].ToUpper();
        if (commandText[0] == '-')
        {
          splitText.RemoveAt(0);

          int paramCount = 0;
          while (splitText.Count > paramCount && splitText[paramCount][0] != '-')
            paramCount++;

          Dictionary<string, string> parameters = new Dictionary<string, string>();
          if (paramCount > 0)
          {
            List<string> parameterStrings = splitText.GetRange(0, paramCount);
            splitText.RemoveRange(0, paramCount);

            foreach (string paramString in parameterStrings)
            {
              List<string> splitParamString = paramString.Split('=').ToList();
              if (splitParamString.Count != 2)
                throw new ArgumentException($"Invalid parameter '{paramString}'.");

              parameters.Add(splitParamString[0], splitParamString[1]);
            }
          }

          switch (commandText)
          {
            case "-ADJUSTSPEED":
              commands.Add(new AdjustSpeedCommand(parameters));
              break;
            case "-ADJUSTVOLUME":
              commands.Add(new AdjustVolumeCommand(parameters));
              break;
            case "-CROP":
              commands.Add(new CropCommand(parameters));
              break;
            case "-EXTRACTAUDIO":
              commands.Add(new ExtractAudioCommand(parameters));
              break;
            case "-GIFTOVIDEO":
              commands.Add(new GifToVideoCommand(parameters));
              break;
            case "-NORMALIZEVOLUME":
              commands.Add(new NormalizeVolumeCommand(parameters));
              break;
            case "-REMOVEAUDIO":
              commands.Add(new RemoveAudioCommand(parameters));
              break;
            case "-RESIZE":
              commands.Add(new ResizeCommand(parameters));
              break;
            case "-REVERSE":
              commands.Add(new ReverseCommand(parameters));
              break;
            case "-STABILIZE":
              commands.Add(new StabilizeCommand(parameters));
              break;
            case "-TRIM":
              commands.Add(new TrimCommand(parameters));
              break;
            default:
              throw new ArgumentException($"Invalid command '{commandText}'.");
          }

        }
        else
          throw new ArgumentException($"Expected a command, encountered '{commandText}'.");
      }

      return commands;
    }
  }

  public class AdjustSpeedCommand : IVAECommand
  {
    private float playbackRate, frameRate;

    public AdjustSpeedCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "FRAMERATE":
            playbackRate = float.Parse(kvp.Value);
            break;
          case "PLAYBACKRATE":
            frameRate = float.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().AdjustAudioOrVideoPlaybackSpeed(filePath, playbackRate, frameRate);
    }
  }

  public class AdjustVolumeCommand : IVAECommand
  {
    private string volume;

    public AdjustVolumeCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "VOLUME":
            volume = kvp.Value;
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().AdjustVolume(filePath, volume);
    }
  }

  public class CropCommand : IVAECommand
  {
    private int x, y, width, height;

    public CropCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "X":
            x = int.Parse(kvp.Value);
            break;
          case "Y":
            y = int.Parse(kvp.Value);
            break;
          case "WIDTH":
            width = int.Parse(kvp.Value);
            break;
          case "HEIGHT":
            height = int.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().CropImageOrVideo(filePath, x, y, width, height);
    }
  }

  public class ExtractAudioCommand : IVAECommand
  {
    public ExtractAudioCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters) { }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().ExtractAudioFromVideo(filePath);
    }
  }

  public class GifToVideoCommand : IVAECommand
  {
    public GifToVideoCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters) { }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().ConvertGifToVideo(filePath);
    }
  }

  public class NormalizeVolumeCommand : IVAECommand
  {
    public NormalizeVolumeCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters) { }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().NormalizeVolume(filePath);
    }
  }

  public class RemoveAudioCommand : IVAECommand
  {
    public RemoveAudioCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters) { }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().RemoveAudioFromVideo(filePath);
    }
  }

  public class ResizeCommand : IVAECommand
  {
    private int width, height;
    private float scale;

    public ResizeCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "SCALE":
            scale = float.Parse(kvp.Value);
            break;
          case "WIDTH":
            width = int.Parse(kvp.Value);
            break;
          case "HEIGHT":
            height = int.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().ResizeImageOrVideo(filePath, width, height, scale);
    }
  }

  public class ReverseCommand : IVAECommand
  {
    public ReverseCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters) { }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().Reverse(filePath);
    }
  }

  public class StabilizeCommand : IVAECommand
  {
    public StabilizeCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters) { }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().StabilizeVideo(filePath);
    }
  }

  public class TrimCommand : IVAECommand
  {
    string start, end;

    public TrimCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "START":
            start = kvp.Value;
            break;
          case "END":
            end = kvp.Value;
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().TrimAudioOrVideo(filePath, start, end);
    }
  }
}
