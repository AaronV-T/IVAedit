using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.MediaManipulation
{
  public class TaskHandler
  {
    public event Action<string> OnChangeStep;
    public event Action<float> OnProgressUpdate;

    /// <summary>
    /// Adds audio from a file to another file.
    /// </summary>
    /// <param name="filePath">The absolute path to the media file.</param>
    /// <param name="audioFilePath">The absolute path to the audio file with audio to add to the other file.</param>
    /// <returns>Absolute path to the media file with the added audio.</returns>
    public string AddAudio(string filePath, string audioFilePath)
    {
      OnChangeStep?.Invoke("Adding Audio");

      string outputPath = $@"{Path.GetDirectoryName(filePath)}\{Path.GetFileNameWithoutExtension(filePath)}_AudioAdded{GetCurrentTimeShort()}{Path.GetExtension(filePath)}";

      AudioManipulator audioManipulator = new AudioManipulator();
      audioManipulator.OnProgress += ProgressUpdate;
      audioManipulator.AddAudio(outputPath, filePath, audioFilePath);
      audioManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    /// <summary>
    /// Adjusts the playback rate of a given audio or video file.
    /// </summary>
    /// <param name="filePath">The absolute path to the media file.</param>
    /// <param name="newPlaybackRate"></param>
    /// <param name="newFrameRate">If file is a video and this parameter is greater than 0 then the framerate with be set to this value.</param>
    /// <returns>Absolute path to the media file with the adjusted playback rate.</returns>
    public string AdjustAudioOrVideoPlaybackSpeed(string filePath, float newPlaybackRate, float newFrameRate = 0)
    {
      OnChangeStep?.Invoke("Changing Playback Rate");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_SpeedAdjusted{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      MediaType mediaType = MediaTypeHelper.GetMediaTypeFromFileName(filePath);
      if (mediaType == MediaType.AUDIO)
      {
        AudioManipulator audioManipulator = new AudioManipulator();
        audioManipulator.OnProgress += ProgressUpdate;
        audioManipulator.AdjustAudioSpeed(outputPath, filePath, newPlaybackRate);
        audioManipulator.OnProgress -= ProgressUpdate;
      }
      else if (mediaType == MediaType.VIDEO)
      {
        VideoManipulator videoManipulator = new VideoManipulator();
        videoManipulator.OnProgress += ProgressUpdate;
        videoManipulator.AdjustVideoSpeed(outputPath, filePath, newPlaybackRate, newFrameRate, true);
        videoManipulator.OnProgress -= ProgressUpdate;
      }
      else
        throw new NotImplementedException($"Unsupported file extension '{System.IO.Path.GetExtension(filePath)}'.");

      return outputPath;
    }

    public string AdjustVolume(string filePath, string volume)
    {
      OnChangeStep?.Invoke("Adjusting Volume");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_VolumeAdjusted{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

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
        string outputPath = $@"{System.IO.Path.GetDirectoryName(imageToAlignPath)}\{System.IO.Path.GetFileNameWithoutExtension(imageToAlignPath)}_Aligned{GetCurrentTimeShort()}{System.IO.Path.GetExtension(imageToAlignPath)}";
        warpedImage.Save(outputPath);

        imageManipulator.OnProgress -= ProgressUpdate;
        return outputPath;
      }
    }

    public string CombineGifs(string[] fileNames, int gifsPerLine)
    {
      OnChangeStep?.Invoke("Combining GIFs.");

      string newGifPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Combined{GetCurrentTimeShort()}.gif";

      ImageManipulator imageManipulator = new ImageManipulator();
      imageManipulator.OnProgress += ProgressUpdate;
      imageManipulator.CombineGifs(newGifPath, fileNames.ToList(), gifsPerLine);
      imageManipulator.OnProgress -= ProgressUpdate;

      return newGifPath;
    }

    public string CombineVideos(string videoPath1, string videoPath2, bool combineHorizontally)
    {
      OnChangeStep?.Invoke("Combining Videos");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoPath1)}\{System.IO.Path.GetFileNameWithoutExtension(videoPath1)}_Combined{GetCurrentTimeShort()}{System.IO.Path.GetExtension(videoPath1)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.CombineVideos(outputPath, videoPath1, videoPath2, combineHorizontally);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string ConvertImagesToGif(string[] fileNames, int x, int y, int width, int height, int frameDelay, int finalDelay, int loops, int fontSize, bool writeFileNames, bool alignImages, ImageAlignmentType? imageAlignmentType)
    {
      List<int> animationDelays = new List<int>();

      for (int i = 0; i < fileNames.Length - 1; i++)
        animationDelays.Add(frameDelay);

      if (finalDelay > 0)
        animationDelays.Add(finalDelay);
      else
        animationDelays.Add(frameDelay);

      return ConvertImagesToGif(fileNames, x, y, width, height, animationDelays, loops, fontSize, writeFileNames, alignImages, imageAlignmentType);
    }

    public string ConvertImagesToGif(string[] fileNames, int x, int y, int width, int height, List<int> animationDelays, int loops, int fontSize, bool writeFileNames, bool alignImages, ImageAlignmentType? imageAlignmentType)
    {
      List<System.Drawing.Bitmap> sourceImages = new List<System.Drawing.Bitmap>();
      try
      {
        for (int i = 0; i < fileNames.Length; i++)
        {
          sourceImages.Add(new System.Drawing.Bitmap(fileNames[i]));
        }
        
        ImageManipulator imageManipulator = new ImageManipulator();

        if (alignImages || writeFileNames || width != 0 || height != 0)
        {
          OnChangeStep?.Invoke("Editing Images Step 1");

          for (int i = 0; i < sourceImages.Count; i++)
          {
            ProgressUpdate(i / (float)sourceImages.Count);

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

            if (alignImages && imageAlignmentType == ImageAlignmentType.MAP && i > 0)
            {
              DateTime start = DateTime.Now;
              System.Drawing.Bitmap combinedImage = imageManipulator.GetCombinedImage(sourceImages[i - 1], sourceImages[i], false);
              Console.WriteLine($"GetCombinedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");
              sourceImages[i].Dispose();
              sourceImages[i] = combinedImage;
            }

            //sourceImages[i].Save($@"{System.IO.Path.GetDirectoryName(fileNames[i])}\edited{System.IO.Path.GetFileNameWithoutExtension(fileNames[i])}{System.IO.Path.GetExtension(fileNames[i])}");
          }

          // Loop defaults to forward but can go backward if using map image alignment.
          int index = 0;
          Func<bool> loopCondition = () => index < sourceImages.Count;
          Action loopAction = () => index++;
          if (alignImages && imageAlignmentType == ImageAlignmentType.MAP)
          {
            index = sourceImages.Count - 1;
            loopCondition = () => index >= 0;
            loopAction = () => index--;
          }

          OnChangeStep?.Invoke("Editing Images Step 2");
          for (; loopCondition(); loopAction())
          {
            ProgressUpdate(index / (float)fileNames.Length);
            Console.WriteLine(index);
            // If image aligning enabled: ...
            if (alignImages && ((imageAlignmentType != ImageAlignmentType.MAP && index > 0) || (imageAlignmentType == ImageAlignmentType.MAP && index < sourceImages.Count - 1)))
            {
              int referenceIndex;
              if (imageAlignmentType != ImageAlignmentType.MAP)
                referenceIndex = index - 1;
              else
                referenceIndex = index + 1;

              DateTime start = DateTime.Now;
              System.Drawing.Bitmap alignedImage = imageManipulator.GetAlignedImage(sourceImages[index], sourceImages[referenceIndex], imageAlignmentType.Value);
              Console.WriteLine($"GetAlignedImage took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");
              sourceImages[index].Dispose();
              sourceImages[index] = alignedImage;
            }

            // If writing text enabled: write the file name onto the image.
            if (writeFileNames)
            {
              // Create an image with the drawn text.
              DateTime start = DateTime.Now;
              string fileName = System.IO.Path.GetFileNameWithoutExtension(fileNames[index]).Trim();
              if (fileName.IndexOf("!end!") > 0)
                fileName = fileName.Substring(0, fileName.IndexOf("!end!"));
              System.Drawing.Bitmap editedImage = imageManipulator.GetImageWithDrawnText(sourceImages[index], fileName, fontSize);
              Console.WriteLine($"GetImageWithDrawnText took {Math.Round((DateTime.Now - start).TotalMilliseconds)}ms.");

              // Dispose the current raw image and store the image with drawn text.
              sourceImages[index].Dispose();
              sourceImages[index] = editedImage;
            }

            //sourceImages[index].Save($@"{System.IO.Path.GetDirectoryName(fileNames[index])}\edited{System.IO.Path.GetFileNameWithoutExtension(fileNames[index])}{System.IO.Path.GetExtension(fileNames[index])}");
          }
        }

        // Add the images to a gif.
        OnChangeStep?.Invoke("Adding Images to GIF");
        string gifPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\ImagesToGif{GetCurrentTimeShort()}.gif";
        imageManipulator.OnProgress += ProgressUpdate;
        imageManipulator.MakeGifFromImages(gifPath, sourceImages, animationDelays, loops);
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

    public string ConvertGifToVideo(string gifFilePath)
    {
      OnChangeStep?.Invoke("Converting GIF to Video");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(gifFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(gifFilePath)}.mp4";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.MakeVideoFromGif(outputPath, gifFilePath);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string CropImageOrVideo(string filePath, double x, double y, double width, double height)
    {
      OnChangeStep?.Invoke("Cropping");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Cropped{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

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

      string outputDirectory = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Images{GetCurrentTimeShort()}";

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
        ImageFeatureDetector.GetXYOffsets(image1, image2);

        string outputPath = $@"{System.IO.Path.GetDirectoryName(image1Path)}\Matches{GetCurrentTimeShort()}.jpg";
        matchesImage.Save(outputPath);
        imageManipulator.OnProgress -= ProgressUpdate;
        return outputPath;
      }
    }

    public string ExtractAudioFromVideo(string videoFilePath)
    {
      OnChangeStep?.Invoke("Extracting Audio");

      string outputPathWithoutExtension = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Extracted{GetCurrentTimeShort()}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      string outputPath = videoManipulator.ExtractAudioFromVideo(outputPathWithoutExtension, videoFilePath);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string FlipImageOrVideo(string filePath, bool horizontal, bool vertical)
    {
      OnChangeStep?.Invoke("Flipping");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Flipped{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      MediaType mediaType = MediaTypeHelper.GetMediaTypeFromFileName(filePath);
      if (mediaType == MediaType.IMAGE)
      {
        ImageManipulator imageManipulator = new ImageManipulator();
        imageManipulator.OnProgress += ProgressUpdate;
        imageManipulator.FlipImage(outputPath, filePath, horizontal, vertical);
        imageManipulator.OnProgress -= ProgressUpdate;
      }
      else if (mediaType == MediaType.VIDEO)
      {
        VideoManipulator videoManipulator = new VideoManipulator();
        videoManipulator.OnProgress += ProgressUpdate;
        videoManipulator.FlipVideo(outputPath, filePath, horizontal, vertical);
        videoManipulator.OnProgress -= ProgressUpdate;
      }
      else
        throw new NotImplementedException($"Unsupported file extension '{System.IO.Path.GetExtension(filePath)}'.");

      return outputPath;
    }

    public string GetScreenshotFromVideo(string videoFilePath, string time, bool end = false)
    {
      if (!string.IsNullOrEmpty(time) && end)
        throw new ArgumentException("Can't set both time and end.");

      OnChangeStep?.Invoke("Getting Screenshot");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Screenshot{GetCurrentTimeShort()}.jpg";

      VideoManipulator videoManipulator = new VideoManipulator();

      if (end)
        videoManipulator.GetScreenshotAtEnd(outputPath, videoFilePath);
      else
        videoManipulator.GetScreenshotAtTime(outputPath, videoFilePath, time);
        
      return outputPath;
    }

    public string ExtendLastFrameOfVideo(string videoFilePath, double seconds)
    {
      OnChangeStep?.Invoke("Extending Last Frame of Video");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Extended{GetCurrentTimeShort()}{System.IO.Path.GetExtension(videoFilePath)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.ExtendLastFrame(outputPath, videoFilePath, seconds);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string NormalizeVolume(string filePath)
    {
      OnChangeStep?.Invoke("Normalizing Audio");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Normalized{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      AudioManipulator audioManipulator = new AudioManipulator();
      audioManipulator.OnProgress += ProgressUpdate;
      audioManipulator.NormalizeVolume(outputPath, filePath);
      audioManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string RemoveAudioFromVideo(string videoFilePath)
    {
      OnChangeStep?.Invoke("Removing Audio");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_AudioRemoved{GetCurrentTimeShort()}{System.IO.Path.GetExtension(videoFilePath)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.RemoveAudioFromVideo(outputPath, videoFilePath);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string ResizeImageOrVideo(string filePath, int width, int height, float scaleFactor = 0)
    {
      bool resize = false, scale = false;
      if (width > 0 || height > 0)
        resize = true;
      if (scaleFactor > 0)
        scale = true;

      if (!resize && !scale)
        throw new ArgumentException("width/height or scaleFactor must be greater than 0 to resize an image or video.");
      if (resize && scale)
        throw new ArgumentException("width/height and scaleFactor can not be greater than 0. Set either width/height or scaleFactor.");

      OnChangeStep?.Invoke("Resizing");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Resized{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      MediaType mediaType = MediaTypeHelper.GetMediaTypeFromFileName(filePath);
      if (mediaType == MediaType.IMAGE)
      {
        using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filePath))
        {
          ImageManipulator imageManipulator = new ImageManipulator();
          System.Drawing.Bitmap newBitmap = null;
          try
          {
            if (resize)
              newBitmap = imageManipulator.GetResizedImage(bitmap, width, height);
            else if (scale)
              newBitmap = imageManipulator.GetScaledImage(bitmap, scaleFactor);

            newBitmap.Save(outputPath);
          }
          catch (Exception)
          {
            throw;
          }
          finally
          {
            newBitmap?.Dispose();
          }
        }
      }
      else if (mediaType == MediaType.VIDEO)
      {
        VideoManipulator videoManipulator = new VideoManipulator();
        videoManipulator.OnProgress += ProgressUpdate;
        if (resize)
          videoManipulator.ResizeVideo(outputPath, filePath, width, height);
        else if (scale)
          videoManipulator.ScaleVideo(outputPath, filePath, scaleFactor);
        videoManipulator.OnProgress -= ProgressUpdate;
      }
      else
        throw new NotImplementedException($"Unsupported file extension '{System.IO.Path.GetExtension(filePath)}'.");

      return outputPath;
    }

    public string Reverse(string filePath)
    {
      OnChangeStep?.Invoke("Reversing File");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Reversed{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      MediaType fileMediaType = MediaTypeHelper.GetMediaTypeFromFileName(filePath);
      if (fileMediaType == MediaType.AUDIO)
      {
        AudioManipulator audioManipulator = new AudioManipulator();
        audioManipulator.OnProgress += ProgressUpdate;
        audioManipulator.ReverseAudio(outputPath, filePath);
        audioManipulator.OnProgress -= ProgressUpdate;

      }
      else if (fileMediaType == MediaType.VIDEO || System.IO.Path.GetExtension(filePath).ToLower() == ".gif")
      {
        VideoManipulator videoManipulator = new VideoManipulator();
        videoManipulator.OnProgress += ProgressUpdate;
        videoManipulator.
ReverseVideo(outputPath, filePath);
        videoManipulator.OnProgress -= ProgressUpdate;
      }
      else
        throw new NotSupportedException($"Media type of '{filePath}' not supported for reversing.");

      return outputPath;
    }

    public string RotateImageOrVideo(string filePath, bool counterClockwise)
    {
      OnChangeStep?.Invoke("Rotating");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Rotated{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      MediaType mediaType = MediaTypeHelper.GetMediaTypeFromFileName(filePath);
      if (mediaType == MediaType.IMAGE)
      {
        ImageManipulator imageManipulator = new ImageManipulator();
        imageManipulator.OnProgress += ProgressUpdate;
        imageManipulator.RotateImage(outputPath, filePath, counterClockwise);
        imageManipulator.OnProgress -= ProgressUpdate;
      }
      else if (mediaType == MediaType.VIDEO)
      {
        VideoManipulator videoManipulator = new VideoManipulator();
        videoManipulator.OnProgress += ProgressUpdate;
        videoManipulator.RotateVideo(outputPath, filePath, counterClockwise);
        videoManipulator.OnProgress -= ProgressUpdate;
      }
      else
        throw new NotImplementedException($"Unsupported file extension '{System.IO.Path.GetExtension(filePath)}'.");

      return outputPath;
    }

    public string StabilizeVideo(string videoFilePath, int optzoom)
    {
      OnChangeStep?.Invoke("Stabilizing Video");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_Stabilized{GetCurrentTimeShort()}{System.IO.Path.GetExtension(videoFilePath)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.StabilizeVideo(outputPath, videoFilePath, optzoom);
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
          string outputPath = $@"{System.IO.Path.GetDirectoryName(fileNames[0])}\Stitched{GetCurrentTimeShort()}{System.IO.Path.GetExtension(fileNames[0])}";
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
      var mfi = new MediaFileInfo(fileNames[0]);
      foreach(var l in mfi.AudioStreams)
      {
        Console.WriteLine("\n\nAUDIO STREAM:");
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(l, Newtonsoft.Json.Formatting.Indented));
      }
      foreach (var l in mfi.VideoStreams)
      {
        Console.WriteLine("\n\nVIDEO STREAM:");
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(l, Newtonsoft.Json.Formatting.Indented));
      }

      return null;
    }

    public string TrimAudioOrVideo(string filePath, string startTime, string endTime)
    {
      OnChangeStep?.Invoke("Trimming File");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(filePath)}\{System.IO.Path.GetFileNameWithoutExtension(filePath)}_Trimmed{GetCurrentTimeShort()}{System.IO.Path.GetExtension(filePath)}";

      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.Trim(outputPath, filePath, startTime, endTime);
      videoManipulator.OnProgress -= ProgressUpdate;

      return outputPath;
    }

    public string TwwToMp4(string videoFilePath, int x, int y, int width, int height)
    {
      if (x == 0 && y == 0 && width == 0 && height == 0)
      {
        x = 400;
        y = 222;
        width = 650;
        height = 444;
      }

      return WarhammerTimelapseHelper(videoFilePath, x, y, width, height);
    }

    public string Tww3ToMp4(List<string> videoFilePaths, double timelapseLengthInSeconds, double endLengthInSeconds,
      int mapX, int mapY, int mapWidth, int mapHeight,
      int turnNumberX, int turnNumberY, int turnNumberWidth, int turnNumberHeight)
    {
      return Warhammer3TimelapseHelper(videoFilePaths, timelapseLengthInSeconds, endLengthInSeconds, mapX, mapY, mapWidth, mapHeight, turnNumberX, turnNumberY, turnNumberWidth, turnNumberHeight);
    }

    public string WowToMp4(List<string> screenshotsFilePaths, string mapVideoFilePath, double timelapseLengthInSeconds, double endLengthInSeconds,
      int levelNumberX, int levelNumberY, int levelNumberWidth, int levelNumberHeight)
    {
      return WowTimelapseHelper(screenshotsFilePaths, mapVideoFilePath, timelapseLengthInSeconds, endLengthInSeconds, levelNumberX, levelNumberY, levelNumberWidth, levelNumberHeight);
    }

    private string GetCurrentTimeShort()
    {
      byte[] bytes = BitConverter.GetBytes(long.Parse(DateTime.Now.ToString("yyMMddHHmmss")));
      Array.Reverse(bytes);

      char[] array = Convert.ToBase64String(bytes).Trim('A', '=').Replace('/', ')').ToCharArray();
      return new string(array);
    }

    private void ProgressUpdate(float percent)
    {
      OnProgressUpdate?.Invoke(percent);
    }

    private string WarhammerTimelapseHelper(string videoFilePath, int x, int y, int width, int height)
    {
      const int frameDelay = 60, finalFramDelay = 2400;

      // Convert video to images.
      string imageDirectory = ConvertVideoToImages(videoFilePath, (1 / .35).ToString());
      List<string> imagePaths = System.IO.Directory.GetFiles(imageDirectory).ToList();
      imagePaths.Sort(new NaturalStringComparer());

      OnChangeStep?.Invoke("Getting Turn Numbers");

      // Rename images.
      Dictionary<decimal, int> turnCounts = new Dictionary<decimal, int>();
      for (int i = 0; i < imagePaths.Count; i++)
      {
        OnProgressUpdate?.Invoke(i / (float)imagePaths.Count);

        decimal turn;
        using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imagePaths[i]))
        using (System.Drawing.Bitmap cbmp = new ImageManipulator().GetCroppedImage(bmp, 1123, 688, 30, 14))
        using (ImageMagick.MagickImage magickImage = new ImageMagick.MagickImage(cbmp))
        {
          magickImage.Grayscale();
          magickImage.Resize(new ImageMagick.MagickGeometry(magickImage.Width * 10, magickImage.Height * 10));
          using (System.Drawing.Bitmap grayBmp = magickImage.ToBitmap())
          {
            string temppath = $@"{System.IO.Path.GetDirectoryName(imagePaths[i])}\{System.IO.Path.GetFileNameWithoutExtension(imagePaths[i])}_temp{System.IO.Path.GetExtension(imagePaths[i])}";
            grayBmp.Save(temppath);
            turn = ImageFeatureDetector.GetNumberFromImage(temppath);
            //Console.WriteLine($"{System.IO.Path.GetFileNameWithoutExtension(imagePaths[i])} -> {txt}");
            System.IO.File.Delete(temppath);
          }
        }

        if (turnCounts.ContainsKey(turn))
          turnCounts[turn]++;
        else
          turnCounts.Add(turn, 1);

        string newImagePath = $@"{System.IO.Path.GetDirectoryName(imagePaths[i])}\{turn}!end!{turnCounts[turn]}{System.IO.Path.GetExtension(imagePaths[i])}";

        System.IO.File.Move(imagePaths[i], newImagePath);
        imagePaths[i] = newImagePath;
      }

      OnChangeStep?.Invoke("Setting Animation Delays");

      List<int> animationDelays = new List<int>();
      for (int i = 0; i < imagePaths.Count; i++)
      {
        OnProgressUpdate?.Invoke(i / (float)imagePaths.Count);

        if (i == imagePaths.Count - 1)
        {
          animationDelays.Add(finalFramDelay);
          break;
        }

        string fileName = System.IO.Path.GetFileNameWithoutExtension(imagePaths[i]);
        decimal turn = decimal.Parse(fileName.Substring(0, fileName.IndexOf("!end!")));
        int count = int.Parse(fileName.Substring(fileName.IndexOf("!end!") + 5));

        int delay = (int)Math.Round(frameDelay / (float)turnCounts[turn]);
        if (delay < 1)
          delay = 1;

        // If this is the last image of a turn and there isn't an image for the next turn: ...
        if (count == turnCounts[turn] && !turnCounts.ContainsKey(turn + 1))
        {
          int skippedTurns = 1;
          while (!turnCounts.ContainsKey(turn + 1 + skippedTurns))
            skippedTurns++;

          delay += skippedTurns * frameDelay;
        }

        animationDelays.Add(delay);

        Console.WriteLine($"{turn}: {delay}");
      }

      string gifPath = ConvertImagesToGif(imagePaths.ToArray(), x, y, width, height, animationDelays, 0, 32, true, true, ImageAlignmentType.MAP);

      string newVideoPath = ConvertGifToVideo(gifPath);

      OnChangeStep?.Invoke("Changing Video Playback Rate");

      string outputPath = $@"{System.IO.Path.GetDirectoryName(videoFilePath)}\{System.IO.Path.GetFileNameWithoutExtension(videoFilePath)}_timelapse.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.AdjustVideoSpeed(outputPath, newVideoPath, 12, 20);
      videoManipulator.OnProgress -= ProgressUpdate;

      OnChangeStep?.Invoke("Deleting Temporary Files");
      System.IO.Directory.Delete(imageDirectory, true);

      return outputPath;
    }

    private string Warhammer3TimelapseHelper(List<string> videoFilePaths, double timelapseLengthInSeconds, double endLengthInSeconds,
      int mapX, int mapY, int mapWidth, int mapHeight, int turnNumberX, int turnNumberY, int turnNumberWidth, int turnNumberHeight)
    {
      var imageDirectoryPaths = new List<string>();
      var mainImageFilePaths = new List<string>();
      var turns = new List<int>();
      for (int i = 0; i < videoFilePaths.Count; i++)
      {
        string videoFilePath = videoFilePaths[i];

        OnChangeStep?.Invoke($"Getting Images From Video {i + 1}");

        imageDirectoryPaths.Add(ConvertVideoToImages(videoFilePath, (1 / .35).ToString()));
        List<string> imagePaths = Directory.GetFiles(imageDirectoryPaths[i]).ToList();
        imagePaths.Sort(new NaturalStringComparer());

        OnChangeStep?.Invoke($"Getting Turn Numbers For Video {i + 1}");
        var turnImagePaths = new Dictionary<decimal, string>();
        for (int j = 0; j < imagePaths.Count; j++)
        {
          OnProgressUpdate?.Invoke(j / (float)imagePaths.Count);

          int turn;
          using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imagePaths[j]))
          using (System.Drawing.Bitmap cbmp = new ImageManipulator().GetCroppedImage(bmp, turnNumberX, turnNumberY, turnNumberWidth, turnNumberHeight))
          using (ImageMagick.MagickImage magickImage = new ImageMagick.MagickImage(cbmp))
          {
            magickImage.Grayscale();
            magickImage.Resize(new ImageMagick.MagickGeometry(magickImage.Width * 10, magickImage.Height * 10));
            using (System.Drawing.Bitmap grayBmp = magickImage.ToBitmap())
            {
              string temppath = $@"{Path.GetDirectoryName(imagePaths[j])}\{Path.GetFileNameWithoutExtension(imagePaths[j])}_temp{Path.GetExtension(imagePaths[j])}";
              grayBmp.Save(temppath);
              turn = (int)ImageFeatureDetector.GetNumberFromImage(temppath);
              //Console.WriteLine($"{System.IO.Path.GetFileNameWithoutExtension(imagePaths[i])} -> turn{turn}");
              File.Delete(temppath);
            }
          }

          string newImagePath = $@"{Path.GetDirectoryName(imagePaths[j])}\turn{turn}{Path.GetExtension(imagePaths[j])}";

          if (!turns.Contains(turn))
          {
            if (i == 0)
            {
              mainImageFilePaths.Add(newImagePath);
              turns.Add(turn);
            }
            else
						{
              Console.WriteLine($"Turn {turn} was not found in the first video; not adding it for video {i + 1}.");
              continue;
						}
          }
          
          if (File.Exists(newImagePath))
					{
            File.Delete(newImagePath);
					}

          new ImageManipulator().CropImage(newImagePath, imagePaths[j], mapX, mapY, mapWidth, mapHeight);

          File.Delete(imagePaths[j]);
        }
      }

      OnChangeStep?.Invoke($"Combining Images");
      List<string> fileNames = Directory.GetFiles(imageDirectoryPaths[0]).Select(path => Path.GetFileName(path)).ToList();
      for (int i = 0; i < fileNames.Count; i++)
			{
        OnProgressUpdate?.Invoke(i / (float)fileNames.Count);
        string mainImageFilePath = $"{imageDirectoryPaths[0]}\\{fileNames[i]}";

        for (int j = 1; j < imageDirectoryPaths.Count; j++)
				{
          string currentImageFilePath = $"{imageDirectoryPaths[j]}\\{fileNames[i]}";

          if (!File.Exists(currentImageFilePath))
					{
            string turn = Path.GetFileNameWithoutExtension(fileNames[i]).Substring(fileNames[i].IndexOf("turn") + 4);
            Console.WriteLine($"Turn {turn} was not found in the video {j + 1} video; removing it from video 1.");
            File.Delete(mainImageFilePath);
            mainImageFilePaths.Remove(mainImageFilePath);
            turns.Remove(int.Parse(turn));

            break;
					}

          string tempImageFilePath = $"{imageDirectoryPaths[0]}\\temp{Path.GetExtension(mainImageFilePath)}";
          using (var bitmap1 = new System.Drawing.Bitmap(mainImageFilePath))
          using (var bitmap2 = new System.Drawing.Bitmap(currentImageFilePath))
          using (var combinedBitmap = new ImageManipulator().GetCombinedImage(bitmap1, bitmap2, false))
					{
            combinedBitmap.Save(tempImageFilePath);
          }

          File.Delete(mainImageFilePath);
          File.Move(tempImageFilePath, mainImageFilePath);
          File.Delete(currentImageFilePath);
        }
      }

      OnChangeStep?.Invoke($"Calculating Animation Delays");
      var animationDelays = new List<int>();
      int delayPerTurnInCs = 60;
      for (int i = 0; i < mainImageFilePaths.Count; i++)
			{
        OnProgressUpdate?.Invoke(i / (float)mainImageFilePaths.Count);

        if (i == mainImageFilePaths.Count - 1)
        {
          animationDelays.Add(delayPerTurnInCs);
          break;
        }

        int thisTurn = turns[i];
        int nextTurn = turns[i + 1];

        animationDelays.Add(delayPerTurnInCs * (nextTurn - thisTurn));
      }

      string gifPath = ConvertImagesToGif(mainImageFilePaths.ToArray(), 0, 0, 0, 0, animationDelays, 0, 32, false, false, null);
      string video1Path = ConvertGifToVideo(gifPath);

      OnChangeStep?.Invoke($"Adjusting Video Speed");
      float csPerTurn = (float)(timelapseLengthInSeconds * 100) / (turns[turns.Count - 1] - turns[0]);
      string video2Path = $"{Path.GetDirectoryName(video1Path)}\\tww3videoraw{Path.GetExtension(video1Path)}";
      VideoManipulator videoManipulator = new VideoManipulator();
      videoManipulator.OnProgress += ProgressUpdate;
      videoManipulator.AdjustVideoSpeed(video2Path, video1Path, delayPerTurnInCs / csPerTurn, 20);
      videoManipulator.OnProgress -= ProgressUpdate;

      string finalVideoPath = $"{Path.GetDirectoryName(videoFilePaths[0])}\\tww3video{GetCurrentTimeShort()}{Path.GetExtension(video2Path)}";
      new VideoManipulator().ExtendLastFrame(finalVideoPath, video2Path, endLengthInSeconds);

      foreach (string directoryPath in imageDirectoryPaths)
			{
         Directory.Delete(directoryPath, true);
			}

      return finalVideoPath;
    }

    private string WowTimelapseHelper(List<string> screenshotsFilePaths, string mapVideoFilePath, double timelapseLengthInSeconds, double endLengthInSeconds,
      int levelNumberX, int levelNumberY, int levelNumberWidth, int levelNumberHeight)
    {
      OnChangeStep?.Invoke($"Getting Images From Video");

      screenshotsFilePaths.Sort(new NaturalStringComparer());

      var mapVideoMediaFileInfo = new MediaFileInfo(mapVideoFilePath);
      double fpsToExtractImagesFromMapVideo = 24 * (timelapseLengthInSeconds / mapVideoMediaFileInfo.Duration.Value);
      if (fpsToExtractImagesFromMapVideo > mapVideoMediaFileInfo.VideoStreams[0].AvgFrameRate) fpsToExtractImagesFromMapVideo = mapVideoMediaFileInfo.VideoStreams[0].AvgFrameRate.Value;
      string videoImagesDirectoryPath = ConvertVideoToImages(mapVideoFilePath, 24.ToString());
      List<string> videoImagesPaths = Directory.GetFiles(videoImagesDirectoryPath).ToList();
      videoImagesPaths.Sort(new NaturalStringComparer());

      OnChangeStep?.Invoke($"Combining Images From Screenshots and Video");
      int currentScreenshotIndex = 0;
      var combinedImagesPaths = new List<string>();
      for (int i = 0; i < videoImagesPaths.Count; i++)
      {
        OnProgressUpdate?.Invoke(i / (float)videoImagesPaths.Count);

        int levelFromVideoImage;
        using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(videoImagesPaths[i]))
        using (System.Drawing.Bitmap cbmp = new ImageManipulator().GetCroppedImage(bmp, levelNumberX, levelNumberY, levelNumberWidth, levelNumberHeight))
        using (ImageMagick.MagickImage magickImage = new ImageMagick.MagickImage(cbmp))
        {
          magickImage.Grayscale();
          magickImage.Resize(new ImageMagick.MagickGeometry(magickImage.Width * 10, magickImage.Height * 10));
          using (System.Drawing.Bitmap grayBmp = magickImage.ToBitmap())
          {
            string temppath = $@"{Path.GetDirectoryName(videoImagesPaths[i])}\{Path.GetFileNameWithoutExtension(videoImagesPaths[i])}_temp{Path.GetExtension(videoImagesPaths[i])}";
            grayBmp.Save(temppath);
            levelFromVideoImage = (int)ImageFeatureDetector.GetNumberFromImage(temppath);
            File.Delete(temppath);
          }
        }

        if (!int.TryParse(Path.GetFileNameWithoutExtension(screenshotsFilePaths[currentScreenshotIndex]), out int levelFromCurrentScreenshot))
        {
          throw new Exception($"Screenshot file at '{Path.GetFileName(screenshotsFilePaths[currentScreenshotIndex])}' must have its file name be a positive integer.");
        }

        if (levelFromVideoImage > levelFromCurrentScreenshot)
        {
          if (currentScreenshotIndex < screenshotsFilePaths.Count - 2)
          {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(screenshotsFilePaths[currentScreenshotIndex + 1]), out int levelFromNextScreenshot))
            {
              throw new Exception($"Screenshot file at '{Path.GetFileName(screenshotsFilePaths[currentScreenshotIndex + 1])}' must have its file name be a positive integer.");
            }

            if (levelFromVideoImage >= levelFromNextScreenshot)
            {
              currentScreenshotIndex++;
            }
          }
        }

        string newImagePath = $@"{Path.GetDirectoryName(videoImagesPaths[i])}\{Path.GetFileNameWithoutExtension(videoImagesPaths[i])}_combined{Path.GetExtension(videoImagesPaths[i])}";
        using (var bitmap1 = new System.Drawing.Bitmap(screenshotsFilePaths[currentScreenshotIndex]))
        using (var bitmap2 = new System.Drawing.Bitmap(videoImagesPaths[i]))
        using (var combinedBitmap = new ImageManipulator().GetCombinedImage(bitmap1, bitmap2, true))
        {
          combinedBitmap.Save(newImagePath);
        }

        File.Delete(videoImagesPaths[i]);
        combinedImagesPaths.Add(newImagePath);
      }
      
      OnChangeStep?.Invoke($"Creating Output Video");
      double outputFps = combinedImagesPaths.Count / timelapseLengthInSeconds;
      string video1Path = $@"{videoImagesDirectoryPath}\tempVideo.mp4";
      new VideoManipulator().MakeVideoFromImages(video1Path, videoImagesDirectoryPath, $"%01d_combined{Path.GetExtension(combinedImagesPaths[0])}", outputFps);

      string finalVideoPath = $"{Path.GetDirectoryName(mapVideoFilePath)}\\wowvideo{GetCurrentTimeShort()}{Path.GetExtension(video1Path)}";
      new VideoManipulator().ExtendLastFrame(finalVideoPath, video1Path, endLengthInSeconds);

      Directory.Delete(videoImagesDirectoryPath, true);

      return finalVideoPath;
    }
  }
}
