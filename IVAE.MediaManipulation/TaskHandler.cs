using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class TaskHandler
  {
    public event Action<string> OnChangeStep;
    public event Action<float> OnProgressUpdate;

    public string AlignImage(string imageToAlignPath, string referenceImagePath, ImageAlignmentType imageAlignmentType)
    {
      using (System.Drawing.Bitmap imageToAlign = new System.Drawing.Bitmap(imageToAlignPath))
      using (System.Drawing.Bitmap referenceImage = new System.Drawing.Bitmap(referenceImagePath))
      using (System.Drawing.Bitmap warpedImage = ImageManipulator.GetAlignedImage(imageToAlign, referenceImage, imageAlignmentType))
      {
        string outputPath = $@"{System.IO.Path.GetDirectoryName(imageToAlignPath)}\{System.IO.Path.GetFileNameWithoutExtension(imageToAlignPath)}_Aligned{DateTime.Now.ToString("yyyMMdd_HHmmss")}{System.IO.Path.GetExtension(imageToAlignPath)}";
        warpedImage.Save(outputPath);
        return outputPath;
      }
    }

    public string CombineGifs(string[] fileNames, int gifsPerLine)
    {
      string newGifPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Combined{DateTime.Now.ToString("yyyMMdd_HHmmss")}.gif";

      OnChangeStep?.Invoke("Combining GIFs.");
      ImageManipulator.OnProgress += ProgressUpdate;
      ImageManipulator.CombineGifs(newGifPath, fileNames.ToList(), gifsPerLine);
      ImageManipulator.OnProgress -= ProgressUpdate;

      return newGifPath;
    }

    public string ConvertImagesToGif(string[] fileNames, int x, int y, int width, int height, int frameDelay, int finalDelay, int loops, int fontSize, bool writeFileNames, bool alignImages, ImageAlignmentType imageAlignmentType)
    {
      List<System.Drawing.Bitmap> sourceImages = new List<System.Drawing.Bitmap>();
      //System.Drawing.Bitmap tempImage = null;
      try
      {
        if (alignImages || writeFileNames || width != 0 || height != 0)
        {
          OnChangeStep?.Invoke("Editing Images");

          for (int i = 0; i < fileNames.Length; i++)
          {
            ProgressUpdate(i / (float)fileNames.Length);

            sourceImages.Add(new System.Drawing.Bitmap(fileNames[i]));

            // If cropping enabled: ...
            if (width != 0 || height != 0)
            {
              if (!alignImages || imageAlignmentType != ImageAlignmentType.CROP || i == 0)
              {
                DateTime start = DateTime.Now;
                System.Drawing.Bitmap croppedImage = ImageManipulator.GetCroppedImage(sourceImages[i], x, y, width, height);
                Console.WriteLine($"GetCroppedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");
                sourceImages[i].Dispose();
                sourceImages[i] = croppedImage;
              }
            }

            // If image aligning enabled: ...
            if (alignImages && i > 0)
            {
              DateTime start = DateTime.Now;
              System.Drawing.Bitmap alignedImage = ImageManipulator.GetAlignedImage(sourceImages[i], sourceImages[i - 1], imageAlignmentType);
              Console.WriteLine($"GetAlignedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");
              sourceImages[i].Dispose();
              sourceImages[i] = alignedImage;
            }

            // If writing text enabled: write the file name onto the image.
            if (writeFileNames)
            {
              // Create an image with the drawn text.
              DateTime start = DateTime.Now;
              System.Drawing.Bitmap editedImage = ImageManipulator.GetImageWithDrawnText(sourceImages[i], System.IO.Path.GetFileNameWithoutExtension(fileNames[i]), fontSize);
              Console.WriteLine($"GetImageWithDrawnText took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");

              // Dispose the current raw image and store the image with drawn text.
              sourceImages[i].Dispose();
              sourceImages[i] = editedImage;
            }
          }
        }

        // Add the images to a gif.
        OnChangeStep?.Invoke("Adding Images to GIF");
        string gifPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Converted{DateTime.Now.ToString("yyyMMdd_HHmmss")}.gif";
        ImageManipulator.OnProgress += ProgressUpdate;
        ImageManipulator.MakeGifFromImages(gifPath, sourceImages, frameDelay, finalDelay, loops);
        ImageManipulator.OnProgress -= ProgressUpdate;

        return gifPath;
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        foreach (System.Drawing.Bitmap bmp in sourceImages)
          bmp.Dispose();
      }
    }

    public string ConvertGifToGifv(string gifFilePath)
    {
      OnChangeStep?.Invoke("Converting GIF to GIFV");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(gifFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(gifFilePath)}.gifv";
      ImageManipulator.OnProgress += ProgressUpdate;
      ImageManipulator.MakeGifvFromGif(outputPath, gifFilePath);
      ImageManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string StitchImages(string[] fileNames)
    {
      List<System.Drawing.Bitmap> bitmaps = new List<System.Drawing.Bitmap>();
      try
      {
        foreach (string file in fileNames)
          bitmaps.Add(new System.Drawing.Bitmap(file));

        using (System.Drawing.Bitmap stitchedImage = ImageManipulator.GetStitchedImage(bitmaps))
        {
          string outputPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Stitched{DateTime.Now.ToString("yyyMMdd_HHmmss")}{System.IO.Path.GetExtension(fileNames[0])}";
          stitchedImage.Save(outputPath);
          return outputPath;
        }
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        foreach (System.Drawing.Bitmap bmp in bitmaps)
          bmp.Dispose();
      }
    }

    public object Test(string[] fileNames)
    {
      foreach (string file in fileNames)
        VideoManipulator.Test(file);

      return null;
    }

    private void ProgressUpdate(float percent)
    {
      OnProgressUpdate?.Invoke(percent);
    }
  }
}
