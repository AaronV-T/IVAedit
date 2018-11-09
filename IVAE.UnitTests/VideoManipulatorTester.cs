using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IVAE.MediaManipulation;
using System.Drawing;

namespace IVAE.UnitTests
{
  [TestClass]
  public class VideoManipulatorTester
  {
    const string TestMediaDirectory = "TestMediaFiles\\";

    [TestMethod]
    public void AdjustVideoSpeedTest()
    {
      Action<VideoManipulator, MediaFileInfo, string, string, float, float, bool> changeVideoSpeedTestHelper = delegate (VideoManipulator vm, MediaFileInfo mfi, string inputPath, string outputPath, float newPlaybackRate, float newFrameRate, bool alsoChangeAudio)
      {
        double inputDuration = mfi.Format.Duration.Value;
        vm.AdjustVideoSpeed(outputPath, inputPath, newPlaybackRate, newFrameRate, alsoChangeAudio);
        double outputDuration = new MediaFileInfo(outputPath).Format.Duration.Value;
        double expectedDuration = inputDuration / newPlaybackRate;
        Assert.IsTrue(Math.Min(expectedDuration, outputDuration) / Math.Max(expectedDuration, outputDuration) > 0.95);
      };

      string inputFilepath = $"{TestMediaDirectory}twwbattles1.mp4";
      string outputFilepath = $"{TestMediaDirectory}output.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();
      MediaFileInfo mfiInput = new MediaFileInfo(inputFilepath);

      changeVideoSpeedTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, 0.4f, 0, false);
      changeVideoSpeedTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, 3, 0, false);

      inputFilepath = $"{TestMediaDirectory}twwbattlesraw.mp4";
      mfiInput = new MediaFileInfo(inputFilepath);
      changeVideoSpeedTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, 0.5f, 0, true);
      changeVideoSpeedTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, 2, 0, true);
      try
      {
        changeVideoSpeedTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, 0.4f, 0, true);
        throw new Exception();
      } catch (ArgumentException) { }
      try
      {
        changeVideoSpeedTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, 3, 0, true);
        throw new Exception();
      }
      catch (ArgumentException) { }
    }

    [TestMethod]
    public void CombineVideosTest()
    {
      Action<VideoManipulator, MediaFileInfo, MediaFileInfo, string, string, string, bool> combineVideosTestHelper = delegate (VideoManipulator vm, MediaFileInfo mf1, MediaFileInfo mf2, string inputPath1, string inputPath2, string outputPath, bool combineHorizontally)
      {
        int input1Dimension, input2Dimension;
        if (combineHorizontally)
        {
          input1Dimension = mf1.VideoStreams[0].Width.Value;
          input2Dimension = mf2.VideoStreams[0].Width.Value;
        }
        else
        {
          input1Dimension = mf1.VideoStreams[0].Height.Value;
          input2Dimension = mf2.VideoStreams[0].Height.Value;
        }

        vm.CombineVideos(outputPath, inputPath1, inputPath2, combineHorizontally);
        if (input1Dimension == input2Dimension)
        {
          MediaFileInfo mfiOutput = new MediaFileInfo(outputPath);
          int outputDimension;
          if (combineHorizontally)
            outputDimension = mfiOutput.VideoStreams[0].Width.Value;
          else
            outputDimension = mfiOutput.VideoStreams[0].Height.Value;

          Assert.AreEqual(input1Dimension * 2, outputDimension);
        }
        else
          Assert.AreEqual(0, new System.IO.FileInfo(outputPath).Length);
      };

      string inputFilepath1 = $"{TestMediaDirectory}twwbattles1.mp4";
      string inputFilepath2 = $"{TestMediaDirectory}twwbattles2.mp4";
      string outputFilepath = $"{TestMediaDirectory}output.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();
      MediaFileInfo mfiInput1 = new MediaFileInfo(inputFilepath1);
      MediaFileInfo mfiInput2 = new MediaFileInfo(inputFilepath2);

      combineVideosTestHelper(videoManipulator, mfiInput1, mfiInput2, inputFilepath1, inputFilepath2, outputFilepath, false);
      combineVideosTestHelper(videoManipulator, mfiInput1, mfiInput2, inputFilepath1, inputFilepath2, outputFilepath, true);
    }

    [TestMethod]
    public void CropVideoTest()
    {
      string inputFilepath = $"{TestMediaDirectory}twwbattles1.mp4";
      string outputFilepath = $"{TestMediaDirectory}output.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();

      videoManipulator.CropVideo(outputFilepath, inputFilepath, 0, 0, 100, 150);
      MediaFileInfo outputMFI = new MediaFileInfo(outputFilepath);
      Assert.AreEqual(100, outputMFI.VideoStreams[0].Width);
      Assert.AreEqual(150, outputMFI.VideoStreams[0].Height);
    }

    [TestMethod]
    public void ExtractAudioFromVideoTest()
    {
      string inputFilepath = $"{TestMediaDirectory}twwraw.mp4";
      string outputFilepathWithoutExtension = $"{TestMediaDirectory}output";
      VideoManipulator videoManipulator = new VideoManipulator();
      MediaFileInfo inputMFI = new MediaFileInfo(inputFilepath);

      MediaFileInfo outputMFI = new MediaFileInfo(videoManipulator.ExtractAudioFromVideo(outputFilepathWithoutExtension, inputFilepath));
      Assert.IsTrue(outputMFI.HasAudio);
      Assert.IsFalse(outputMFI.HasVideo);
    }

    [TestMethod]
    public void ResizeVideoTest()
    {
      string inputFilepath = $"{TestMediaDirectory}twwbattles1.mp4";
      string outputFilepath = $"{TestMediaDirectory}output.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();
      MediaFileInfo inputMFI = new MediaFileInfo(inputFilepath);

      videoManipulator.ResizeVideo(outputFilepath, inputFilepath, 300, 0);
      MediaFileInfo outputMFI = new MediaFileInfo(outputFilepath);
      Assert.AreEqual(300, outputMFI.VideoStreams[0].Width);
      Assert.AreEqual((int)Math.Floor((((double)300 / inputMFI.VideoStreams[0].Width) * inputMFI.VideoStreams[0].Height).Value), outputMFI.VideoStreams[0].Height.Value);

      videoManipulator.ResizeVideo(outputFilepath, inputFilepath, 0, 300);
      outputMFI = new MediaFileInfo(outputFilepath);
      Assert.AreEqual(300, outputMFI.VideoStreams[0].Height);
      Assert.AreEqual((int)Math.Floor((((double)300 / inputMFI.VideoStreams[0].Height) * inputMFI.VideoStreams[0].Width).Value), outputMFI.VideoStreams[0].Width.Value);
    }

    [TestMethod]
    public void ScaleVideoTest()
    {
      string inputFilepath = $"{TestMediaDirectory}twwbattles1.mp4";
      string outputFilepath = $"{TestMediaDirectory}output.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();
      MediaFileInfo inputMFI = new MediaFileInfo(inputFilepath);
      MediaFileInfo outputMFI;
      float modifier;

      modifier = 0.5f;
      videoManipulator.ScaleVideo(outputFilepath, inputFilepath, modifier);
      outputMFI = new MediaFileInfo(outputFilepath);
      Assert.AreEqual((int)(inputMFI.VideoStreams[0].Width * modifier) / 2 * 2, outputMFI.VideoStreams[0].Width);
      Assert.AreEqual((int)(inputMFI.VideoStreams[0].Height * modifier) / 2 * 2, outputMFI.VideoStreams[0].Height);

      modifier = 2;
      videoManipulator.ScaleVideo(outputFilepath, inputFilepath, 2);
      outputMFI = new MediaFileInfo(outputFilepath);
      Assert.AreEqual((int)(inputMFI.VideoStreams[0].Width * modifier) / 2 * 2, outputMFI.VideoStreams[0].Width);
      Assert.AreEqual((int)(inputMFI.VideoStreams[0].Height * modifier) / 2 * 2, outputMFI.VideoStreams[0].Height);
    }

    [TestMethod]
    public void TrimTest()
    {
      Action<VideoManipulator, MediaFileInfo, string, string, string, string, double> trimTestHelper = delegate (VideoManipulator vm, MediaFileInfo mfi, string inputPath, string outputPath, string startTime, string endTime, double expectedDuration)
      {
        double inputDuration = mfi.Format.Duration.Value;
        vm.Trim(outputPath, inputPath, startTime, endTime);
        double outputDuration = new MediaFileInfo(outputPath).Format.Duration.Value;
        Assert.AreEqual(expectedDuration, outputDuration);
      };

      string inputFilepath = $"{TestMediaDirectory}twwbattles1.mp4";
      string outputFilepath = $"{TestMediaDirectory}output.mp4";
      VideoManipulator videoManipulator = new VideoManipulator();
      MediaFileInfo mfiInput = new MediaFileInfo(inputFilepath);

      trimTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, "0", "3", 3);
      trimTestHelper(videoManipulator, mfiInput, inputFilepath, outputFilepath, "1", "5", 4);
    }
  }
}
