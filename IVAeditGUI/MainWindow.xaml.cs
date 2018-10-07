using System;
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
    public event Action OnAlignImageButtonClick, OnCombineGifsButtonClick, OnImagesToGifButtonClick, OnGifToGifvButtonClick, OnTestButtonClick;

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

    private void btnAlignImage_Click(object sender, RoutedEventArgs e)
    {
      OnAlignImageButtonClick?.Invoke();
    }

    private void btnCombineGifs_Click(object sender, RoutedEventArgs e)
    {
      OnCombineGifsButtonClick?.Invoke();
    }

    private void btnImagesToGif_Click(object sender, RoutedEventArgs e)
    {
      OnImagesToGifButtonClick?.Invoke();
    }

    private void btnGifToGifv_Click(object sender, RoutedEventArgs e)
    {
      OnGifToGifvButtonClick?.Invoke();
    }

    private void btnTest_Click(object sender, RoutedEventArgs e)
    {
      OnTestButtonClick?.Invoke();
    }
  }
}
