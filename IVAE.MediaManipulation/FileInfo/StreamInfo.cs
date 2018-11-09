using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCalc;

namespace IVAE.MediaManipulation
{
  public abstract class StreamInfo
  {
    public int? Index;
    public string CodecName;
    public string CodecLongName;
    public string CodecType;
    public double? CodecTimeBase;
    public string CodecTagString;
    public string CodecTag;
    public double? RFrameRate;
    public double? AvgFrameRate;
    public double? TimeBase;
    public int? StartPts;
    public double? StartTime;
    public long? DurationTs;
    public double? Duration;
    public double? BitRate;
    public double? NbFrames;
    public Dictionary<string, object> Disposition;
    public Dictionary<string, object> Tags;

    public Dictionary<string, object> AdditionalAttributes;

    public StreamInfo(Dictionary<string, object> dict)
    {
      Dictionary<string, object> streamDictionary = new Dictionary<string, object>(dict);

      Index = streamDictionary.ContainsKey("index") ? Convert.ToInt32(streamDictionary.GetValueAndRemove("index")) : (int?)null;
      CodecName = streamDictionary.GetValueAndRemove("codec_name")?.ToString();
      CodecLongName = streamDictionary.GetValueAndRemove("codec_long_name")?.ToString();
      CodecType = streamDictionary.GetValueAndRemove("codec_type")?.ToString();
      CodecTimeBase = streamDictionary.ContainsKey("codec_time_base") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("codec_time_base").ToString()).Evaluate()) : (double?)null;
      CodecTagString = streamDictionary.GetValueAndRemove("codec_tag_string")?.ToString();
      CodecTag = streamDictionary.GetValueAndRemove("codec_tag")?.ToString();
      RFrameRate = streamDictionary.ContainsKey("r_frame_rate") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("r_frame_rate").ToString()).Evaluate()) : (double?)null;
      AvgFrameRate = streamDictionary.ContainsKey("avg_frame_rate") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("avg_frame_rate").ToString()).Evaluate()) : (double?)null;
      TimeBase = streamDictionary.ContainsKey("time_base") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("time_base").ToString()).Evaluate()) : (double?)null;
      StartPts = streamDictionary.ContainsKey("start_pts") ? Convert.ToInt32(streamDictionary.GetValueAndRemove("start_pts")) : (int?)null;
      StartTime = streamDictionary.ContainsKey("start_time") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("start_time").ToString()).Evaluate()) : (double?)null;
      DurationTs = streamDictionary.ContainsKey("duration_ts") ? Convert.ToInt64(streamDictionary.GetValueAndRemove("duration_ts")) : (long?)null;
      Duration = streamDictionary.ContainsKey("duration") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("duration").ToString()).Evaluate()) : (double?)null;
      BitRate = streamDictionary.ContainsKey("bit_rate") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("bit_rate").ToString()).Evaluate()) : (double?)null;
      NbFrames = streamDictionary.ContainsKey("nb_frames") ? Convert.ToDouble(new Expression(streamDictionary.GetValueAndRemove("nb_frames").ToString()).Evaluate()) : (double?)null;
      Disposition = streamDictionary.ContainsKey("disposition") ? ((Newtonsoft.Json.Linq.JObject)streamDictionary.GetValueAndRemove("disposition")).ToObject<Dictionary<string, object>>() : null;
      Tags = streamDictionary.ContainsKey("tags") ? ((Newtonsoft.Json.Linq.JObject)streamDictionary.GetValueAndRemove("tags")).ToObject<Dictionary<string, object>>() : null;

      AdditionalAttributes = streamDictionary;
    }
  }
}
