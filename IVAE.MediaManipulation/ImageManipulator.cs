using System;
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
using System.Drawing.Imaging;

namespace IVAE.MediaManipulation
{
  public class ImageManipulator
  {
    public event Action<float> OnProgress;

    public void CombineGifs(string outputPath, List<string> gifPaths, int gifsPerLine)
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

    public void CropImage(string outputPath, string imagePath, double x, double y, double width, double height)
    {
      Bitmap croppedImage;
      using (Bitmap originalImage = new Bitmap(imagePath))
      {
        croppedImage = GetCroppedImage(originalImage, x, y, width, height);
      }

      croppedImage.SaveAndInferFormat(outputPath);
      croppedImage.Dispose();
    }

    public void DrawTextOnImage(IMagickImage image, string text, int fontSize)
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

    public void DrawTextOnImage(string imagePath, string text, int fontSize)
    {
      DateTime start = DateTime.Now;
      using (MagickImage image = new MagickImage(imagePath))
      {
        DrawTextOnImage(image, text, fontSize);

        image.Write(imagePath);
      }
    }

    public void FlipImage(string outputPath, string imagePath, bool horizontal, bool vertical)
    {
      using (Bitmap image = new Bitmap(imagePath))
      {
        RotateFlipType rotateFlipType;
        if (horizontal && vertical)
          rotateFlipType = RotateFlipType.RotateNoneFlipXY;
        else if (horizontal)
          rotateFlipType = RotateFlipType.RotateNoneFlipX;
        else if (vertical)
          rotateFlipType = RotateFlipType.RotateNoneFlipY;
        else
          throw new ArgumentException("Either horizontal or vertical must be true.");

        image.RotateFlip(rotateFlipType);
        image.SaveAndInferFormat(outputPath);
      }
    }

    public Bitmap GetAlignedImage(Bitmap imageToAlign, Bitmap referenceImage, ImageAlignmentType imageAlignmentType)
    {
      if (imageAlignmentType == ImageAlignmentType.CROP || imageAlignmentType == ImageAlignmentType.MAP)
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(imageToAlign, referenceImage);

        if (imageAlignmentType == ImageAlignmentType.CROP)
          return GetCroppedImage(imageToAlign, offsets.Item1, offsets.Item2, referenceImage.Width, referenceImage.Height);

        Bitmap bmp = new Bitmap(referenceImage.Width, referenceImage.Height);
        using (Graphics g = Graphics.FromImage(bmp))
        using (Brush brush = new SolidBrush(Color.Black))
        {
          g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
          g.DrawImage(imageToAlign, -offsets.Item1, -offsets.Item2);
        }

        return bmp;
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
            matchingTechnique = MatchingTechnique.ORB;
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

    public Bitmap GetCombinedImage(Bitmap originalImage, Bitmap updateImage)
    {
      Tuple<int,int> offsets = ImageFeatureDetector.GetXYOffsets(originalImage, updateImage);

      int width, height, originalImageX, originalImageY, updateImageX, updateImageY;
      if (offsets.Item1 >= 0) {
        width = (originalImage.Width > offsets.Item1 + updateImage.Width) ? originalImage.Width : offsets.Item1 + updateImage.Width;
        originalImageX = 0;
        updateImageX = offsets.Item1;
      }
      else
      {
        width = -offsets.Item1 + ((originalImage.Width > offsets.Item1 + updateImage.Width) ? originalImage.Width : offsets.Item1 + updateImage.Width);
        originalImageX = -offsets.Item1;
        updateImageX = 0;
      }

      if (offsets.Item2 >= 0)
      {
        height = (originalImage.Height > offsets.Item2 + updateImage.Height) ? originalImage.Height : offsets.Item2 + updateImage.Height;
        originalImageY = 0;
        updateImageY = offsets.Item2;
      }
      else
      {
        height = -offsets.Item2 + ((originalImage.Height > offsets.Item2 + updateImage.Height) ? originalImage.Height : offsets.Item2 + updateImage.Height);
        originalImageY = -offsets.Item2;
        updateImageY = 0;
      }

      Bitmap bmp = new Bitmap(width, height);
      using (Graphics g = Graphics.FromImage(bmp))
      {
        g.DrawImage(originalImage, originalImageX, originalImageY);
        g.DrawImage(updateImage, updateImageX, updateImageY);
      }

      return bmp;
    }

    public Bitmap GetCroppedImage(Bitmap image, double x, double y, double width, double height)
    {
      if (x < 0)
        throw new ArgumentOutOfRangeException(nameof(x));
      if (y < 0)
        throw new ArgumentOutOfRangeException(nameof(y));
      if (width <= 0)
        throw new ArgumentOutOfRangeException(nameof(width));
      if (height <= 0)
        throw new ArgumentOutOfRangeException(nameof(height));

      int xCoord = (x < 1) ? (int)(image.Width * x) : (int)x;
      int yCoord = (y < 1) ? (int)(image.Height * y) : (int)y;
      int widthInPixels = (width < 1 || (width == 1 && x == 0)) ? (int)(image.Width * width) : (int)width;
      int heightInPixels = (height < 1 || (height == 1 && y == 0)) ? (int)(image.Height * height) : (int)height;

      if (image.Width < (xCoord + widthInPixels))
        throw new Exception($"Image is not wide enough to crop. (Image Width: {image.Width}. Crop X: {xCoord}. Crop Width: {widthInPixels})");
      if (image.Height < (yCoord + heightInPixels))
        throw new Exception($"Image is not high enough to crop. (Image Height: {image.Height}. Crop Y: {yCoord}. Crop Height: {heightInPixels})");

      Rectangle rect = new Rectangle(xCoord, yCoord, widthInPixels, heightInPixels);

      return image.Clone(rect, image.PixelFormat);
    }

    /// <summary>
    /// Draw the model image and observed image, the matched features and homography projection.
    /// </summary>
    /// <param name="modelImage">The model image</param>
    /// <param name="observedImage">The observed image</param>
    /// <returns>The model image and observed image, the matched features and homography projection.</returns>
    public Bitmap GetImageWithDrawnMatches(Bitmap modelImage, Bitmap observedImage, MatchingTechnique matchingTechnique)
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
          mask?.Dispose();
          homography?.Dispose();
        }
      }
    }

    public Bitmap GetImageWithDrawnText(Bitmap image, string text, int fontSize)
    {
      if (image == null)
        throw new ArgumentNullException(nameof(image));

      DateTime start = DateTime.Now;
      using (MagickImage mi = new MagickImage(image))
      {
        DrawTextOnImage(mi, text, fontSize);
        return mi.ToBitmap();
      }
    }

    public Bitmap GetResizedImage(Bitmap image, int width, int height)
    {
      if (image == null)
        throw new ArgumentNullException(nameof(image));
      if (width == 0 && height == 0)
        throw new ArgumentException($"width or height must be greater than 0.");
      if (width < 0)
        throw new ArgumentOutOfRangeException(nameof(width));
      if (height < 0)
        throw new ArgumentOutOfRangeException(nameof(height));

      using (MagickImage magickImage = new MagickImage(image))
      {
        magickImage.Resize(width, height);

        return magickImage.ToBitmap();
      }
    }

    public Bitmap GetScaledImage(Bitmap image, float scaleModifier)
    {
      if (image == null)
        throw new ArgumentNullException(nameof(image));
      if (scaleModifier <= 0)
        throw new ArgumentOutOfRangeException(nameof(scaleModifier));

      return GetResizedImage(image, (int)Math.Round(image.Width * scaleModifier), 0);
    }

    public Bitmap GetStitchedImage(List<Bitmap> sourceImages)
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

    public void MakeGifFromImages(string outputPath, List<Bitmap> images, int animationDelay, int finalFrameAnimationDelay = 0, int animationIterations = 0)
    {
      List<int> animationDelays = new List<int>();

      for (int i = 0; i < images.Count - 1; i++)
        animationDelays.Add(animationDelay);

      if (finalFrameAnimationDelay > 0)
        animationDelays.Add(finalFrameAnimationDelay);
      else
        animationDelays.Add(animationDelay);

      MakeGifFromImages(outputPath, images, animationDelays, animationIterations);
    }

    public void MakeGifFromImages(string outputPath, List<Bitmap> images, List<int> animationDelays, int animationIterations = 0)
    {
      if (images.Count != animationDelays.Count)
        throw new ArgumentException($"{nameof(images)} count is not equal to {nameof(animationDelays)} count.");

      using (MagickImageCollection imageCollection = new MagickImageCollection())
      {
        QuantizeSettings quantizeSettings = new QuantizeSettings();
        quantizeSettings.Colors = 256;
        quantizeSettings.DitherMethod = DitherMethod.No;

        for (int i = 0; i < images.Count; i++)
        {
          OnProgress?.Invoke(i / (float)images.Count);

          imageCollection.Add(new MagickImage(images[i]));

          Console.Write($"{animationDelays[i]} ");

          imageCollection[imageCollection.Count - 1].AnimationDelay = animationDelays[i];
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

    public void RotateImage(string outputPath, string imagePath, bool counterClockwise)
    {
      using (Bitmap image = new Bitmap(imagePath))
      {
        RotateFlipType rotateFlipType = counterClockwise ? RotateFlipType.Rotate270FlipNone : RotateFlipType.Rotate90FlipNone;
        image.RotateFlip(rotateFlipType);
        image.SaveAndInferFormat(outputPath);
      }
    }
  }
}
