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
  public static class ImageFeatureDetector
  {
    public static void FindMatchesFast(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
    {
      Stopwatch watch = Stopwatch.StartNew();

      int k = 2;
      double uniquenessThreshold = 0.8;

      homography = null;
      modelKeyPoints = new VectorOfKeyPoint();
      observedKeyPoints = new VectorOfKeyPoint();

      FastDetector fastCPU = new FastDetector(20, true);
      BriefDescriptorExtractor descriptor = new BriefDescriptorExtractor();

      // Extract features from model image.
      UMat modelDescriptors = new UMat();
      fastCPU.DetectRaw(modelImage, modelKeyPoints, null);
      modelKeyPoints = GetBestKeypoints(modelKeyPoints, 500);
      descriptor.Compute(modelImage, modelKeyPoints, modelDescriptors);

      // Extract features from observed image.
      UMat observedDescriptors = new UMat();
      fastCPU.DetectRaw(observedImage, observedKeyPoints, null);
      observedKeyPoints = GetBestKeypoints(observedKeyPoints, 500);
      descriptor.Compute(observedImage, observedKeyPoints, observedDescriptors);

      BFMatcher matcher = new BFMatcher(DistanceType.Hamming);
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

      watch.Stop();
      matchTime = watch.ElapsedMilliseconds;
    }

    public static void FindMatchesSurf(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
    {
      Stopwatch watch = Stopwatch.StartNew();

      int k = 2;
      double uniquenessThreshold = 0.8;
      double hessianThresh = 300;

      homography = null;
      modelKeyPoints = new VectorOfKeyPoint();
      observedKeyPoints = new VectorOfKeyPoint();

#if !__IOS__
      if (CudaInvoke.HasCuda)
      {
        CudaSURF surfCuda = new CudaSURF((float)hessianThresh);
        using (GpuMat gpuModelImage = new GpuMat(modelImage))
        //extract features from the object image
        using (GpuMat gpuModelKeyPoints = surfCuda.DetectKeyPointsRaw(gpuModelImage, null))
        using (GpuMat gpuModelDescriptors = surfCuda.ComputeDescriptorsRaw(gpuModelImage, null, gpuModelKeyPoints))
        using (CudaBFMatcher matcher = new CudaBFMatcher(DistanceType.L2))
        {
          surfCuda.DownloadKeypoints(gpuModelKeyPoints, modelKeyPoints);

          // extract features from the observed image
          using (GpuMat gpuObservedImage = new GpuMat(observedImage))
          using (GpuMat gpuObservedKeyPoints = surfCuda.DetectKeyPointsRaw(gpuObservedImage, null))
          using (GpuMat gpuObservedDescriptors = surfCuda.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
          //using (GpuMat tmp = new GpuMat())
          //using (Stream stream = new Stream())
          {
            matcher.KnnMatch(gpuObservedDescriptors, gpuModelDescriptors, matches, k);

            surfCuda.DownloadKeypoints(gpuObservedKeyPoints, observedKeyPoints);

            mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
            mask.SetTo(new MCvScalar(255));
            Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

            int nonZeroCount = CvInvoke.CountNonZero(mask);
            if (nonZeroCount >= 4)
            {
              nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                 matches, mask, 1.5, 20);
              if (nonZeroCount >= 4)
                homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                   observedKeyPoints, matches, mask, 2);
            }
          }
          watch.Stop();
        }
      }
      else
#endif
      {
        using (UMat uModelImage = modelImage.ToUMat(AccessType.Read))
        using (UMat uObservedImage = observedImage.ToUMat(AccessType.Read))
        {
          SURF surfCPU = new SURF(hessianThresh);
          //extract features from the object image
          UMat modelDescriptors = new UMat();
          surfCPU.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);

          // extract features from the observed image
          UMat observedDescriptors = new UMat();
          surfCPU.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);
          BFMatcher matcher = new BFMatcher(DistanceType.L2);
          matcher.Add(modelDescriptors);

          matcher.KnnMatch(observedDescriptors, matches, k, null);
          mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
          mask.SetTo(new MCvScalar(255));
          Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

          int nonZeroCount = CvInvoke.CountNonZero(mask);
          if (nonZeroCount >= 4)
          {
            nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
               matches, mask, 1.5, 20);
            if (nonZeroCount >= 4)
              homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                 observedKeyPoints, matches, mask, 2);
          }

          watch.Stop();
        }
      }
      matchTime = watch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Draw the model image and observed image, the matched features and homography projection.
    /// </summary>
    /// <param name="modelImage">The model image</param>
    /// <param name="observedImage">The observed image</param>
    /// <param name="matchTime">The output total time for computing the homography matrix.</param>
    /// <returns>The model image and observed image, the matched features and homography projection.</returns>
    public static Mat DrawMatchesSurf(Mat modelImage, Mat observedImage, out long matchTime)
    {
      Mat homography;
      VectorOfKeyPoint modelKeyPoints;
      VectorOfKeyPoint observedKeyPoints;
      using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
      {
        Mat mask;
        FindMatchesSurf(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches,
           out mask, out homography);

        //Draw the matched keypoints
        Mat result = new Mat();
        Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
           matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);

        #region draw the projected region on the image

        if (homography != null)
        {
          //draw a rectangle along the projected model
          Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
          PointF[] pts = new PointF[]
          {
                  new PointF(rect.Left, rect.Bottom),
                  new PointF(rect.Right, rect.Bottom),
                  new PointF(rect.Right, rect.Top),
                  new PointF(rect.Left, rect.Top)
          };
          pts = CvInvoke.PerspectiveTransform(pts, homography);

          Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
          using (VectorOfPoint vp = new VectorOfPoint(points))
          {
            CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
          }

        }

        #endregion

        return result;
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
        long matchTime;
        FindMatchesFast(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches, out mask, out homography);

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

    private static VectorOfKeyPoint GetBestKeypoints(VectorOfKeyPoint keyPoints, int count)
    {
      List<MKeyPoint> kpList = keyPoints.ToArray().ToList();
      kpList.Sort((x, y) => x.Response > y.Response ? 1 : x.Response < y.Response ? -1 : 0);
      kpList = kpList.Take(count).ToList();
      return new VectorOfKeyPoint(kpList.ToArray());
    }
  }
}