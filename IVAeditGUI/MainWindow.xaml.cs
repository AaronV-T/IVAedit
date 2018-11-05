﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IVAeditGUI
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public event Action
      OnAdjustVolumeButtonClick,
      OnAlignImageButtonClick,
      OnChangeSpeedButtonClick,
      OnCombineGifsButtonClick,
      OnCropButtonClick,
      OnDrawMatchesButtonClick,
      OnExtractAudioButtonClick,
      OnImagesToGifButtonClick,
      OnGifToVideoButtonClick,
      OnNormalizeVolumeButtonClick,
      OnStabilizeVideoButtonClick,
      OnStitchImagesButtonClick,
      OnTestButtonClick,
      OnTrimButtonClick,
      OnTwwToMp4ButtonClick,
      OnVideoToImagesButtonClick;

    public MainWindow()
    {
      InitializeComponent();

      new Controller(this);
    }

    public void SetMessage(string text)
    {
      // If we can access the message TextBlock: Set its text.
      if (tblckMessage.Dispatcher.CheckAccess())
        tblckMessage.Text = text;
      else // If we can not: Invoke a delegate to call this method on the UI thread..
      {
        Action<string> d = new Action<string>(SetMessage);
        tblckMessage.Dispatcher.Invoke(d, new object[] { text });
      }
    }

    private void btnAdjustVolume_Click(object sender, RoutedEventArgs e)
    {
      OnAdjustVolumeButtonClick?.Invoke();
    }

    private void btnAlignImage_Click(object sender, RoutedEventArgs e)
    {
      OnAlignImageButtonClick?.Invoke();
    }

    private void btnChangeSpeed_Click(object sender, RoutedEventArgs e)
    {
      OnChangeSpeedButtonClick?.Invoke();
    }

    private void btnCombineGifs_Click(object sender, RoutedEventArgs e)
    {
      OnCombineGifsButtonClick?.Invoke();
    }

    private void btnCrop_Click(object sender, RoutedEventArgs e)
    {
      OnCropButtonClick?.Invoke();
    }

    private void btnDrawMatches_Click(object sender, RoutedEventArgs e)
    {
      OnDrawMatchesButtonClick?.Invoke();
    }

    private void btnExtractAudio_Click(object sender, RoutedEventArgs e)
    {
      OnExtractAudioButtonClick?.Invoke();
    }

    private void btnImagesToGif_Click(object sender, RoutedEventArgs e)
    {
      OnImagesToGifButtonClick?.Invoke();
    }

    private void btnGifToVideo_Click(object sender, RoutedEventArgs e)
    {
      OnGifToVideoButtonClick?.Invoke();
    }

    private void btnNormalizeVolume_Click(object sender, RoutedEventArgs e)
    {
      OnNormalizeVolumeButtonClick?.Invoke();
    }

    private void btnStabilizeVideo_Click(object sender, RoutedEventArgs e)
    {
      OnStabilizeVideoButtonClick?.Invoke();
    }

    private void btnStitchImages_Click(object sender, RoutedEventArgs e)
    {
      OnStitchImagesButtonClick?.Invoke();
    }

    private void btnTest_Click(object sender, RoutedEventArgs e)
    {
      OnTestButtonClick?.Invoke();
    }

    private void btnTrim_Click(object sender, RoutedEventArgs e)
    {
      OnTrimButtonClick?.Invoke();
    }

    private void btnTwwToMp4_Click(object sender, RoutedEventArgs e)
    {
      OnTwwToMp4ButtonClick?.Invoke();
    }

    private void btnVideoToImages_Click(object sender, RoutedEventArgs e)
    {
      OnVideoToImagesButtonClick?.Invoke();
    }
  }
}
