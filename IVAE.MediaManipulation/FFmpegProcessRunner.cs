using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public static class FFmpegProcessRunner
  {
    public static string Run(string arguments)
    {
      System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("ffmpeg.exe", arguments);
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;
      processStartInfo.CreateNoWindow = true;

      using (System.Diagnostics.Process process = new System.Diagnostics.Process())
      {
        process.StartInfo = processStartInfo;
        process.Start();
        string result = process.StandardOutput.ReadToEnd();

        return result;
      }
    }
  }
}
