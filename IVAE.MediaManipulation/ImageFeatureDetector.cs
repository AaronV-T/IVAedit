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

  public enum ImageAlignmentType { CROP = 0, FASTWARP = 1, FULLWARP = 2 }

  public static class ImageFeatureDetector
  {
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
            detectorParameter = 100000;

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

        Tuple<float, Tuple<List<float>, List<float>>> lowestDistanceOffsets = new Tuple<float, Tuple<List<float>, List<float>>>(float.MaxValue, null);
        for (int i = 0; i < matches.Size; i++)
        {
          // Skip matches not verified in the mask.
          if (maskList[i] == 0)
            continue;

          for (int j = 0; j < matches[i].Size; j++)
          {
            float dist = (float)Math.Round(matches[i][j].Distance * 100) / 100;

            if (dist > lowestDistanceOffsets.Item1)
              continue;
            else if (dist < lowestDistanceOffsets.Item1)
              lowestDistanceOffsets = new Tuple<float, Tuple<List<float>, List<float>>>(dist, new Tuple<List<float>, List<float>>(new List<float>(), new List<float>()));

            int queryIdx = matches[i][j].QueryIdx;
            int trainIdx = matches[i][j].TrainIdx;

            float modelX = modelKeyPoints[trainIdx].Point.X;
            float modelY = modelKeyPoints[trainIdx].Point.Y;
            float obsX = observedKeyPoints[queryIdx].Point.X;
            float obsY = observedKeyPoints[queryIdx].Point.Y;

            float offsetX = modelX - obsX;
            float offsetY = modelY - obsY;

            lowestDistanceOffsets.Item2.Item1.Add(offsetX);
            lowestDistanceOffsets.Item2.Item2.Add(offsetY);
          }
        }

        Console.WriteLine($"{(int)Math.Round(lowestDistanceOffsets.Item2.Item1.Average())} {(int)Math.Round(lowestDistanceOffsets.Item2.Item2.Average())}");

        return new Tuple<int, int>((int)Math.Round(lowestDistanceOffsets.Item2.Item1.Average()), (int)Math.Round(lowestDistanceOffsets.Item2.Item2.Average()));
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
      using (Mat mat1 = img1.Mat)
      using (Mat mat2 = img2.Mat)
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

    public static Bitmap ToBitmap(this Mat mat)
    {
      using (Image<Bgr, byte> img = mat.ToImage<Bgr, byte>())
      {
        return img.ToBitmap();
      }
    }
  }
}