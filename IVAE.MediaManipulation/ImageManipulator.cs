﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace IVAE.MediaManipulation
{
  public static class ImageManipulator
  {
    public delegate void ProgressDelegate(float percentage);
    public static event ProgressDelegate OnProgress;

    public static void CombineGifs(string outputPath, List<string> gifPaths, int gifsPerLine)
    {
      if (outputPath == null)
        throw new ArgumentNullException(nameof(outputPath));
      if (gifPaths == null)
        throw new ArgumentNullException(nameof(gifPaths));
      if (gifsPerLine <= 0)
        throw new ArgumentOutOfRangeException(nameof(gifsPerLine));

      List<MagickImageCollection> gifs = new List<MagickImageCollection>(gifPaths.Count);
      List<int> indices = new List<int>(gifPaths.Count);
      List<int> remainingAnimationTimes = new List<int>(gifPaths.Count);
      try
      {
        for (int i = 0; i < gifPaths.Count; i++)
        {
          gifs.Add(new MagickImageCollection(gifPaths[i]));
          indices.Add(0);
          remainingAnimationTimes.Add(gifs[i][0].AnimationDelay);
        }

        int individualWidth = gifs[0][0].Width;
        int individualHeight = gifs[0][0].Height;
        int width = Math.Min(gifs.Count, gifsPerLine) * individualWidth;
        int height = (int)Math.Ceiling(gifs.Count / (double)gifsPerLine) * individualHeight;
        using (MagickImageCollection newGif = new MagickImageCollection())
        {
          bool doneWithAllGifs = false;
          while (!doneWithAllGifs)
          {
            List<float> percentFinishedPerGif = new List<float>(gifs.Count);
            for(int i = 0; i < gifs.Count; i++)
              percentFinishedPerGif.Add(indices[i] / (float)gifs[i].Count);
            OnProgress?.Invoke(percentFinishedPerGif.Average());

            for (int i = 0; i < indices.Count; i++)
            {
              if (remainingAnimationTimes[i] <= 0 && indices[i] < gifs[i].Count - 1)
              {
                indices[i]++;
                remainingAnimationTimes[i] = gifs[i][indices[i]].AnimationDelay;
              }
            }

            doneWithAllGifs = true;
            for (int i = 0; i < indices.Count; i++)
            {
              if (indices[i] < gifs[i].Count - 1)
              {
                doneWithAllGifs = false;
                break;
              }
            }

            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height))
            {
              using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
              {
                for (int i = 0; i < gifs.Count; i++)
                {
                  graphics.DrawImage(gifs[i][indices[i]].ToBitmap(), (int)Math.Floor((double)i % gifsPerLine) * individualWidth, (int)Math.Floor((double)i / gifsPerLine) * individualHeight);
                }
              }

              newGif.Add(new MagickImage(bitmap));

              int animationDelay;
              if (!doneWithAllGifs)
              {
                animationDelay = int.MaxValue;
                for (int i = 0; i < remainingAnimationTimes.Count; i++)
                {
                  if (indices[i] < gifs[i].Count - 1 && remainingAnimationTimes[i] > 0 && remainingAnimationTimes[i] < animationDelay)
                    animationDelay = remainingAnimationTimes[i];
                }
              }
              else
                animationDelay = remainingAnimationTimes.Max();

              newGif[newGif.Count - 1].AnimationDelay = animationDelay;

              for (int i = 0; i < remainingAnimationTimes.Count; i++)
              {
                if (remainingAnimationTimes[i] > 0)
                  remainingAnimationTimes[i] -= animationDelay;
              }
            }
          }

          long size = 0;
          foreach(var v in newGif)
          {
            System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
            size += ((byte[])converter.ConvertTo(v.ToBitmap(), typeof(byte[]))).Length;
          }
          Console.WriteLine($"Combined GIF size: {size / 1024}KB");

          newGif.Write(outputPath);
        }

        foreach (MagickImageCollection mic in gifs)
          mic.Dispose();
      }
      catch (Exception)
      {
        foreach(MagickImageCollection mic in gifs)
          mic.Dispose();

        throw;
      }
    }

    public static void CropImage(string imagePath, int x, int y, int width, int height)
    {
      System.Drawing.Bitmap croppedImage;
      using (System.Drawing.Bitmap originalImage = new System.Drawing.Bitmap(imagePath))
      {
        croppedImage = GetCroppedImage(originalImage, x, y, width, height);
      }

      croppedImage.Save(imagePath);
      croppedImage.Dispose();
    }

    public static void DrawTextOnImage(IMagickImage image, string text, int fontSize)
    {
      if (image == null)
        throw new ArgumentNullException(nameof(image));
      if (text == null)
        throw new ArgumentNullException(nameof(text));
      if (fontSize <= 0)
        throw new ArgumentOutOfRangeException(nameof(fontSize));

      new Drawables()
        .FontPointSize(fontSize)
        .StrokeColor(MagickColors.Black)
        .FillColor(MagickColors.White)
        .Text(image.Width - 2, image.Height - 2, text)
        .TextAlignment(TextAlignment.Right)
        .Draw(image);
    }

    public static void DrawTextOnImage(string imagePath, string text, int fontSize)
    {
      DateTime start = DateTime.Now;
      using (MagickImage image = new MagickImage(imagePath))
      {
        DrawTextOnImage(image, text, fontSize);

        image.Write(imagePath);
      }
    }

    public static Bitmap GetAlignedImage(Bitmap imageToAlign, Bitmap referenceImage, ImageAlignmentType imageAlignmentType)
    {
      if (imageAlignmentType == ImageAlignmentType.CROP)
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(imageToAlign, referenceImage);
        return GetCroppedImage(imageToAlign, offsets.Item1, offsets.Item2, referenceImage.Width, referenceImage.Height);
      }

      using (Image<Bgr, byte> alignImg = new Image<Bgr, byte>(imageToAlign))
      using (Image<Bgr, byte> refImg = new Image<Bgr, byte>(referenceImage))
      using (Mat alignMat = alignImg.Mat)
      using (Mat refMat = refImg.Mat)
      {
        using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
        {
          MatchingTechnique matchingTechnique;
          if (imageAlignmentType == ImageAlignmentType.FASTWARP)
            matchingTechnique = MatchingTechnique.FAST;
          else if (imageAlignmentType == ImageAlignmentType.FULLWARP)
            matchingTechnique = MatchingTechnique.SURF;
          else
            throw new NotImplementedException();

          ImageFeatureDetector.FindMatches(alignMat, refMat, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, matches, out Mat mask, out Mat homography, matchingTechnique, 1.0f);

          try
          {
            using (Mat result = new Mat())
            {
              Features2DToolbox.DrawMatches(alignMat, modelKeyPoints, refMat, observedKeyPoints, matches, result, new MCvScalar(255, 0, 0), new MCvScalar(0, 0, 255), mask);
              result.Save(@"D:\Downloads\Draw.jpg");
            }

            using (Mat warped = new Mat())
            {
              if (homography == null)
                throw new Exception("Could not determine homography between images.");

              CvInvoke.WarpPerspective(alignMat, warped, homography, refMat.Size);
              
              return warped.ToBitmap();
            }
          }
          catch (Exception)
          {
            throw;
          }
          finally
          {
            mask.Dispose();
            homography.Dispose();
          }
        }
      }
    }

    public static Bitmap GetCroppedImage(Bitmap image, int x, int y, int width, int height)
    {
      if (x < 0)
        throw new ArgumentOutOfRangeException(nameof(x));
      if (y < 0)
        throw new ArgumentOutOfRangeException(nameof(y));
      if (width <= 0)
        throw new ArgumentOutOfRangeException(nameof(width));
      if (height <= 0)
        throw new ArgumentOutOfRangeException(nameof(height));

      if (image.Width < (x + width))
        throw new Exception($"Image is not wide enough to crop. (Image Width: {image.Width}. Crop X: {x}. Crop Width: {width})");
      if (image.Height < (y + height))
        throw new Exception($"Image is not high enough to crop. (Image Height: {image.Height}. Crop Y: {y}. Crop Height: {height})");

      System.Drawing.Rectangle rect = new System.Drawing.Rectangle(x, y, width, height);

      return image.Clone(rect, image.PixelFormat);
    }

    /// <summary>
    /// Draw the model image and observed image, the matched features and homography projection.
    /// </summary>
    /// <param name="modelImage">The model image</param>
    /// <param name="observedImage">The observed image</param>
    /// <returns>The model image and observed image, the matched features and homography projection.</returns>
    public static Bitmap GetImageWithDrawnMatches(Bitmap modelImage, Bitmap observedImage, MatchingTechnique matchingTechnique)
    {
      VectorOfKeyPoint modelKeyPoints;
      VectorOfKeyPoint observedKeyPoints;

      using (Image<Bgr, byte> modelImg = new Image<Bgr, byte>(modelImage))
      using (Image<Bgr, byte> observedImg = new Image<Bgr, byte>(observedImage))
      using (Emgu.CV.Mat modelMat = modelImg.Mat)
      using (Emgu.CV.Mat observedMat = observedImg.Mat)
      using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
      {
        ImageFeatureDetector.FindMatches(modelMat, observedMat, out modelKeyPoints, out observedKeyPoints, matches, out Mat mask, out Mat homography, matchingTechnique);

        try
        {
          using (Mat result = new Mat())
          {
            Features2DToolbox.DrawMatches(modelMat, modelKeyPoints, observedMat, observedKeyPoints, matches, result, new MCvScalar(255, 0, 0), new MCvScalar(0, 0, 255), mask);

            return result.ToBitmap();
          }
        }
        catch (Exception)
        {
          throw;
        }
        finally
        {
          mask.Dispose();
          homography.Dispose();
        }
      }
    }

    public static Bitmap GetImageWithDrawnText(Bitmap image, string text, int fontSize)
    {
      DateTime start = DateTime.Now;
      using (MagickImage mi = new MagickImage(image))
      {
        DrawTextOnImage(mi, text, fontSize);
        return mi.ToBitmap();
      }
    }

    public static Bitmap GetStitchedImage(List<Bitmap> sourceImages)
    {
      VectorOfMat sourceMats = new VectorOfMat();
      try
      {
        foreach (Bitmap bmp in sourceImages)
          sourceMats.Push(new Image<Bgr, byte>(bmp).Mat);

        using (Emgu.CV.Stitching.Stitcher stitcher = new Emgu.CV.Stitching.Stitcher(false))
        {
          using (Mat pano = new Mat())
          {
            if (!stitcher.Stitch(sourceMats, pano))
              throw new Exception($"Failed to stitch images.");

            return pano.ToBitmap();
          }
        }
      }
      catch (Exception)
      {
        throw;
      }
      finally
      {
        for(int i = 0; i < sourceMats.Size; i++)
          sourceMats[i].Dispose();
      }
    }

    public static void MakeGifFromImages(string outputPath, List<Bitmap> images, int animationDelay, int finalFrameAnimationDelay, int animationIterations)
    {
      using (MagickImageCollection imageCollection = new MagickImageCollection())
      {
        QuantizeSettings quantizeSettings = new QuantizeSettings();
        quantizeSettings.Colors = 256;
        quantizeSettings.DitherMethod = DitherMethod.No;

        for (int i = 0; i < images.Count; i++)
        {
          OnProgress?.Invoke(i / (float)images.Count);

          imageCollection.Add(new MagickImage(images[i]));

          int delay = animationDelay;
          if (i == images.Count - 1)
            delay = finalFrameAnimationDelay;

          imageCollection[imageCollection.Count - 1].AnimationDelay = delay;
          imageCollection[imageCollection.Count - 1].AnimationIterations = animationIterations;
          imageCollection[imageCollection.Count - 1].Format = MagickFormat.Gif;
          imageCollection[imageCollection.Count - 1].Quantize(quantizeSettings);
        }
        
        //imageCollection.Quantize(quantizeSettings); reduces gif size but reduces overall colors

        //imageCollection.Optimize(); does nothing

        imageCollection.Write(outputPath);

        //ImageMagick.ImageOptimizers.GifOptimizer gifOptimizer = new ImageMagick.ImageOptimizers.GifOptimizer(); does nothing
        //gifOptimizer.LosslessCompress(outputPath);
      }
    }

    public static void MakeGifvFromGif(string outputPath, string gifPath)
    {
      int width, height;
      List<int> animationDelays = new List<int>();
      using (MagickImageCollection imageCollection = new MagickImageCollection(gifPath))
      {
        width = imageCollection[0].Width;
        height = imageCollection[0].Height;

        if (width % 2 != 0)
          width--;
        if (height % 2 != 0)
          height--;

        for (int i = 0; i < imageCollection.Count; i++)
        {
          animationDelays.Add(imageCollection[i].AnimationDelay);
        }
      }

      int gcdAnimationDelay = MathHelper.GCD(animationDelays.ToArray());

      using (Accord.Video.FFMPEG.VideoFileWriter vfw = new Accord.Video.FFMPEG.VideoFileWriter())
      {
        vfw.Open(outputPath, width, height, new Accord.Math.Rational(100, gcdAnimationDelay), Accord.Video.FFMPEG.VideoCodec.MPEG4);
        
        using (MagickImageCollection imageCollection = new MagickImageCollection(gifPath))
        {
          for (int i = 0; i < imageCollection.Count; i++)
          {
            OnProgress?.Invoke(i / (float)imageCollection.Count);

            for (int j = 0; j < imageCollection[i].AnimationDelay / gcdAnimationDelay; j++)
              vfw.WriteVideoFrame(imageCollection[i].ToBitmap());
          }
        }

        vfw.Close();
      }
    }

    public static void MakeGifvFromImages(string outputPath, List<string> imagePaths, int animationDelay, int finalFrameAnimationDelay, int animationIterations)
    {
      int width, height;
      using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imagePaths[0]))
      {
        width = bmp.Width;
        height = bmp.Height;
      }

      using (Accord.Video.FFMPEG.VideoFileWriter vfw = new Accord.Video.FFMPEG.VideoFileWriter())
      {
        vfw.Open(outputPath, width, height, new Accord.Math.Rational(100, animationDelay), Accord.Video.FFMPEG.VideoCodec.MPEG4);

        for (int i = 0; i < imagePaths.Count; i++)
        {
          OnProgress?.Invoke(i / (float)imagePaths.Count);

          using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imagePaths[i]))
          {
            vfw.WriteVideoFrame(bmp);
          }
        }

        vfw.Close();
      }
    }
  }
}