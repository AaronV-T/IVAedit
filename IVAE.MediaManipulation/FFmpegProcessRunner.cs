﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace IVAE.MediaManipulation
{
  public class FFmpegProcessRunner
  {
    public event Action<double> OnDurationMessage, OnTimeMessage;

    public string Run(string arguments)
    {
      Log.Verbose($"ffmpeg.exe {arguments}");

      ProcessStartInfo processStartInfo = new ProcessStartInfo("ffmpeg.exe", arguments);
      processStartInfo.RedirectStandardError = true;
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;
      processStartInfo.CreateNoWindow = true;

      using (Process process = new Process())
      {
        process.StartInfo = processStartInfo;

        StringBuilder sb = new StringBuilder();
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (s, e) => { Log.Verbose($"o: {e.Data}"); sb.AppendLine(e.Data); DataReceivedFromProcess(e.Data); };
        process.ErrorDataReceived += (s, e) => { Log.Verbose($"e: {e.Data}"); sb.AppendLine(e.Data); DataReceivedFromProcess(e.Data); };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();

        return sb.ToString();
      }
    }

    private void DataReceivedFromProcess(string data)
    {
      if (data == null)
        return;

      if (data.IndexOf("Duration:") >= 0)
        OnDurationMessage?.Invoke(FFmpegTimeToMS(data.Substring(data.LastIndexOf("Duration:") + 9, 12).Trim()));
      if (data.IndexOf("time=") >= 0)
        OnTimeMessage?.Invoke(FFmpegTimeToMS(data.Substring(data.LastIndexOf("time=") + 5, 11)));
    }

    private double FFmpegTimeToMS(string timeString)
    {
      if (timeString[0] == '-')
        return -1;
      if (!System.Text.RegularExpressions.Regex.IsMatch(timeString, @"\d\d:\d\d:\d\d"))
        return -1;

      int hours = int.Parse(timeString.Substring(0, 2));
      int minutes = int.Parse(timeString.Substring(3, 2));
      float seconds = float.Parse(timeString.Substring(6));

      return (hours * 60 * 60 * 1000) + (minutes * 60 * 1000) + (seconds * 1000);
    }
  }
}
