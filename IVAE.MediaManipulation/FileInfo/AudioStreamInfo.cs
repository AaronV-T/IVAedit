using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCalc;

namespace IVAE.MediaManipulation
{
  public class AudioStreamInfo : StreamInfo
  {
    public string SampleFmt;
    public double? SampleRate;
    public int? Channels;
    public string ChannelLayout;
    public int? BitsPerSample;

    public AudioStreamInfo(Dictionary<string,object> dict) : base(dict)
    {
      SampleFmt = AdditionalAttributes.GetValueAndRemove("sample_fmt")?.ToString();
      SampleRate = AdditionalAttributes.ContainsKey("sample_rate") ? Convert.ToDouble(new Expression(AdditionalAttributes.GetValueAndRemove("sample_rate").ToString()).Evaluate()) : (double?)null;
      Channels = AdditionalAttributes.ContainsKey("channels") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("channels")) : (int?)null;
      ChannelLayout = AdditionalAttributes.GetValueAndRemove("channel_layout")?.ToString();
      BitsPerSample = AdditionalAttributes.ContainsKey("bits_per_sample") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("bits_per_sample")) : (int?)null;
    }
  }
}
