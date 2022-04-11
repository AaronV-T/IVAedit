using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IVAeditGUI
{
  public class Controller
  {
    private string CurrentStep { get; set; }
    private MainWindow mainWindow;

    private Dictionary<string, OperationSetupInfo> operationSetups;

    public Controller(MainWindow mainWindow)
    {
      this.mainWindow = mainWindow;

      this.mainWindow.OnOperationSelectorSelectionChanged += OperationSelectionChanged;
      this.mainWindow.OnRunButtonClick += Run;

      operationSetups = new Dictionary<string, OperationSetupInfo>()
      {
        {
          "Adjust Speed",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "FPS", new InputSetupInfo(ControlType.TextBox) },
              { "Speed", new InputSetupInfo(ControlType.TextBox) }
            },
            AdjustSpeed
          )
        },
        {
          "Adjust Volume",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Volume", new InputSetupInfo(ControlType.TextBox) }
            },
            AdjustVolume
          )
        },
        {
          "Align Image",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              {
                "Align Mode", new InputSetupInfo
                (
                  ControlType.ComboBox,
                  ((IVAE.MediaManipulation.ImageAlignmentType[])Enum.GetValues(typeof(IVAE.MediaManipulation.ImageAlignmentType))).Select(item => item.ToString()).ToList()
                )
              }
            },
            AlignImage
          )
        },
        {
          "Combine Gifs",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Gifs Per Line", new InputSetupInfo(ControlType.TextBox) }
            },
            CombineGifs
          )
        },
        {
          "Combine Videos",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Horizontal", new InputSetupInfo(ControlType.CheckBox) }
            },
            CombineVideos
          )
        },
        {
          "Crop",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "X Coordinate", new InputSetupInfo(ControlType.TextBox) },
              { "Y Coordinate", new InputSetupInfo(ControlType.TextBox) },
              { "Width", new InputSetupInfo(ControlType.TextBox) },
              { "Height", new InputSetupInfo(ControlType.TextBox) }
            },
            Crop
          )
        },
        {
          "Draw Matches",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              {
                "Align Mode", new InputSetupInfo
                (
                  ControlType.ComboBox,
                  ((IVAE.MediaManipulation.ImageAlignmentType[])Enum.GetValues(typeof(IVAE.MediaManipulation.ImageAlignmentType))).Select(item => item.ToString()).ToList()
                )
              }
            },
            DrawMatches
          )
        },
        {
          "Extend Video",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Seconds", new InputSetupInfo(ControlType.TextBox) },
            },
            ExtendVideo
          )
        },
        {
          "Extract Audio",
          new OperationSetupInfo(null, ExtractAudio)
        },
        {
          "Images To Gif",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "X Coordinate", new InputSetupInfo(ControlType.TextBox) },
              { "Y Coordinate", new InputSetupInfo(ControlType.TextBox) },
              { "Width", new InputSetupInfo(ControlType.TextBox) },
              { "Height", new InputSetupInfo(ControlType.TextBox) },
              { "Frame Delay", new InputSetupInfo(ControlType.TextBox) },
              { "Final Delay", new InputSetupInfo(ControlType.TextBox) },
              { "Loops", new InputSetupInfo(ControlType.TextBox) },
              { "Write Names", new InputSetupInfo(ControlType.CheckBox)},
              { "Font Size", new InputSetupInfo(ControlType.TextBox) },
              { "Align Images", new InputSetupInfo(ControlType.CheckBox) },
              {
                "Align Mode", new InputSetupInfo
                (
                  ControlType.ComboBox,
                  ((IVAE.MediaManipulation.ImageAlignmentType[])Enum.GetValues(typeof(IVAE.MediaManipulation.ImageAlignmentType))).Select(item => item.ToString()).ToList()
                )
              }
            },
            ImagesToGif
          )
        },
        {
          "Flip",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Horizontal", new InputSetupInfo(ControlType.CheckBox) },
              { "Vertical", new InputSetupInfo(ControlType.CheckBox) }
            },
            Flip
          )
        },
        {
          "Get Screenshot",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "End", new InputSetupInfo(ControlType.CheckBox) },
              { "Time", new InputSetupInfo(ControlType.TextBox) }
            },
            GetScreenshot
          )
        },
        {
          "Gif To Video",
          new OperationSetupInfo(null, GifToVideo)
        },
        {
          "Normalize Volume",
          new OperationSetupInfo(null, NormalizeVolume)
        },
        {
          "Remove Audio",
          new OperationSetupInfo(null, RemoveAudio)
        },
        {
          "Resize",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Width", new InputSetupInfo(ControlType.TextBox) },
              { "Height", new InputSetupInfo(ControlType.TextBox) },
              { "Scale Factor", new InputSetupInfo(ControlType.TextBox) }
            },
            Resize
          )
        },
        {
          "Reverse",
          new OperationSetupInfo(null, Reverse)
        },
        {
          "Rotate",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Counter Clockwise", new InputSetupInfo(ControlType.CheckBox) }
            },
            Rotate
          )
        },
        {
          "Stabilize Video",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Optzoom", new InputSetupInfo(ControlType.TextBox) }
            },
            StabilizeVideo
          )
        },
        {
          "Stitch Images",
          new OperationSetupInfo(null, StitchImages)
        },
        {
          "Test",
          new OperationSetupInfo(null, Test)
        },
        {
          "Trim",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Start Time", new InputSetupInfo(ControlType.TextBox) },
              { "End Time", new InputSetupInfo(ControlType.TextBox) }
            },
            Trim
          )
        },
        {
          "TWW To MP4",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Main X", new InputSetupInfo(ControlType.TextBox) },
              { "Main Y", new InputSetupInfo(ControlType.TextBox) },
              { "Main Width", new InputSetupInfo(ControlType.TextBox) },
              { "Main Height", new InputSetupInfo(ControlType.TextBox) }
            },
            TwwToMp4
          )
        },
        {
          "TWW3 To MP4",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "Timelapse Seconds", new InputSetupInfo(ControlType.TextBox) },
              { "End Seconds", new InputSetupInfo(ControlType.TextBox) },
              { "Map X", new InputSetupInfo(ControlType.TextBox) },
              { "Map Y", new InputSetupInfo(ControlType.TextBox) },
              { "Map Width", new InputSetupInfo(ControlType.TextBox) },
              { "Map Height", new InputSetupInfo(ControlType.TextBox) },
              { "Turn X", new InputSetupInfo(ControlType.TextBox) },
              { "Turn Y", new InputSetupInfo(ControlType.TextBox) },
              { "Turn Width", new InputSetupInfo(ControlType.TextBox) },
              { "Turn Height", new InputSetupInfo(ControlType.TextBox) },
            },
            Tww3ToMp4
          )
        },
        {
          "Video To Images",
          new OperationSetupInfo
          (
            new Dictionary<string, InputSetupInfo>
            {
              { "FPS", new InputSetupInfo(ControlType.TextBox) }
            },
            VideoToImages
          )
        }
      };

      try
      {
        foreach (var kvp in operationSetups)
          this.mainWindow.cbOperationSelector.Items.Add(kvp.Key);

        Dictionary<string, string> settings = SettingsIO.LoadSettings(new List<string> { "SelectedOperation" });
        if (settings.ContainsKey("SelectedOperation"))
          this.mainWindow.cbOperationSelector.SelectedValue = settings["SelectedOperation"];
        else
          this.mainWindow.cbOperationSelector.SelectedIndex = 0;
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message}");
        Console.WriteLine(ex);
      }
    }

    #region GUI Event Handlers
    public void OperationSelectionChanged()
    {
      try
      {
        // Get and save selected operation.
        string selectedOperation = mainWindow.cbOperationSelector.SelectedItem as string;
        SettingsIO.SaveSettings(new Dictionary<string, string> {
          { "SelectedOperation", selectedOperation }
        });

        // Remove UI input controls.
        this.mainWindow.gridInputs.Children.Clear();

        if (!operationSetups.ContainsKey(selectedOperation))
          throw new Exception($"The selected operation '{selectedOperation}' does not have setup information.");

        if (operationSetups[selectedOperation] == null || operationSetups[selectedOperation].InputInfo == null || operationSetups[selectedOperation].InputInfo.Count == 0)
          return;

        // Create UI input controls.
        Dictionary<string, string> savedOperationInputs = SettingsIO.LoadSettingsWithPrefix($"{selectedOperation.Replace(" ", string.Empty)}_");
        float midPoint = (operationSetups[selectedOperation].InputInfo.Count - 1) / (float)2;
        int count = 0;
        foreach (var kvp in operationSetups[selectedOperation].InputInfo)
        {
          string inputSettingName = GetInputSettingName(selectedOperation, kvp.Key);
          string savedInputValue = string.Empty;
          if (savedOperationInputs.ContainsKey(inputSettingName))
            savedInputValue = savedOperationInputs[inputSettingName];

          Thickness gridMargin = new Thickness(160 * (count - midPoint), 0, 0, 0);

          Grid inputGrid = new Grid
          {
            Margin = gridMargin,
            Name = $"inputGrid{inputSettingName}",
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
          };
          mainWindow.gridInputs.Children.Add(inputGrid);

          if (kvp.Value.ControlType == ControlType.CheckBox)
          {
            inputGrid.Children.Add(new Label
            {
              Content = kvp.Key,
              Margin = new Thickness(0, 0, 0, 0),
              Height = 30,
              Width = 80,
              HorizontalAlignment = HorizontalAlignment.Center,
              VerticalAlignment = VerticalAlignment.Top,
              HorizontalContentAlignment = HorizontalAlignment.Center
            });

            bool isChecked = string.IsNullOrWhiteSpace(savedInputValue) ? false : Convert.ToBoolean(savedInputValue);
            CheckBox valueChbx = new CheckBox {
              IsChecked = isChecked,
              Margin = new Thickness(0, 30, 0, 0),
              Height = 25,
              HorizontalAlignment = HorizontalAlignment.Center,
              VerticalAlignment = VerticalAlignment.Top
            };

            valueChbx.Click += (obj, args) => 
            {
              SettingsIO.SaveSettings(new Dictionary<string, string>
              {
                { inputSettingName, valueChbx.IsChecked.ToString() }
              });
            };

            inputGrid.Children.Add(valueChbx);
          }
          else if (kvp.Value.ControlType == ControlType.ComboBox)
          {
            inputGrid.Children.Add(new Label
            {
              Content = kvp.Key,
              Margin = new Thickness(0, 0, 0, 0),
              Height = 30,
              Width = 80,
              HorizontalAlignment = HorizontalAlignment.Center,
              VerticalAlignment = VerticalAlignment.Top,
              HorizontalContentAlignment = HorizontalAlignment.Center
            });

            ComboBox valueCmbx = new ComboBox
            {
              Margin = new Thickness(0, 30, 0, 0),
              Height = 25,
              Width = 70,
              HorizontalAlignment = HorizontalAlignment.Center,
              VerticalAlignment = VerticalAlignment.Top
            };

            foreach (string item in (List<string>)kvp.Value.AdditionalInfo)
              valueCmbx.Items.Add(item);

            if (string.IsNullOrWhiteSpace(savedInputValue))
              valueCmbx.SelectedIndex = 0;
            else
              valueCmbx.SelectedValue = savedInputValue;

            valueCmbx.SelectionChanged += (obj, args) =>
            {
              SettingsIO.SaveSettings(new Dictionary<string, string>
              {
                { inputSettingName, valueCmbx.SelectedItem.ToString() }
              });
            };

            inputGrid.Children.Add(valueCmbx);
          }
          else if (kvp.Value.ControlType == ControlType.TextBox)
          {
            inputGrid.Children.Add(new Label
            {
              Content = kvp.Key,
              Margin = new Thickness(0, 0, 0, 0),
              Height = 30,
              Width = 80,
              HorizontalAlignment = HorizontalAlignment.Center,
              VerticalAlignment = VerticalAlignment.Top,
              HorizontalContentAlignment = HorizontalAlignment.Center
            });

            TextBox valueTbx = new TextBox
            {
              Text = savedInputValue,
              Margin = new Thickness(0, 30, 0, 0),
              Height = 25,
              Width = 50,
              HorizontalAlignment = HorizontalAlignment.Center,
              VerticalAlignment = VerticalAlignment.Top
            };

            valueTbx.LostFocus += (obj, args) =>
            {
              SettingsIO.SaveSettings(new Dictionary<string, string>
              {
                { inputSettingName, valueTbx.Text }
              });
            };

            inputGrid.Children.Add(valueTbx);
          }
          else
            throw new NotImplementedException($"Control type '{kvp.Value.ControlType}' not implemented.");

          count++;
        }
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }

    public async void Run()
    {
      try
      {
        string selectedOperation = mainWindow.cbOperationSelector.SelectedItem as string;

        if (operationSetups == null)
          throw new Exception("OperationSetups dictionary is not initialized.");

        if (!operationSetups.ContainsKey(selectedOperation))
          throw new Exception($"The selected operation '{selectedOperation}' does not have setup information.");

        if (operationSetups[selectedOperation].Func == null)
          throw new Exception("The selected operation does not have an action set.");

        await operationSetups[selectedOperation].Func();
      }
      catch (Exception ex)
      {
        mainWindow.SetMessage($"Error: {ex.Message.Replace(Environment.NewLine, " ")}");
        Console.WriteLine(ex);
      }
    }
    #endregion

    #region Operation Controllers
    private async Task AdjustSpeed()
    {
      const string OP_NAME = "Adjust Speed";
      string fpsInputText = GetInputValue(OP_NAME, "FPS");
      string speedModifierText = GetInputValue(OP_NAME, "Speed");

      float fps = 0, speedModifier = 0;
      if (fpsInputText != string.Empty && !float.TryParse(fpsInputText, out fps))
        throw new ArgumentException("FPS is not a valid number.");
      if (!float.TryParse(speedModifierText, out speedModifier))
        throw new ArgumentException("Speed modifier is not a valid number.");

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
        outputPath = taskHandler.AdjustAudioOrVideoPlaybackSpeed(openFileDialog.FileName, speedModifier, fps);
      });

      mainWindow.SetMessage($"File with adjusted speed created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task AdjustVolume()
    {
      const string OP_NAME = "Adjust Volume";
      string volumeInputText = GetInputValue(OP_NAME, "Volume");

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
        outputPath = taskHandler.AdjustVolume(openFileDialog.FileName, volumeInputText);
      });

      mainWindow.SetMessage($"File with ajusted volume created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task AlignImage()
    {
      mainWindow.SetMessage("Aligning image.");

      const string OP_NAME = "Align Image";
      string alignModeInputText = GetInputValue(OP_NAME, "Align Mode");

      IVAE.MediaManipulation.ImageAlignmentType imageAlignmentType;
      if (!Enum.TryParse(alignModeInputText, out imageAlignmentType))
        throw new ArgumentException($"Image alignment type is not valid.");

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

    private async Task CombineGifs()
    {
      mainWindow.SetMessage("Combining GIFs.");

      const string OP_NAME = "Combine Gifs";
      string gifsPerLineInputText = GetInputValue(OP_NAME, "Gifs Per Line");

      int gifsPerLine = 0;
      if (gifsPerLineInputText != string.Empty && !int.TryParse(gifsPerLineInputText, out gifsPerLine))
        throw new ArgumentException($"Gifs per line is not a valid integer.");

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

    private async Task CombineVideos()
    {
      mainWindow.SetMessage("Combining Videos");

      const string OP_NAME = "Combine Videos";
      string combineHorizontallyInputText = GetInputValue(OP_NAME, "Horizontal");

      bool combineHorizontally = Convert.ToBoolean(combineHorizontallyInputText);

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

    private async Task Crop()
    {
      const string OP_NAME = "Crop";
      string xCoordinateInputText = GetInputValue(OP_NAME, "X Coordinate");
      string yCoordinateInputText = GetInputValue(OP_NAME, "Y Coordinate");
      string widthInputText = GetInputValue(OP_NAME, "Width");
      string heightInputText = GetInputValue(OP_NAME, "Height");

      double x = 0, y = 0, width = 0, height = 0;
      if (xCoordinateInputText != string.Empty && !double.TryParse(xCoordinateInputText, out x))
        throw new ArgumentException($"X coordinate is not a real number.");
      if (yCoordinateInputText != string.Empty && !double.TryParse(yCoordinateInputText, out y))
        throw new ArgumentException($"Y coordinate is not a real number.");
      if (widthInputText != string.Empty && !double.TryParse(widthInputText, out width))
        throw new ArgumentException($"Width is not a real number.");
      if (heightInputText != string.Empty && !double.TryParse(heightInputText, out height))
        throw new ArgumentException($"Height is not a real number.");

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

    private async Task DrawMatches()
    {
      const string OP_NAME = "Draw Matches";
      string alignModeInputText = GetInputValue(OP_NAME, "Align Mode");

      IVAE.MediaManipulation.ImageAlignmentType imageAlignmentType;
      if (!Enum.TryParse(alignModeInputText, out imageAlignmentType))
        throw new ArgumentException($"Image alignment mode is not valid.");

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

    private async Task ExtendVideo()
    {
      const string OP_NAME = "Extend Video";
      string secondsInputText = GetInputValue(OP_NAME, "Seconds");

      double seconds;
      if (!double.TryParse(secondsInputText, out seconds))
        throw new ArgumentException($"Seconds is not a positive real number.");

      System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
      openFileDialog.Filter = $"Video Files|{GetVideoFormatsFilterString()}";
      openFileDialog.Title = "Select Video File.";
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
        outputPath = taskHandler.ExtendLastFrameOfVideo(openFileDialog.FileName, seconds);
      });

      mainWindow.SetMessage($"Extended file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task ExtractAudio()
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

    private async Task Flip()
    {
      const string OP_NAME = "Flip";
      string horizontalInputText = GetInputValue(OP_NAME, "Horizontal");
      string verticalInputText = GetInputValue(OP_NAME, "Vertical");

      bool horizontal = Convert.ToBoolean(horizontalInputText);
      bool vertical = Convert.ToBoolean(verticalInputText);

      System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
      openFileDialog.Filter = $"Image or Video Files|{GetImageFormatsFilterString()}{GetVideoFormatsFilterString()}";
      openFileDialog.Title = "Select image or video file.";
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
        outputPath = taskHandler.FlipImageOrVideo(openFileDialog.FileName, horizontal, vertical);
      });

      mainWindow.SetMessage($"Rotated file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task GetScreenshot()
    {
      const string OP_NAME = "Get Screenshot";
      string endInputText = GetInputValue(OP_NAME, "End");
      string timeInputText = GetInputValue(OP_NAME, "Time");
      
      bool end = Convert.ToBoolean(endInputText);

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
        outputPath = taskHandler.GetScreenshotFromVideo(openFileDialog.FileName, timeInputText, end);
      });

      mainWindow.SetMessage($"Screenshot created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task GifToVideo()
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

    private async Task ImagesToGif()
    {
      mainWindow.SetMessage("Creating GIF.");

      const string OP_NAME = "Images To Gif";
      string xCoordinateInputText = GetInputValue(OP_NAME, "X Coordinate");
      string yCoordinateInputText = GetInputValue(OP_NAME, "Y Coordinate");
      string widthInputText = GetInputValue(OP_NAME, "Width");
      string heightInputText = GetInputValue(OP_NAME, "Height");
      string frameDelayInputText = GetInputValue(OP_NAME, "Frame Delay");
      string finalDelayInputText = GetInputValue(OP_NAME, "Final Delay");
      string loopsInputText = GetInputValue(OP_NAME, "Loops");
      string writeNamesInputText = GetInputValue(OP_NAME, "Write Names");
      string fontSizeInputText = GetInputValue(OP_NAME, "Font Size");
      string alignImagesInputText = GetInputValue(OP_NAME, "Align Images");
      string alignModeInputText = GetInputValue(OP_NAME, "Align Mode");

      int x = 0, y = 0, width = 0, height = 0, frameDelay = 0, finalDelay = 0, loops = 0, fontSize = 0;
      bool writeFileNames, alignImages;
      IVAE.MediaManipulation.ImageAlignmentType imageAlignmentType;
      if (xCoordinateInputText != string.Empty && !int.TryParse(xCoordinateInputText, out x))
        throw new ArgumentException($"X coordinate is not a valid integer.");
      if (yCoordinateInputText != string.Empty && !int.TryParse(yCoordinateInputText, out y))
        throw new ArgumentException($"Y coordinate is not a valid integer.");
      if (widthInputText != string.Empty && !int.TryParse(widthInputText, out width))
        throw new ArgumentException($"Width is not a valid integer.");
      if (heightInputText != string.Empty && !int.TryParse(heightInputText, out height))
        throw new ArgumentException($"Height is not a valid integer.");
      if (frameDelayInputText != string.Empty && !int.TryParse(frameDelayInputText, out frameDelay))
        throw new ArgumentException($"Frame delay is not a valid integer.");
      if (finalDelayInputText != string.Empty && !int.TryParse(finalDelayInputText, out finalDelay))
        throw new ArgumentException($"Final delay is not a valid integer.");
      if (loopsInputText != string.Empty && !int.TryParse(loopsInputText, out loops))
        throw new ArgumentException($"Loops is not a valid integer.");
      writeFileNames = Convert.ToBoolean(writeNamesInputText);
      if (fontSizeInputText != string.Empty && !int.TryParse(fontSizeInputText, out fontSize))
        throw new ArgumentException($"Font size is not a valid integer.");
      alignImages = Convert.ToBoolean(alignImagesInputText);
      if (!Enum.TryParse(alignModeInputText, out imageAlignmentType))
        throw new ArgumentException($"Image alignment type is not valid.");

      if (finalDelay == 0)
        finalDelay = frameDelay;

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

    private async Task NormalizeVolume()
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

    private async Task RemoveAudio()
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
        outputPath = taskHandler.RemoveAudioFromVideo(openFileDialog.FileName);
      });

      mainWindow.SetMessage($"Audioless video file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task Resize()
    {
      const string OP_NAME = "Resize";
      string widthInputText = GetInputValue(OP_NAME, "Width");
      string heightInputText = GetInputValue(OP_NAME, "Height");
      string scaleFactorInputText = GetInputValue(OP_NAME, "Scale Factor");

      int width = 0, height = 0;
      float scaleFactor = 0;
      if (widthInputText != string.Empty && !int.TryParse(widthInputText, out width))
        throw new ArgumentException($"Width is not a valid integer.");
      if (heightInputText != string.Empty && !int.TryParse(heightInputText, out height))
        throw new ArgumentException($"Height is not a valid integer.");
      if (scaleFactorInputText != string.Empty && !float.TryParse(scaleFactorInputText, out scaleFactor))
        throw new ArgumentException($"Scale Factor is not a valid number.");

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

    private async Task Reverse()
    {
      System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
      openFileDialog.Filter = $"Audio, Video, or GIF File|*.gif;{GetAudioFormatsFilterString()}{GetVideoFormatsFilterString()}";
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
        outputPath = taskHandler.Reverse(openFileDialog.FileName);
      });

      mainWindow.SetMessage($"Reversed file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task Rotate()
    {
      const string OP_NAME = "Rotate";
      string ccInputText = GetInputValue(OP_NAME, "Counter Clockwise");

      bool counterClockwise = Convert.ToBoolean(ccInputText);

      System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
      openFileDialog.Filter = $"Image or Video Files|{GetImageFormatsFilterString()}{GetVideoFormatsFilterString()}";
      openFileDialog.Title = "Select image or video file.";
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
        outputPath = taskHandler.RotateImageOrVideo(openFileDialog.FileName, counterClockwise);
      });

      mainWindow.SetMessage($"Rotated file created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task StabilizeVideo()
    {
      const string OP_NAME = "Stabilize Video";
      string optzoomInputText = GetInputValue(OP_NAME, "Optzoom");

      int optzoom = 0;
      if (optzoomInputText != string.Empty && !int.TryParse(optzoomInputText, out optzoom))
        throw new ArgumentException($"Optzoom is not a valid integer.");

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
        outputPath = taskHandler.StabilizeVideo(openFileDialog.FileName, optzoom);
      });

      mainWindow.SetMessage($"Stabilized video created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task StitchImages()
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

    private async Task Test()
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

    private async Task Trim()
    {
      const string OP_NAME = "Trim";
      string startTimeInputText = GetInputValue(OP_NAME, "Start Time");
      string endTimeInputText = GetInputValue(OP_NAME, "End Time");

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
        outputPath = taskHandler.TrimAudioOrVideo(openFileDialog.FileName, startTimeInputText, endTimeInputText);
      });

      mainWindow.SetMessage($"Trimmed video created '{outputPath}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task TwwToMp4()
    {
      const string OP_NAME = "TWW To MP4";
      string mainXInputText = GetInputValue(OP_NAME, "Main X");
      string mainYInputText = GetInputValue(OP_NAME, "Main Y");
      string mainWidthInputText = GetInputValue(OP_NAME, "Main Width");
      string mainHeightInputText = GetInputValue(OP_NAME, "Main Height");

      int mainX = 0, mainY = 0, mainWidth = 0, mainHeight = 0;
      if (mainXInputText != string.Empty && !int.TryParse(mainXInputText, out mainX))
        throw new ArgumentException($"Main X coordinate is not a valid integer.");
      if (mainYInputText != string.Empty && !int.TryParse(mainYInputText, out mainY))
        throw new ArgumentException($"Main Y coordinate is not a valid integer.");
      if (mainWidthInputText != string.Empty && !int.TryParse(mainWidthInputText, out mainWidth))
        throw new ArgumentException($"Main Width is not a valid integer.");
      if (mainHeightInputText != string.Empty && !int.TryParse(mainHeightInputText, out mainHeight))
        throw new ArgumentException($"Main Height is not a valid integer.");

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
        outputPath = taskHandler.TwwToMp4(openFileDialog.FileName, mainX, mainY, mainWidth, mainHeight);
      });

      mainWindow.SetMessage($"TWW Timelapse '{outputPath}' created in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task Tww3ToMp4()
    {
      const string OP_NAME = "TWW3 To MP4";
      string timelapseLengthInSecondsText = GetInputValue(OP_NAME, "Timelapse Seconds");
      string endLengthInSecondsText = GetInputValue(OP_NAME, "End Seconds");
      string mapXInputText = GetInputValue(OP_NAME, "Map X");
      string mapYInputText = GetInputValue(OP_NAME, "Map Y");
      string mapWidthInputText = GetInputValue(OP_NAME, "Map Width");
      string mapHeightInputText = GetInputValue(OP_NAME, "Map Height");
      string turnNumberXInputText = GetInputValue(OP_NAME, "Turn X");
      string turnNumberYInputText = GetInputValue(OP_NAME, "Turn Y");
      string turnNumberWidthInputText = GetInputValue(OP_NAME, "Turn Width");
      string turnNumberHeightInputText = GetInputValue(OP_NAME, "Turn Height");

      if (string.IsNullOrEmpty(timelapseLengthInSecondsText) || !double.TryParse(timelapseLengthInSecondsText, out double timelapseLengthInSeconds))
        throw new ArgumentException($"Timelapse Seconds coordinate is not a valid double.");
      if (string.IsNullOrEmpty(endLengthInSecondsText) || !double.TryParse(endLengthInSecondsText, out double endLengthInSeconds))
        throw new ArgumentException($"End Seconds is not a valid double.");
      if (string.IsNullOrEmpty(mapXInputText) || !int.TryParse(mapXInputText, out int mapX))
        throw new ArgumentException($"Map X coordinate is not a valid integer.");
      if (string.IsNullOrEmpty(mapYInputText) || !int.TryParse(mapYInputText, out int mapY))
        throw new ArgumentException($"Map Y coordinate is not a valid integer.");
      if (string.IsNullOrEmpty(mapWidthInputText) || !int.TryParse(mapWidthInputText, out int mapWidth))
        throw new ArgumentException($"Map Width is not a valid integer.");
      if (string.IsNullOrEmpty(mapHeightInputText) || !int.TryParse(mapHeightInputText, out int mapHeight))
        throw new ArgumentException($"Map Height is not a valid integer.");
      if (string.IsNullOrEmpty(turnNumberXInputText) || !int.TryParse(turnNumberXInputText, out int turnNumberX))
        throw new ArgumentException($"Turn Number X coordinate is not a valid integer.");
      if (string.IsNullOrEmpty(turnNumberYInputText) || !int.TryParse(turnNumberYInputText, out int turnNumberY))
        throw new ArgumentException($"Turn Number Y coordinate is not a valid integer.");
      if (string.IsNullOrEmpty(turnNumberWidthInputText) || !int.TryParse(turnNumberWidthInputText, out int turnNumberWidth))
        throw new ArgumentException($"Turn Number Width is not a valid integer.");
      if (string.IsNullOrEmpty(turnNumberHeightInputText) || !int.TryParse(turnNumberHeightInputText, out int turnNumberHeight))
        throw new ArgumentException($"Turn Number Height is not a valid integer.");

      System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
      openFileDialog.Filter = $"Video File|{GetVideoFormatsFilterString()}";
      openFileDialog.Title = "Select Videos.";
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
        outputPath = taskHandler.Tww3ToMp4(openFileDialog.FileNames.ToList(), timelapseLengthInSeconds, endLengthInSeconds, mapX, mapY, mapWidth, mapHeight,
          turnNumberX, turnNumberY, turnNumberWidth, turnNumberHeight);
      });

      mainWindow.SetMessage($"TWW3 Timelapse '{outputPath}' created in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputPath);
    }

    private async Task VideoToImages()
    {
      const string OP_NAME = "Video To Images";
      string fpsInputText = GetInputValue(OP_NAME, "FPS");

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
        outputDirectory = taskHandler.ConvertVideoToImages(openFileDialog.FileName, fpsInputText);
      });

      mainWindow.SetMessage($"Images created '{outputDirectory}' in {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s.");
      System.Diagnostics.Process.Start(outputDirectory);

    }
    #endregion

    #region Helpers
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

    private string GetInputSettingName(string operation, string inputName)
    {
      return $"{operation.Replace(" ", string.Empty)}_{inputName.Replace(" ", string.Empty)}";
    }

    private string GetInputValue(string operation, string inputName)
    {
      string inputSettingName = GetInputSettingName(operation, inputName);

      Grid inputGrid = null;
      for (int i = 0; i < mainWindow.gridInputs.Children.Count; i++)
      {
        Grid grid = mainWindow.gridInputs.Children[i] as Grid;

        if (grid == null)
          continue;

        if (grid.Name == $"inputGrid{inputSettingName}")
        {
          inputGrid = grid;
          break;
        }
      }

      if (inputGrid == null)
        throw new Exception($"Could not find input grid '{inputSettingName}'.");

      OperationSetupInfo operationSetupInfo = operationSetups[operation];
      ControlType inputControlType = operationSetupInfo.InputInfo[inputName].ControlType;
      if (inputControlType == ControlType.CheckBox)
        return ((CheckBox)inputGrid.Children[1]).IsChecked.ToString();
      else if (inputControlType == ControlType.ComboBox)
        return ((ComboBox)inputGrid.Children[1]).SelectedValue.ToString();
      else if (inputControlType == ControlType.TextBox)
        return ((TextBox)inputGrid.Children[1]).Text;
      else
        throw new NotImplementedException($"Unsupported control type '{operationSetups[operation].InputInfo[inputName].ToString()}'.");
    }
    #endregion
  }
}
