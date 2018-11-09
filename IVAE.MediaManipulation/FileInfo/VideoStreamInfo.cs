using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class VideoStreamInfo : StreamInfo
  {
    public string Profile;
    public int? Width;
    public int? Height;
    public int? CodedWidth;
    public int? CodedHeight;
    public bool? HasBFrames;
    public string SampleAspectRatio;
    public string DisplayAspectRatio;
    public string PixFmt;
    public int? Level;
    public int? Refs;

    public VideoStreamInfo(Dictionary<string,object> dict) : base(dict)
    {
      Profile = AdditionalAttributes.GetValueAndRemove("profile")?.ToString();
      Width = AdditionalAttributes.ContainsKey("width") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("width")) : (int?)null;
      Height = AdditionalAttributes.ContainsKey("height") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("height")) : (int?)null;
      CodedWidth = AdditionalAttributes.ContainsKey("coded_width") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("coded_width")) : (int?)null;
      CodedHeight = AdditionalAttributes.ContainsKey("coded_height") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("coded_height")) : (int?)null;
      HasBFrames = AdditionalAttributes.ContainsKey("has_b_frames") ? Convert.ToBoolean(AdditionalAttributes.GetValueAndRemove("has_b_frames")) : (bool?)null;
      SampleAspectRatio = AdditionalAttributes.GetValueAndRemove("sample_aspect_ratio")?.ToString();
      DisplayAspectRatio = AdditionalAttributes.GetValueAndRemove("display_aspect_ratio")?.ToString();
      PixFmt = AdditionalAttributes.GetValueAndRemove("pix_fmt")?.ToString();
      Level = AdditionalAttributes.ContainsKey("level") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("level")) : (int?)null;
      Refs = AdditionalAttributes.ContainsKey("refs") ? Convert.ToInt32(AdditionalAttributes.GetValueAndRemove("refs")) : (int?)null;
    }
  }
}
