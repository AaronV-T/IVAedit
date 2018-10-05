﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

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

    public static System.Drawing.Bitmap GetCroppedImage(System.Drawing.Bitmap image, int x, int y, int width, int height)
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

    public static System.Drawing.Bitmap GetImageWithDrawnText(System.Drawing.Bitmap image, string text, int fontSize)
    {
      DateTime start = DateTime.Now;
      using (MagickImage mi = new MagickImage(image))
      {
        DrawTextOnImage(mi, text, fontSize);
        return mi.ToBitmap();
      }
    }

    public static void MakeGifFromImages(string outputPath, List<System.Drawing.Bitmap> images, int animationDelay, int finalFrameAnimationDelay, int animationIterations)
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
          width = imageCollection[0].Width;
          height = imageCollection[0].Height;

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
