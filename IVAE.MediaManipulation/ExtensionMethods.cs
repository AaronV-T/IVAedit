using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public static class ExtensionMethods
  {
    public static object GetValueAndRemove(this Dictionary<string, object> dict, string key)
    {
      if (dict.ContainsKey(key))
      {
        object value = dict[key];
        dict.Remove(key);
        return value;
      }

      return null;
    }

    public static void SaveAndInferFormat(this System.Drawing.Image image, string outputPath)
    {
      System.Drawing.Imaging.ImageFormat imageFormat;

      string imageExtension = System.IO.Path.GetExtension(outputPath).ToLower();
      switch (imageExtension)
      {
        case ".bmp":
          imageFormat = System.Drawing.Imaging.ImageFormat.Bmp;
          break;
        case ".jpeg":
        case ".jpg":
          imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
          break;
        case ".png":
          imageFormat = System.Drawing.Imaging.ImageFormat.Png;
          break;
        case ".tif":
        case ".tiff":
          imageFormat = System.Drawing.Imaging.ImageFormat.Tiff;
          break;
        default:
          throw new NotImplementedException($"Unimplement image extension '{imageExtension}'.");
      }

      image.Save(outputPath, imageFormat);
    }
  }
}
