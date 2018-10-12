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

      this.mainWindow.OnAlignImageButtonClick += AlignImage;
      this.mainWindow.OnCombineGifsButtonClick += CombineGifs;
      this.mainWindow.OnGifToGifvButtonClick += ConvertGifToGifv;
      this.mainWindow.OnImagesToGifButtonClick += ConvertImagesToGif;
      this.mainWindow.OnNormalizeVolumeButtonClick += NormalizeVolume;
      this.mainWindow.OnStabilizeVideoButtonClick += StabilizeVideo;
      this.mainWindow.OnStitchImagesButtonClick += StitchImages;
      this.mainWindow.OnTestButtonClick += Test;
      this.mainWindow.OnTrimVideoButtonClick += Trimvideo;

      try
      {
        foreach (IVAE.MediaManipulation.ImageAlignmentType type in (IVAE.MediaManipulation.ImageAlignmentType[])Enum.GetValues(typeof(IVAE.MediaManipulation.ImageAlignmentType)))
          this.mainWindow.cbImageAlignmentType.Items.Add(type.ToString());
        this.mainWindow.cbImageAlignmentType.SelectedIndex = 0;

        Dictionary<string, string> settings = SettingsIO.LoadSettings(/*new List<string> { "XCoordinate", "YCoordinate", "Width", "Height", "FrameDelay", "FinalDelay", "Loops", "WriteFileNames", "FontSize", "GifsPerLine", "AlignImages", "ImageAlignmentType", "StartTime", "EndTime" }*/);
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
          mainWindow.checkboxAlignImages.IsChecked= Convert.ToBoolean(settings["AlignImages"]);
        if (settings.ContainsKey("ImageAlignmentType"))
          mainWindow.cbImageAlignmentType.SelectedValue = settings["ImageAlignmentType"];
        if (settings.ContainsKey("StartTime"))
          mainWindow.tbxStartTime.Text = settings["StartTime"];
        if (settings.ContainsKey("EndTime"))
          mainWindow.tbxEndTime.Text = settings["EndTime"];
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message}");
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
        openFileDialog1.Filter = "Images (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG";
        openFileDialog1.Title = "Select image to align.";
        openFileDialog1.Multiselect = false;

        if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
          mainWindow.SetMessage("Canceled.");
          return;
        }

        System.Windows.Forms.OpenFileDialog openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
        openFileDialog2.Filter = "Images (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG";
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
        openFileDialog.Filter = "Gifs|*.GIF";
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
        openFileDialog.Filter = "Images (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG";
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

    public async void ConvertGifToGifv()
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
          gifvPath = taskHandler.ConvertGifToGifv(openFileDialog.FileName);
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

    public async void NormalizeVolume()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = "Audio or Video File|*.avi;*.mov;*.mp3;*.mp4;*.webm";
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

    public async void StabilizeVideo()
    {
      try
      {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        openFileDialog.Filter = "Video File|*.avi;*.mov;*.mp4;*.webm";
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
        openFileDialog.Filter = "Images (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG";
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

        await Task.Factory.StartNew(() =>
        {
          taskHandler.Test(openFileDialog.FileNames);
        });

        mainWindow.SetMessage($"Finished.");
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void Trimvideo()
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
        openFileDialog.Filter = "Video File|*.avi;*.mov;*.mp4;*.webm";
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
          outputPath = taskHandler.TrimVideo(openFileDialog.FileName, startTime, endTime);
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

    private void ChangeCurrentStep(string currentStep)
    {
      CurrentStep = currentStep;
      mainWindow.SetMessage($"{CurrentStep}.");
    }

    private void ProgressUpdate(float percent)
    {
      mainWindow.SetMessage($"{CurrentStep}: {percent.ToString("P2")}");
    }
  }
}
