using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class FormatInfo
  {
    public string Filename;
    public int? NbStreams;
    public int? NbPrograms;
    public string FormatName;
    public string FormatLongName;
    public double? StartTime;
    public double? Duration;
    public long? Size;
    public long? BitRate;
    public int? ProbeScore;
    public Dictionary<string,object> Tags;
    public Dictionary<string, object> AdditionalAttributes;

    public FormatInfo(Dictionary<string,object> dict)
    {
      Dictionary<string, object> formatDictionary = new Dictionary<string, object>(dict);

      if (formatDictionary.ContainsKey("filename"))
        Filename = formatDictionary.GetValueAndRemove("filename")?.ToString();
      if (formatDictionary.ContainsKey("nb_streams"))
        NbStreams = Convert.ToInt32(formatDictionary.GetValueAndRemove("nb_streams"));
      if (formatDictionary.ContainsKey("nb_programs"))
        NbPrograms = Convert.ToInt32(formatDictionary.GetValueAndRemove("nb_programs"));
      if (formatDictionary.ContainsKey("format_name"))
        FormatName = formatDictionary.GetValueAndRemove("format_name")?.ToString();
      if (formatDictionary.ContainsKey("format_long_name"))
        FormatLongName = formatDictionary.GetValueAndRemove("format_long_name")?.ToString();
      if (formatDictionary.ContainsKey("start_time"))
        StartTime = Convert.ToDouble(formatDictionary.GetValueAndRemove("start_time"));
      if (formatDictionary.ContainsKey("duration"))
        Duration = Convert.ToDouble(formatDictionary.GetValueAndRemove("duration"));
      if (formatDictionary.ContainsKey("size"))
        Size = Convert.ToInt64(formatDictionary.GetValueAndRemove("size"));
      if (formatDictionary.ContainsKey("bit_rate"))
        BitRate = Convert.ToInt64(formatDictionary.GetValueAndRemove("bit_rate"));
      if (formatDictionary.ContainsKey("probe_score"))
        ProbeScore = Convert.ToInt32(formatDictionary.GetValueAndRemove("probe_score"));
      if (formatDictionary.ContainsKey("tags"))
        Tags = ((Newtonsoft.Json.Linq.JObject)formatDictionary.GetValueAndRemove("tags")).ToObject<Dictionary<string, object>>();

      AdditionalAttributes = formatDictionary;
    }
  }
}
