using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Cuda;
using Emgu.CV.XFeatures2D;

namespace IVAE.MediaManipulation
{
  public enum MatchingTechnique { FAST = 0, ORB = 1, SURF = 2 }

  public static class ImageFeatureDetector
  {
    /// <summary>
    /// Draw the model image and observed image, the matched features and homography projection.
    /// </summary>
    /// <param name="modelImage">The model image</param>
    /// <param name="observedImage">The observed image</param>
    /// <returns>The model image and observed image, the matched features and homography projection.</returns>
    public static Bitmap DrawMatches(Bitmap modelImage, Bitmap observedImage, MatchingTechnique matchingTechnique)
    {
      VectorOfKeyPoint modelKeyPoints;
      VectorOfKeyPoint observedKeyPoints;

      using (Image<Bgr, byte> modelImg = new Image<Bgr, byte>(modelImage))
      using (Image<Bgr, byte> observedImg = new Image<Bgr, byte>(observedImage))
      using (Emgu.CV.Mat modelMat = modelImg.Mat)
      using (Emgu.CV.Mat observedMat = observedImg.Mat)
      using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
      {
        FindMatches(modelMat, observedMat, out modelKeyPoints, out observedKeyPoints, matches, out Mat mask, out Mat homography, matchingTechnique);

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

    public static void FindMatches(Mat modelImage, Mat observedImage, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, MatchingTechnique matchingTechnique, float keyPointFilter = 1, double detectorParameter = -1)
    {
      int k = 2;
      double uniquenessThreshold = 0.8;

      homography = null;
      modelKeyPoints = new VectorOfKeyPoint();
      observedKeyPoints = new VectorOfKeyPoint();

      using (UMat uModelImage = modelImage.ToUMat(AccessType.Read))
      using (UMat uObservedImage = observedImage.ToUMat(AccessType.Read))
      {
        Feature2D detector;
        Feature2D descriptor;
        DistanceType distanceType;
        if (matchingTechnique == MatchingTechnique.FAST)
        {
          if (detectorParameter <= 0)
            detectorParameter = 20;

          detector = new FastDetector((int)detectorParameter);
          descriptor = new BriefDescriptorExtractor();
          //descriptor = new Freak();
          distanceType = DistanceType.Hamming;
        }
        else if (matchingTechnique == MatchingTechnique.ORB)
        {
          if (detectorParameter <= 0)
            detectorParameter = 500;

          detector = new ORBDetector((int)detectorParameter);
          descriptor = detector;
          distanceType = DistanceType.Hamming;
        }
        else if (matchingTechnique == MatchingTechnique.SURF)
        {
          if (detectorParameter <= 0)
            detectorParameter = 300;

          detector = new SURF(detectorParameter);
          descriptor = detector;
          distanceType = DistanceType.L2;
        }
        else
          throw new NotImplementedException($"{matchingTechnique} not supported.");

        // Extract features from model image.
        UMat modelDescriptors = new UMat();
        detector.DetectRaw(uModelImage, modelKeyPoints, null);
        if (keyPointFilter < 2)
          modelKeyPoints = GetBestKeypointsPercent(modelKeyPoints, keyPointFilter);
        else
          modelKeyPoints = GetBestKeypointsCount(modelKeyPoints, (int)keyPointFilter);
        descriptor.Compute(uModelImage, modelKeyPoints, modelDescriptors);

        // Extract features from observed image.
        UMat observedDescriptors = new UMat();
        detector.DetectRaw(uObservedImage, observedKeyPoints, null);
        if (keyPointFilter < 2)
          observedKeyPoints = GetBestKeypointsPercent(observedKeyPoints, keyPointFilter);
        else
          observedKeyPoints = GetBestKeypointsCount(observedKeyPoints, (int)keyPointFilter);
        descriptor.Compute(uObservedImage, observedKeyPoints, observedDescriptors);

        // Match keypoints.
        BFMatcher matcher = new BFMatcher(distanceType);
        matcher.Add(modelDescriptors);
        matcher.KnnMatch(observedDescriptors, matches, k, null);

        mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
        mask.SetTo(new MCvScalar(255));
        Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

        int nonZeroCount = CvInvoke.CountNonZero(mask);
        if (nonZeroCount >= 4)
        {
          nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, matches, mask, 1.5, 20);
          if (nonZeroCount >= 4)
            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, matches, mask, 2);
        }
      }
    }

    public static Bitmap GetAlignedImage(Bitmap imageToAlign, Bitmap referenceImage)
    {
      using (Image<Bgr, byte> alignImg = new Image<Bgr, byte>(imageToAlign))
      using (Image<Bgr, byte> refImg = new Image<Bgr, byte>(referenceImage))
      using (Emgu.CV.Mat alignMat = alignImg.Mat)
      using (Emgu.CV.Mat refMat = refImg.Mat)
      {
        using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
        {
          FindMatches(alignMat, refMat, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, matches, out Mat mask, out Mat homography, MatchingTechnique.SURF, 1f);

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

    /// <summary>
    /// Gets the x and y offsets between two images.
    /// </summary>
    /// <param name="modelImage">The model image</param>
    /// <param name="observedImage">The observed image</param>
    /// <returns>A tuple of the x and y offsets between the modelImage and the observedImage.</returns>
    public static Tuple<int,int> GetXYOffsets(Mat modelImage, Mat observedImage)
    {
      Mat homography;
      VectorOfKeyPoint modelKeyPoints;
      VectorOfKeyPoint observedKeyPoints;
      using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
      {
        Mat mask;
        FindMatches(modelImage, observedImage, out modelKeyPoints, out observedKeyPoints, matches, out mask, out homography, MatchingTechnique.FAST, 500);

        var maskMatrix = new Matrix<byte>(mask.Rows, mask.Cols);
        mask.CopyTo(maskMatrix);
        var maskMA = maskMatrix.ManagedArray;
        var maskList = maskMA.OfType<byte>().ToList();

        Dictionary<float, int> distCounts = new Dictionary<float, int>();
        Dictionary<float, Tuple<List<float>, List<float>>> distOffsets = new Dictionary<float, Tuple<List<float>, List<float>>>();
        for (int i = 0; i < matches.Size; i++)
        {
          // Skip matches not verified in the mask.
          if (maskList[i] == 0)
            continue;

          for (int j = 0; j < matches[i].Size; j++)
          {
            float dist = (float)Math.Round(matches[i][j].Distance * 100) / 100;

            int queryIdx = matches[i][j].QueryIdx;
            int trainIdx = matches[i][j].TrainIdx;

            float modelX = modelKeyPoints[trainIdx].Point.X;
            float modelY = modelKeyPoints[trainIdx].Point.Y;
            float obsX = observedKeyPoints[queryIdx].Point.X;
            float obsY = observedKeyPoints[queryIdx].Point.Y;

            float offsetX = modelX - obsX;
            float offsetY = modelY - obsY;

            if (distCounts.ContainsKey(dist))
            {
              distCounts[dist] += 1;
            }
            else
            {
              distCounts.Add(dist, 1);
              distOffsets[dist] = new Tuple<List<float>, List<float>>(new List<float>(), new List<float>());
            }

            distOffsets[dist].Item1.Add(offsetX);
            distOffsets[dist].Item2.Add(offsetY);
          }
        }

        KeyValuePair<float, int> commonDistance = new KeyValuePair<float, int>(0, 0);
        foreach (var kvp in distCounts)
        {
          //Console.WriteLine($"Dist: {kvp.Key}. Occurrences: {kvp.Value}. Offset: {(int)Math.Round(distOffsets[kvp.Key].Item1.Average())}, {(int)Math.Round(distOffsets[kvp.Key].Item2.Average())}");
          if (kvp.Value > commonDistance.Value)
            commonDistance = kvp;
        }
        Console.WriteLine($"Matches: {matches.Size}. CommonDistance {commonDistance.Key} occurrences {commonDistance.Value}");

        return new Tuple<int, int>((int)Math.Round(distOffsets[commonDistance.Key].Item1.Average()), (int)Math.Round(distOffsets[commonDistance.Key].Item2.Average()));
      }
    }

    /// <summary>
    /// Gets the x and y offsets between two images.
    /// </summary>
    /// <param name="modelImage">The model image.</param>
    /// <param name="observedImage">The observed image.</param>
    /// <returns>A tuple of the x and y offsets between the modelImage and the observedImage.</returns>
    public static Tuple<int, int> GetXYOffsets(System.Drawing.Bitmap modelImage, System.Drawing.Bitmap observedImage)
    {
      using (Image<Bgr, byte> img1 = new Image<Bgr, byte>(modelImage))
      using (Image<Bgr, byte> img2 = new Image<Bgr, byte>(observedImage))
      using (Emgu.CV.Mat mat1 = img1.Mat)
      using (Emgu.CV.Mat mat2 = img2.Mat)
      {
        return GetXYOffsets(mat1, mat2);
      }
    }

    /// <summary>
    /// Gets the x and y offsets between two images.
    /// </summary>
    /// <param name="modelImagePath">The path to the model image.</param>
    /// <param name="observedImagePath">The path to the observed image.</param>
    /// <returns>A tuple of the x and y offsets between the modelImage and the observedImage.</returns>
    public static Tuple<int, int> GetXYOffsets(string modelImagePath, string observedImagePath)
    {
      using (Emgu.CV.Mat mat1 = Emgu.CV.CvInvoke.Imread(modelImagePath, Emgu.CV.CvEnum.LoadImageType.AnyColor))
      using (Emgu.CV.Mat mat2 = Emgu.CV.CvInvoke.Imread(observedImagePath, Emgu.CV.CvEnum.LoadImageType.AnyColor))
      {
        return GetXYOffsets(mat1, mat2);
      }
    }

    private static VectorOfKeyPoint GetBestKeypointsCount(VectorOfKeyPoint keyPoints, int count)
    {
      List<MKeyPoint> kpList = keyPoints.ToArray().ToList();
      kpList.Sort((x, y) => x.Response > y.Response ? 1 : x.Response < y.Response ? -1 : 0);
      kpList = kpList.Take(count).ToList();
      return new VectorOfKeyPoint(kpList.ToArray());
    }

    private static VectorOfKeyPoint GetBestKeypointsPercent(VectorOfKeyPoint keyPoints, float percent)
    {
      if (percent < 0 || percent > 1)
        throw new ArgumentOutOfRangeException(nameof(percent));

      if (percent == 1)
        return keyPoints;

      int count = (int)Math.Round(keyPoints.Size * percent);
      return GetBestKeypointsCount(keyPoints, count);
    }

    private static Bitmap ToBitmap(this Mat mat)
    {
      using (Image<Bgr, byte> img = mat.ToImage<Bgr, byte>())
      {
        return img.ToBitmap();
      }
    }
  }
}