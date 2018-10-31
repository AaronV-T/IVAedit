using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IVAE.MediaManipulation;
using System.Drawing;

namespace IVAE.UnitTests
{
  [TestClass]
  public class ImageFeatureDetectorTester
  {
    const string TestImagesDirectoryPath = "TestImages\\";

    [TestMethod]
    public void GetNumberFromImage_Test()
    {
      Assert.AreEqual(7, ImageFeatureDetector.GetNumberFromImage(TestImagesDirectoryPath + "GetNumberFromImage7.png"));
      Assert.AreEqual(96, ImageFeatureDetector.GetNumberFromImage(TestImagesDirectoryPath + "GetNumberFromImage96.png"));
      Assert.AreEqual(148, ImageFeatureDetector.GetNumberFromImage(TestImagesDirectoryPath + "GetNumberFromImage148.png"));
    }

    [TestMethod]
    public void GetXYOffsets_Test()
    {
      using (Bitmap img1 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets1_1.png"))
      using (Bitmap img2 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets1_2.png"))
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(img1, img2);
        Assert.AreEqual(688, offsets.Item1);
        Assert.AreEqual(123, offsets.Item2);
      }

      using (Bitmap img1 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets2_1.png"))
      using (Bitmap img2 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets2_2.png"))
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(img1, img2);
        Assert.AreEqual(0, offsets.Item1);
        Assert.AreEqual(263, offsets.Item2);
      }

      using (Bitmap img1 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets3_1.png"))
      using (Bitmap img2 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets3_2.png"))
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(img1, img2);
        Assert.AreEqual(0, offsets.Item1);
        Assert.AreEqual(334, offsets.Item2);
      }

      using (Bitmap img1 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets4_1.png"))
      using (Bitmap img2 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets4_2.png"))
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(img1, img2);
        Assert.AreEqual(0, offsets.Item1);
        Assert.AreEqual(900, offsets.Item2);
      }

      using (Bitmap img1 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets5_1.png"))
      using (Bitmap img2 = new Bitmap(TestImagesDirectoryPath + "GetXYOffsets5_2.png"))
      {
        Tuple<int, int> offsets = ImageFeatureDetector.GetXYOffsets(img1, img2);
        Assert.AreEqual(677, offsets.Item1);
        Assert.AreEqual(131, offsets.Item2);
      }
    }
  }
}
