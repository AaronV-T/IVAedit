using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class FFProbeProcessRunner
  {
    public string Run(string arguments)
    {
      System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("ffprobe.exe", arguments);
      processStartInfo.RedirectStandardError = true;
      processStartInfo.RedirectStandardOutput = true;
      processStartInfo.UseShellExecute = false;
      processStartInfo.CreateNoWindow = true;

      using (System.Diagnostics.Process process = new System.Diagnostics.Process())
      {
        process.StartInfo = processStartInfo;

        StringBuilder sb = new StringBuilder();
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) sb.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) sb.AppendLine(e.Data); };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();

        return sb.ToString();
      }
    }
  }
}
