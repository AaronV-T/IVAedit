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
      splitText.RemoveAt(0);

      while(splitText.Count > 0)
      {
        switch(splitText[0].ToUpper())
        {
          case "-CROP":
            splitText.RemoveAt(0);
            int count = 0;
            while (splitText.Count > count && splitText[count][0] != '-')
              count++;
            
            commands.Add(new CropCommand(splitText.GetRange(0, count)));
            splitText.RemoveRange(0, count);
            break;
          default:
            throw new ArgumentException($"Invalid command '{splitText[0]}'.");
        }
      }

      return commands;
    }
  }

  public class CropCommand : IVAECommand
  {
    private int x, y, width, height;

    public CropCommand(List<string> commandParameters)
    {
      foreach (string param in commandParameters)
      {
        List<string> splitParam = param.Split('=').ToList();
        switch (splitParam[0].ToUpper())
        {
          case "X":
            x = int.Parse(splitParam[1]);
            break;
          case "Y":
            y = int.Parse(splitParam[1]);
            break;
          case "WIDTH":
            width = int.Parse(splitParam[1]);
            break;
          case "HEIGHT":
            height = int.Parse(splitParam[1]);
            break;
        }
      }
    }

    public string Execute(string filePath)
    {
      return new MediaManipulation.TaskHandler().CropImageOrVideo(filePath, x, y, width, height);
    }
  }
}
