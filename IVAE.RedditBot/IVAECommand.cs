﻿using System;
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

      List<string> splitText = text.Split(new string[] { " ", "\n" }, StringSplitOptions.RemoveEmptyEntries ).ToList();

      if (splitText.Count == 0)
        return null;

      splitText.RemoveAt(0); // Remove bot username mention.

      while (splitText.Count > 0 && splitText[0][0] == '!')
        splitText.RemoveAt(0); // Remove non media file commands.

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
              if (splitParamString.Count > 2 || string.IsNullOrEmpty(splitParamString[0]))
                throw new ArgumentException($"Invalid parameter '{paramString}'.");

              string value = null;
              if (splitParamString.Count == 2)
                value = splitParamString[1];

              parameters.Add(splitParamString[0], value);
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
            case "-EXTEND":
              commands.Add(new ExtendVideoCommand(parameters));
              break;
            case "-EXTRACTAUDIO":
              commands.Add(new ExtractAudioCommand(parameters));
              break;
            case "-FLIP":
              commands.Add(new FlipCommand(parameters));
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
            case "-ROTATE":
              commands.Add(new RotateCommand(parameters));
              break;
            case "-SCREENSHOT":
              commands.Add(new ScreenshotCommand(parameters));
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
    public float PlaybackRate { get; private set; }
    public float FrameRate { get; private set; }

    public AdjustSpeedCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "FRAMERATE":
            FrameRate = float.Parse(kvp.Value);
            break;
          case "SPEED":
            PlaybackRate = float.Parse(kvp.Value);
            if (PlaybackRate < 0.125 || PlaybackRate > 8)
              throw new ArgumentException("Speed not a valid value.");

            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().AdjustAudioOrVideoPlaybackSpeed(filePath, PlaybackRate, FrameRate);
    }
  }

  public class AdjustVolumeCommand : IVAECommand
  {
    public string Volume { get; private set; }

    public AdjustVolumeCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "VOLUME":
            Volume = kvp.Value;
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().AdjustVolume(filePath, Volume);
    }
  }

  public class CropCommand : IVAECommand
  {
    private double x, y, width, height;

    public CropCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "X":
            x = double.Parse(kvp.Value);
            break;
          case "Y":
            y = double.Parse(kvp.Value);
            break;
          case "WIDTH":
            width = double.Parse(kvp.Value);
            break;
          case "HEIGHT":
            height = double.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().CropImageOrVideo(filePath, x, y, width, height);
    }
  }

  public class ExtendVideoCommand : IVAECommand
  {
    private double seconds;

    public ExtendVideoCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "SECONDS":
            seconds = double.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().ExtendLastFrameOfVideo(filePath, seconds);
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

  public class FlipCommand : IVAECommand
  {
    private bool horizontal, vertical;

    public FlipCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "HORIZONTAL":
            horizontal = string.IsNullOrEmpty(kvp.Value) ? true : bool.Parse(kvp.Value);
            break;
          case "VERTICAL":
            vertical = string.IsNullOrEmpty(kvp.Value) ? true : bool.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().FlipImageOrVideo(filePath, horizontal, vertical);
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

  public class RotateCommand : IVAECommand
  {
    private bool counterClockwise;

    public RotateCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "COUNTERCLOCKWISE":
            counterClockwise = string.IsNullOrEmpty(kvp.Value) ? true : bool.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().RotateImageOrVideo(filePath, counterClockwise);
    }
  }

  public class ScreenshotCommand : IVAECommand
  {
    bool end;
    string time;

    public ScreenshotCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "END":
            end = string.IsNullOrEmpty(kvp.Value) ? true : bool.Parse(kvp.Value);
            break;
          case "TIME":
            time = kvp.Value;
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().GetScreenshotFromVideo(filePath, time, end);
    }
  }

  public class StabilizeCommand : IVAECommand
  {
    private int optzoom;

    public StabilizeCommand(Dictionary<string, string> parameters)
    {
      foreach (var kvp in parameters)
      {
        switch (kvp.Key.ToUpper())
        {
          case "OPTZOOM":
            optzoom = int.Parse(kvp.Value);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().StabilizeVideo(filePath, optzoom);
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
