using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAeditGUI
{
  public static class SettingsIO
  {
    const string settingsFilePath = "settings.dat";

    public static Dictionary<string, string> LoadSettings(List<string> settingsNames)
    {
      Dictionary<string, string> settings = new Dictionary<string, string>();

      if (!System.IO.File.Exists(settingsFilePath))
        return settings;

      using (System.IO.StreamReader sr = new System.IO.StreamReader(settingsFilePath))
      {
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          string[] splitLine = line.Split();

          if (splitLine.Length < 2)
            continue;

          if (settingsNames.Contains(splitLine[0]))
            settings.Add(splitLine[0], line.Substring(splitLine[0].Length + 1));
        }
      }

      return settings;
    }

    public static void SaveSettings(Dictionary<string,string> settings)
    {
      List<string> lines = new List<string>(); ;

      // Update settings that are already in the settings file.
      if (System.IO.File.Exists(settingsFilePath))
      {
        lines = System.IO.File.ReadAllLines(settingsFilePath).ToList();

        for (int i = 0; i < lines.Count; i++)
        {
          string[] splitLine = lines[i].Split();

          if (splitLine.Length < 2)
            continue;

          string settingName = splitLine[0];
          if (settings.ContainsKey(settingName))
          {
            lines[i] = $"{settingName} {settings[settingName]}";
            settings.Remove(settingName);
          }
        }
      }

      // Add any settings that aren't already in the settings file.
      foreach (var kvp in settings)
        lines.Add($"{kvp.Key} {kvp.Value}");

      System.IO.File.WriteAllLines(settingsFilePath, lines.ToArray());
    }
  }
}
