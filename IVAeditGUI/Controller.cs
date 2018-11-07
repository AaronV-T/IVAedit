using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAeditGUI
{
  public class Controller
  {
    private string CurrentStep { get; set; }
    private MainWindow mainWindow;

    public Controller(MainWindow mainWindow)
    {
      this.mainWindow = mainWindow;

      this.mainWindow.OnAdjustVolumeButtonClick += AdjustVolume;
      this.mainWindow.OnAlignImageButtonClick += AlignImage;
      this.mainWindow.OnChangeSpeedButtonClick += ChangeSpeed;
      this.mainWindow.OnCombineGifsButtonClick += CombineGifs;
      this.mainWindow.OnCombineVideosButtonClick += CombineVideos;
      this.mainWindow.OnCropButtonClick += Crop;
      this.mainWindow.OnDrawMatchesButtonClick += DrawMatches;
      this.mainWindow.OnGifToVideoButtonClick += ConvertGifToVideo;
      this.mainWindow.OnImagesToGifButtonClick += ConvertImagesToGif;
      this.mainWindow.OnExtractAudioButtonClick += ExtractAudio;
      this.mainWindow.OnNormalizeVolumeButtonClick += NormalizeVolume;
      this.mainWindow.OnResizeButtonClick += Resize;
      this.mainWindow.OnStabilizeVideoButtonClick += StabilizeVideo;
      this.mainWindow.OnStitchImagesButtonClick += StitchImages;
      this.mainWindow.OnTestButtonClick += Test;
      this.mainWindow.OnTrimButtonClick += Trim;
      this.mainWindow.OnTwwToMp4ButtonClick += TwwToMp4;
      this.mainWindow.OnVideoToImagesButtonClick += ConvertVideoToImages;

      try
      {
        foreach (IVAE.MediaManipulation.ImageAlignmentType type in (IVAE.MediaManipulation.ImageAlignmentType[])Enum.GetValues(typeof(IVAE.MediaManipulation.ImageAlignmentType)))
          this.mainWindow.cbImageAlignmentType.Items.Add(type.ToString());
        this.mainWindow.cbImageAlignmentType.SelectedIndex = 0;

        Dictionary<string, string> settings = SettingsIO.LoadSettings();
        if (settings.ContainsKey("XCoordinate"))
          mainWindow.tbxXCoordinate.Text = settings["XCoordinate"];
        if (settings.ContainsKey("YCoordinate"))
          mainWindow.tbxYCoordinate.Text = settings["YCoordinate"];
        if (settings.ContainsKey("Width"))
          mainWindow.tbxWidth.Text = settings["Width"];
        if (settings.ContainsKey("Height"))
          mainWindow.tbxHeight.Text = settings["Height"];
        if (settings.ContainsKey("FrameDelay"))
          mainWindow.tbxFrameDelay.Text = settings["FrameDelay"];
        if (settings.ContainsKey("FinalDelay"))
          mainWindow.tbxFinalDelay.Text = settings["FinalDelay"];
        if (settings.ContainsKey("Loops"))
          mainWindow.tbxLoops.Text = settings["Loops"];
        if (settings.ContainsKey("WriteFileNames"))
          mainWindow.checkboxWriteFileNames.IsChecked = Convert.ToBoolean(settings["WriteFileNames"]);
        if (settings.ContainsKey("FontSize"))
          mainWindow.tbxFontSize.Text = settings["FontSize"];
        if (settings.ContainsKey("GifsPerLine"))
          mainWindow.tbxGifsPerLine.Text = settings["GifsPerLine"];
        if (settings.ContainsKey("AlignImages"))
          mainWindow.checkboxAlignImages.IsChecked = Convert.ToBoolean(settings["AlignImages"]);
        if (settings.ContainsKey("ImageAlignmentType"))
          mainWindow.cbImageAlignmentType.SelectedValue = settings["ImageAlignmentType"];
        if (settings.ContainsKey("StartTime"))
          mainWindow.tbxStartTime.Text = settings["StartTime"];
        if (settings.ContainsKey("EndTime"))
          mainWindow.tbxEndTime.Text = settings["EndTime"];
        if (settings.ContainsKey("Volume"))
          mainWindow.tbxVolume.Text = settings["Volume"];
        if (settings.ContainsKey("FPS"))
          mainWindow.tbxFPS.Text = settings["FPS"];
        if (settings.ContainsKey("Modifier"))
          mainWindow.tbxModifier.Text = settings["Modifier"];
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message}");
        Console.WriteLine(ex);
      }
    }

    public async void AdjustVolume()
    {
      try
      {
        string volume = mainWindow.tbxVolume.Text;

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "Volume", volume }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Audio or Video File|{GetAudioFormatsFilterString()}{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select audio or video file.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.AdjustVolume(openFileDialog.FileName, volume);
        });

        mainWindow.SetMessage($"File with ajusted volume created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void AlignImage()
    {
      try
      {
        mainWindow.SetMessage("Aligning image.");

        IVAE.MediaManipulation.ImageAlignmentType imageAlignmentType;
        if (!Enum.TryParse(mainWindow.cbImageAlignmentType.SelectedItem.ToString(), out imageAlignmentType))
          throw new ArgumentException($"Image alignment type is not valid.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "ImageAlignmentType", imageAlignmentType.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog1.Filter = $"Images|{GetImageFormatsFilterString()}";
        openFileDialog1.Title = "Select image to align.";
        openFileDialog1.Multiselect = false;

        if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        System.Windows.Forms.OpenFileDialog openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog2.Filter = $"Images|{GetImageFormatsFilterString()}";
        openFileDialog2.Title = "Select reference image.";
        openFileDialog2.Multiselect = false;

        if (openFileDialog2.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.AlignImage(openFileDialog1.FileName, openFileDialog2.FileName, imageAlignmentType);
        });

        mainWindow.SetMessage($"Image aligned '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");

        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void ChangeSpeed()
    {
      try
      {
        float fps = 0, playbackRate = 0;
        if (mainWindow.tbxFPS.Text != string.Empty && !float.TryParse(mainWindow.tbxFPS.Text, out fps))
          throw new ArgumentException("FPS is not a valid number.");
        if (!float.TryParse(mainWindow.tbxModifier.Text, out playbackRate))
          throw new ArgumentException("Modifier (playback rate) is not a valid number.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "FPS", fps.ToString() },
          { "Modifier", playbackRate.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Audio or Video File|{GetAudioFormatsFilterString()}{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select Video or Audio File.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.ChangeAudioOrVideoPlaybackSpeed(openFileDialog.FileName, playbackRate, fps);
        });

        mainWindow.SetMessage($"File with adjusted speed created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void CombineGifs()
    {
      try
      {
        mainWindow.SetMessage("Combining GIFs.");

        int gifsPerLine = 0;
        if (mainWindow.tbxXCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxGifsPerLine.Text, out gifsPerLine))
          throw new ArgumentException($"Gifs per line is not a valid integer.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "GifsPerLine", gifsPerLine.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = "Gifs|*.gif";
        openFileDialog.Title = "Select GIF files.";
        openFileDialog.Multiselect = true;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        string newGifPath = null;
        await Task.Factory.StartNew(() =>
        {
          newGifPath = taskHandler.CombineGifs(openFileDialog.FileNames, gifsPerLine);
        });

        mainWindow.SetMessage($"Gifs combined: {newGifPath}");

        System.Diagnostics.Process.Start(newGifPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void CombineVideos()
    {
      try
      {
        mainWindow.SetMessage("Combining Videos");

        int modifier = 0;
        if (mainWindow.tbxModifier.Text != string.Empty && !int.TryParse(mainWindow.tbxModifier.Text, out modifier))
          throw new ArgumentException($"Modifier is not a valid integer.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "Modifier", modifier.ToString() }
        });

        bool combineHorizontally = modifier == 0;

        System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog1.Filter = $"Videos|{GetVideoFormatsFilterString()}";
        openFileDialog1.Title = "Select first video.";
        openFileDialog1.Multiselect = false;

        if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        System.Windows.Forms.OpenFileDialog openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog2.Filter = $"Videos|{GetVideoFormatsFilterString()}";
        openFileDialog2.Title = "Select second video.";
        openFileDialog2.Multiselect = false;

        if (openFileDialog2.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.CombineVideos(openFileDialog1.FileName, openFileDialog2.FileName, combineHorizontally);
        });

        mainWindow.SetMessage($"Videos combined '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");

        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void ConvertImagesToGif()
    {
      try
      {
        mainWindow.SetMessage("Creating GIF.");

        int x = 0, y = 0, width = 0, height = 0, frameDelay = 0, finalDelay = 0, loops = 0, fontSize = 0;
        bool writeFileNames, alignImages;
        IVAE.MediaManipulation.ImageAlignmentType imageAlignmentType;
        if (mainWindow.tbxXCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxXCoordinate.Text, out x))
          throw new ArgumentException($"X coordinate is not a valid integer.");
        if (mainWindow.tbxYCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxYCoordinate.Text, out y))
          throw new ArgumentException($"Y coordinate is not a valid integer.");
        if (mainWindow.tbxWidth.Text != string.Empty && !int.TryParse(mainWindow.tbxWidth.Text, out width))
          throw new ArgumentException($"Width is not a valid integer.");
        if (mainWindow.tbxHeight.Text != string.Empty && !int.TryParse(mainWindow.tbxHeight.Text, out height))
          throw new ArgumentException($"Height is not a valid integer.");
        if (mainWindow.tbxFrameDelay.Text != string.Empty && !int.TryParse(mainWindow.tbxFrameDelay.Text, out frameDelay))
          throw new ArgumentException($"Frame delay is not a valid integer.");
        if (mainWindow.tbxFinalDelay.Text != string.Empty && !int.TryParse(mainWindow.tbxFinalDelay.Text, out finalDelay))
          throw new ArgumentException($"Final delay is not a valid integer.");
        if (mainWindow.tbxLoops.Text != string.Empty && !int.TryParse(mainWindow.tbxLoops.Text, out loops))
          throw new ArgumentException($"Loops is not a valid integer.");
        writeFileNames = mainWindow.checkboxWriteFileNames.IsChecked.Value;
        if (mainWindow.tbxFontSize.Text != string.Empty && !int.TryParse(mainWindow.tbxFontSize.Text, out fontSize))
          throw new ArgumentException($"Font size is not a valid integer.");
        alignImages = mainWindow.checkboxAlignImages.IsChecked.Value;
        if (!Enum.TryParse(mainWindow.cbImageAlignmentType.SelectedItem.ToString(), out imageAlignmentType))
          throw new ArgumentException($"Image alignment type is not valid.");

        if (finalDelay == 0)
          finalDelay = frameDelay;

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "XCoordinate", x.ToString() },
          { "YCoordinate", y.ToString() },
          { "Width", width.ToString() },
          { "Height", height.ToString() },
          { "FrameDelay", frameDelay.ToString() },
          { "FinalDelay", finalDelay.ToString() },
          { "Loops", loops.ToString() },
          { "WriteFileNames", writeFileNames.ToString() },
          { "FontSize", fontSize.ToString() },
          { "AlignImages", alignImages.ToString() },
          { "ImageAlignmentType", imageAlignmentType.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Images|{GetImageFormatsFilterString()}";
        openFileDialog.Title = "Select image files.";
        openFileDialog.Multiselect = true;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string gifPath = null;
        await Task.Factory.StartNew(() =>
        {
          gifPath = taskHandler.ConvertImagesToGif(openFileDialog.FileNames, x, y, width, height, frameDelay, finalDelay, loops, fontSize, writeFileNames, alignImages, imageAlignmentType);
        });

        mainWindow.SetMessage($"Gif created '{gifPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(gifPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void ConvertGifToVideo()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = "GIF File|*.gif";
        openFileDialog.Title = "Select GIF.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string gifvPath = null;
        await Task.Factory.StartNew(() =>
        {
          gifvPath = taskHandler.ConvertGifToVideo(openFileDialog.FileName);
        });

        mainWindow.SetMessage($"Gifv created '{gifvPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(gifvPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void ConvertVideoToImages()
    {
      try
      {
        string fps = mainWindow.tbxFPS.Text;

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "FPS", fps.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Video File|{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select video file.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputDirectory = null;
        await Task.Factory.StartNew(() =>
        {
          outputDirectory = taskHandler.ConvertVideoToImages(openFileDialog.FileName, fps);
        });

        mainWindow.SetMessage($"Images created '{outputDirectory}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputDirectory);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void Crop()
    {
      try
      {
        int x = 0, y = 0, width = 0, height = 0;
        if (mainWindow.tbxXCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxXCoordinate.Text, out x))
          throw new ArgumentException($"X coordinate is not a valid integer.");
        if (mainWindow.tbxYCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxYCoordinate.Text, out y))
          throw new ArgumentException($"Y coordinate is not a valid integer.");
        if (mainWindow.tbxWidth.Text != string.Empty && !int.TryParse(mainWindow.tbxWidth.Text, out width))
          throw new ArgumentException($"Width is not a valid integer.");
        if (mainWindow.tbxHeight.Text != string.Empty && !int.TryParse(mainWindow.tbxHeight.Text, out height))
          throw new ArgumentException($"Height is not a valid integer.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "XCoordinate", x.ToString() },
          { "YCoordinate", y.ToString() },
          { "Width", width.ToString() },
          { "Height", height.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Image or Video Files|{GetImageFormatsFilterString()}{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select Image or Video File.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.CropImageOrVideo(openFileDialog.FileName, x, y, width, height);
        });

        mainWindow.SetMessage($"Cropped file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void DrawMatches()
    {
      try
      {
        IVAE.MediaManipulation.ImageAlignmentType imageAlignmentType;
        if (!Enum.TryParse(mainWindow.cbImageAlignmentType.SelectedItem.ToString(), out imageAlignmentType))
          throw new ArgumentException($"Image alignment type is not valid.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "ImageAlignmentType", imageAlignmentType.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog1.Filter = $"Images|{GetImageFormatsFilterString()}";
        openFileDialog1.Title = "Select image 1.";
        openFileDialog1.Multiselect = false;

        if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        System.Windows.Forms.OpenFileDialog openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog2.Filter = $"Images|{GetImageFormatsFilterString()}";
        openFileDialog2.Title = "Select image 2.";
        openFileDialog2.Multiselect = false;

        if (openFileDialog2.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.DrawMatches(openFileDialog1.FileName, openFileDialog2.FileName, imageAlignmentType);
        });

        mainWindow.SetMessage($"Image with matches created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void ExtractAudio()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Video File|{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select video file.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.ExtractAudioFromVideo(openFileDialog.FileName);
        });

        mainWindow.SetMessage($"Extracted audio file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void NormalizeVolume()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Audio or Video File|{GetAudioFormatsFilterString()}{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select Video or Audio File.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.NormalizeVolume(openFileDialog.FileName);
        });

        mainWindow.SetMessage($"File with normalized audio created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void Resize()
    {
      try
      {
        int width = 0, height = 0;
        float scaleFactor = 0;
        if (mainWindow.tbxWidth.Text != string.Empty && !int.TryParse(mainWindow.tbxWidth.Text, out width))
          throw new ArgumentException($"Width is not a valid integer.");
        if (mainWindow.tbxHeight.Text != string.Empty && !int.TryParse(mainWindow.tbxHeight.Text, out height))
          throw new ArgumentException($"Height is not a valid integer.");
        if (mainWindow.tbxModifier.Text != string.Empty && !float.TryParse(mainWindow.tbxModifier.Text, out scaleFactor))
          throw new ArgumentException($"ScaleFactor is not a valid number.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "Width", width.ToString() },
          { "Height", height.ToString() },
          { "Modifier", scaleFactor.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Image or Video Files|{GetImageFormatsFilterString()}{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select Image or Video File.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.ResizeImageOrVideo(openFileDialog.FileName, width, height, scaleFactor);
        });

        mainWindow.SetMessage($"Resized file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void StabilizeVideo()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Video File|{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select Video.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.StabilizeVideo(openFileDialog.FileName);
        });

        mainWindow.SetMessage($"Stabilized video created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void StitchImages()
    {
      try
      {
        mainWindow.SetMessage("Stitching images.");

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Images|{GetImageFormatsFilterString()}";
        openFileDialog.Title = "Select image files.";
        openFileDialog.Multiselect = true;

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.StitchImages(openFileDialog.FileNames);
        });

        mainWindow.SetMessage($"Stitched image created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void Test()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = "Files|*.*";
        openFileDialog.Title = "Select files.";
        openFileDialog.Multiselect = true;

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        DateTime start = DateTime.Now;
        await Task.Factory.StartNew(() =>
        {
          taskHandler.Test(openFileDialog.FileNames);
        });

        mainWindow.SetMessage($"Finished in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void Trim()
    {
      try
      {
        string startTime = mainWindow.tbxStartTime.Text;
        string endTime = mainWindow.tbxEndTime.Text;

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "StartTime", startTime },
          { "EndTime", endTime }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Audio or Video File|{GetAudioFormatsFilterString()}{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select audio or video file.";
        openFileDialog.Multiselect = false;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.TrimAudioOrVideo(openFileDialog.FileName, startTime, endTime);
        });

        mainWindow.SetMessage($"Trimmed video created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void TwwToMp4()
    {
      try
      {
        int x = 0, y = 0, width = 0, height = 0;
        if (mainWindow.tbxXCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxXCoordinate.Text, out x))
          throw new ArgumentException($"X coordinate is not a valid integer.");
        if (mainWindow.tbxYCoordinate.Text != string.Empty && !int.TryParse(mainWindow.tbxYCoordinate.Text, out y))
          throw new ArgumentException($"Y coordinate is not a valid integer.");
        if (mainWindow.tbxWidth.Text != string.Empty && !int.TryParse(mainWindow.tbxWidth.Text, out width))
          throw new ArgumentException($"Width is not a valid integer.");
        if (mainWindow.tbxHeight.Text != string.Empty && !int.TryParse(mainWindow.tbxHeight.Text, out height))
          throw new ArgumentException($"Height is not a valid integer.");

        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "XCoordinate", x.ToString() },
          { "YCoordinate", y.ToString() },
          { "Width", width.ToString() },
          { "Height", height.ToString() }
        });

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = $"Video File|{GetVideoFormatsFilterString()}";
        openFileDialog.Title = "Select Video.";
        openFileDialog.Multiselect = false;

        IVAE.MediaManipulation.TaskHandler taskHandler = new IVAE.MediaManipulation.TaskHandler();
        taskHandler.OnChangeStep += ChangeCurrentStep;
        taskHandler.OnProgressUpdate += ProgressUpdate;

        if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        DateTime start = DateTime.Now;
        string outputPath = null;
        await Task.Factory.StartNew(() =>
        {
          outputPath = taskHandler.TwwToMp4(openFileDialog.FileName, x, y, width, height);
        });

        mainWindow.SetMessage($"TWW Timelapse '{outputPath}' created in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
        System.Diagnostics.Process.Start(outputPath);
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    private void ChangeCurrentStep(string currentStep)
    {
      CurrentStep = currentStep;
      mainWindow.SetMessage($"{CurrentStep}.");
    }

    private void ProgressUpdate(float percent)
    {
      mainWindow.SetMessage($"{CurrentStep}: {percent.ToString("P2")}");
    }

    private string GetAudioFormatsFilterString()
    {
      StringBuilder sb = new StringBuilder();
      foreach (string s in IVAE.MediaManipulation.MediaTypeHelper.AudioExtensions)
        sb.Append($"*{s};");

      return sb.ToString();
    }

    private string GetImageFormatsFilterString()
    {
      StringBuilder sb = new StringBuilder();
      foreach (string s in IVAE.MediaManipulation.MediaTypeHelper.ImageExtensions)
        sb.Append($"*{s};");

      return sb.ToString();
    }

    private string GetVideoFormatsFilterString()
    {
      StringBuilder sb = new StringBuilder();
      foreach (string s in IVAE.MediaManipulation.MediaTypeHelper.VideoExtensions)
        sb.Append($"*{s};");

      return sb.ToString();
    }
  }
}
