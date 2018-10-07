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

    public string AlignImage(string imageToAlignPath, string referenceImagePath)
    {
      using (System.Drawing.Bitmap imageToAlign = new System.Drawing.Bitmap(imageToAlignPath))
      using (System.Drawing.Bitmap referenceImage = new System.Drawing.Bitmap(referenceImagePath))
      using (System.Drawing.Bitmap warpedImage = ImageFeatureDetector.GetAlignedImage(imageToAlign, referenceImage))
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

    public string ConvertImagesToGif(string[] fileNames, int x, int y, int width, int height, int frameDelay, int finalDelay, int loops, int fontSize, bool writeFileNames, bool alignImages)
    {
      List<System.Drawing.Bitmap> sourceImages = new List<System.Drawing.Bitmap>();
      System.Drawing.Bitmap tempImage = null;
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
              // If aligning enabled: get the x/y offsets between the previous image and current image.
              if (alignImages && i > 0)
              {
                DateTime start1 = DateTime.Now;
                Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(tempImage, sourceImages[i]);
                Console.WriteLine($"GetXYOffsets took {Math.Round((DateTime.Now - start1).TotalMilliseconds)}ms. Offsets{i + 1}: {offsets.Item1},{offsets.Item2}.");
                x -= offsets.Item1;
                y -= offsets.Item2;
              }

              // Dispose the previous raw image.
              if (tempImage != null)
                tempImage.Dispose();

              // Create a cropped image.
              DateTime start = DateTime.Now;
              System.Drawing.Bitmap croppedImage = ImageManipulator.GetCroppedImage(sourceImages[i], x, y, width, height);
              Console.WriteLine($"GetCroppedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");

              // Store the current raw image.
              tempImage = sourceImages[i];

              // Store the current cropped image.
              sourceImages[i] = croppedImage;
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
        tempImage.Dispose();
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

    public object Test(string[] fileNames)
    {
      using (System.Drawing.Bitmap referenceImage = new System.Drawing.Bitmap(fileNames[0]))
      using (System.Drawing.Bitmap imageToAlign = new System.Drawing.Bitmap(fileNames[1]))
      using (System.Drawing.Bitmap warpedImage = ImageFeatureDetector.GetAlignedImage(imageToAlign, referenceImage))
      {
        warpedImage.Save($@"{System.IO.Path.GetDirectoryName(fileNames[1])}\Warped{DateTime.Now.ToString("yyyMMdd_HHmmss")}{System.IO.Path.GetExtension(fileNames[1])}");
      }
        
      return null;
    }

    private void ProgressUpdate(float percent)
    {
      OnProgressUpdate?.Invoke(percent);
    }
  }
}
