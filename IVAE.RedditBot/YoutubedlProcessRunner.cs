﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace IVAE.RedditBot
{
  public class YoutubedlProcessRunner
  {
    public List<string> Run(string arguments)
    {
      Log.Verbose($"youtube-dl.exe {arguments}");

      ProcessStartInfo processStartInfo = new ProcessStartInfo("youtube-dl.exe", arguments);
      //processStartInfo.RedirectStandardError = true;
      //processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;
      processStartInfo.CreateNoWindow = true;

      using (Process process = new Process())
      {
        process.StartInfo = processStartInfo;

        List<string> output = new List<string>();
        process.EnableRaisingEvents = true;
        //process.ErrorDataReceived += (s, e) => { Debug.WriteLine($"e: {e.Data}"); sb.AppendLine(e.Data); };
        //process.OutputDataReceived += (s, o) => { Debug.WriteLine($"o: {o.Data}"); sb.AppendLine(o.Data); };

        process.Start();
        //process.BeginErrorReadLine();
        //process.BeginOutputReadLine();
        process.WaitForExit();

        return output;
      }
    }
  }
}
