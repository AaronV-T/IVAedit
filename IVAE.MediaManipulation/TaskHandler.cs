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

    public string AdjustVolume(string filePath, string volume)
    {
      OnChangeStep?.Invoke("Adjusting Volume");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_AdjustedVolume{DateTime.Now.ToString("yyyMMdd_HHmmss")}{System.IO.Path.GetExtension(filePath)}";

      AudioManipulator audioManipulator = new AudioManipulator();
      audioManipulator.OnProgress += ProgressUpdate;
      audioManipulator.AdjustVolume(outputPath, filePath, volume);
      audioManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string AlignImage(string imageToAlignPath, string referenceImagePath, ImageAlignmentType imageAlignmentType)
    {
      ImageManipulator imageManipulator = new ImageManipulator();
      imageManipulator.OnProgress += ProgressUpdate;

      using (System.Drawing.Bitmap imageToAlign = new System.Drawing.Bitmap(imageToAlignPath))
      using (System.Drawing.Bitmap referenceImage = new System.Drawing.Bitmap(referenceImagePath))
      using (System.Drawing.Bitmap warpedImage = imageManipulator.GetAlignedImage(imageToAlign, referenceImage, imageAlignmentType))
      {
        string outputPath = $@"{System.IO.Path.GetDirectoryName(imageToAlignPath)}\{System.IO.Path.GetFileNameWithoutExtension(imageToAlignPath)}_Aligned{DateTime.Now.ToString("yyyMMdd_HHmmss")}{System.IO.Path.GetExtension(imageToAlignPath)}";
        warpedImage.Save(outputPath);

        imageManipulator.OnProgress -= ProgressUpdate;
        return outputPath;
      }
    }

    public string CombineGifs(string[] fileNames, int gifsPerLine)
    {
      OnChangeStep?.Invoke("Combining GIFs.");

      string newGifPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Combined{DateTime.Now.ToString("yyyMMdd_HHmmss")}.gif";

      ImageManipulator imageManipulator = new ImageManipulator();
      imageManipulator.OnProgress += ProgressUpdate;
      imageManipulator.CombineGifs(newGifPath, fileNames.ToList(), gifsPerLine);
      imageManipulator.OnProgress -= ProgressUpdate;

      return newGifPath;
    }

    public string ConvertImagesToGif(string[] fileNames, int x, int y, int width, int height, int frameDelay, int finalDelay, int loops, int fontSize, bool writeFileNames, bool alignImages, ImageAlignmentType imageAlignmentType)
    {
      List<System.Drawing.Bitmap> sourceImages = new List<System.Drawing.Bitmap>();
      //System.Drawing.Bitmap tempImage = null;
      try
      {
        ImageManipulator imageManipulator = new ImageManipulator();

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

                System.Drawing.Bitmap croppedImage = imageManipulator.GetCroppedImage(sourceImages[i], x, y, width, height);
                Console.WriteLine($"GetCroppedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");
                sourceImages[i].Dispose();
                sourceImages[i] = croppedImage;
              }
            }

            // If image aligning enabled: ...
            if (alignImages && i > 0)
            {
              DateTime start = DateTime.Now;
              System.Drawing.Bitmap alignedImage = imageManipulator.GetAlignedImage(sourceImages[i], sourceImages[i - 1], imageAlignmentType);
              Console.WriteLine($"GetAlignedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");
              sourceImages[i].Dispose();
              sourceImages[i] = alignedImage;
            }

            // If writing text enabled: write the file name onto the image.
            if (writeFileNames)
            {
              // Create an image with the drawn text.
              DateTime start = DateTime.Now;
              System.Drawing.Bitmap editedImage = imageManipulator.GetImageWithDrawnText(sourceImages[i], System.IO.Path.GetFileNameWithoutExtension(fileNames[i]), fontSize);
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
        imageManipulator.OnProgress += ProgressUpdate;
        imageManipulator.MakeGifFromImages(gifPath, sourceImages, frameDelay, finalDelay, loops);
        imageManipulator.OnProgress -= ProgressUpdate;

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

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.MakeGifvFromGif(outputPath, gifFilePath);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string CropImageOrVideo(string filePath, int x, int y, int width, int height)
    {
      OnChangeStep?.Invoke("Cropping");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Cropped{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{System.IO.Path.GetExtension(filePath)}";

      MediaType mediaType = MediaTypeHelper.GetMediaTypeFromFileName(filePath);
      if (mediaType == MediaType.IMAGE)
      {
        ImageManipulator imageManipulator = new ImageManipulator();
        imageManipulator.OnProgress += ProgressUpdate;
        imageManipulator.CropImage(outputPath, filePath, x, y, width, height);
        imageManipulator.OnProgress -= ProgressUpdate;
      }
      else if (mediaType == MediaType.VIDEO)
      {
        VideoManipulator videoManipulator = new VideoManipulator();
        videoManipulator.OnProgress += ProgressUpdate;
        videoManipulator.CropVideo(outputPath, filePath, x, y, width, height);
        videoManipulator.OnProgress -= ProgressUpdate;
      }
      else
        throw new NotImplementedException($"Unsupported file extension '{System.IO.Path.GetExtension(filePath)}'.");

      return outputPath;
    }

    public string ConvertVideoToImages(string videoFilePath, string fps)
    {
      OnChangeStep?.Invoke("Converting Video to Images");

      string outputDirectory = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Images{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.MakeImagesFromVideo(outputDirectory, videoFilePath, fps);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputDirectory;
    }

    public string DrawMatches(string image1Path, string image2Path, ImageAlignmentType imageAlignmentType)
    {
      ImageManipulator imageManipulator = new ImageManipulator();
      imageManipulator.OnProgress += ProgressUpdate;

      MatchingTechnique mt;
      if (imageAlignmentType != ImageAlignmentType.FULLWARP)
        mt = MatchingTechnique.FAST;
      else
        mt = MatchingTechnique.ORB;

      using (System.Drawing.Bitmap image1 = new System.Drawing.Bitmap(image1Path))
      using (System.Drawing.Bitmap image2 = new System.Drawing.Bitmap(image2Path))
      using (System.Drawing.Bitmap matchesImage = imageManipulator.GetImageWithDrawnMatches(image1, image2, mt))
      {
        string outputPath = $@"{System.IO.Path.GetDirectoryName(image1Path)}\Matches{DateTime.Now.ToString("yyyMMdd_HHmmss")}.jpg";
        matchesImage.Save(outputPath);
        imageManipulator.OnProgress -= ProgressUpdate;
        return outputPath;
      }
    }

    public string ExtractAudioFromVideo(string videoFilePath)
    {
      OnChangeStep?.Invoke("Extracting Audio");

      string outputPathWithoutExtension = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Extracted{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      string outputPath = videoManipulator.ExtractAudioFromVideo(outputPathWithoutExtension, videoFilePath);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string NormalizeVolume(string filePath)
    {
      OnChangeStep?.Invoke("Normalizing Audio");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Normalized{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{System.IO.Path.GetExtension(filePath)}";

      AudioManipulator audioManipulator = new AudioManipulator();
      audioManipulator.OnProgress += ProgressUpdate;
      audioManipulator.NormalizeVolume(outputPath, filePath);
      audioManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string StabilizeVideo(string videoFilePath)
    {
      OnChangeStep?.Invoke("Stabilizing Video");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Stabilized{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{System.IO.Path.GetExtension(videoFilePath)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.StabilizeVideo(outputPath, videoFilePath);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string StitchImages(string[] fileNames)
    {
      List<System.Drawing.Bitmap> bitmaps = new List<System.Drawing.Bitmap>();
      try
      {
        foreach (string file in fileNames)
          bitmaps.Add(new System.Drawing.Bitmap(file));

        ImageManipulator imageManipulator = new ImageManipulator();
        imageManipulator.OnProgress += ProgressUpdate;

        using (System.Drawing.Bitmap stitchedImage = imageManipulator.GetStitchedImage(bitmaps))
        {
          string outputPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Stitched{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{System.IO.Path.GetExtension(fileNames[0])}";
          stitchedImage.Save(outputPath);

          imageManipulator.OnProgress -= ProgressUpdate;
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
      OnChangeStep?.Invoke("Making GIF");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Giffed{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.gif";

      List<System.Drawing.Bitmap> bitmaps = new List<System.Drawing.Bitmap>();
      try
      {
        ImageManipulator imageManipulator = new ImageManipulator();

        for (int i = 0; i < fileNames.Length; i++)
        {
          if (i == 0)
          {
            bitmaps.Add(new System.Drawing.Bitmap(fileNames[i]));
            continue;
          }

          using (System.Drawing.Bitmap currentBmp = new System.Drawing.Bitmap(fileNames[i]))
          {
            Console.Write($"{i}: ");
            bitmaps.Add(imageManipulator.GetCombinedImage(bitmaps[i - 1], currentBmp));
          }

          //bitmaps[i].Save($@"{System.IO.Path.GetDirectoryName(fileNames[0])}\bmp{i}.png");
        }

        for (int i = fileNames.Length - 2; i >= 0; i--)
        {
          System.Drawing.Bitmap mappedBmp = imageManipulator.GetAlignedImage(bitmaps[i], bitmaps[i + 1], ImageAlignmentType.MAP);
          bitmaps[i].Dispose();
          bitmaps[i] = mappedBmp;
        }

        imageManipulator.OnProgress += ProgressUpdate;
        imageManipulator.MakeGifFromImages(outputPath, bitmaps, 5, 100, 0);
        imageManipulator.OnProgress -= ProgressUpdate;
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        foreach (var bmp in bitmaps)
          bmp?.Dispose();
      }

      return outputPath;
    }

    public string TrimAudioOrVideo(string filePath, string startTime, string endTime)
    {
      OnChangeStep?.Invoke("Trimming File");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Trimmed{DateTime.Now.ToString("yyyyMMdd_HHmmss")}{System.IO.Path.GetExtension(filePath)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.Trim(outputPath, filePath, startTime, endTime);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    private void ProgressUpdate(float percent)
    {
      OnProgressUpdate?.Invoke(percent);
    }
  }
}
