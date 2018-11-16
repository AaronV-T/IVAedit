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
    public event Action
      OnOperationSelectorSelectionChanged,
      OnRunButtonClick,
      OnStopButtonClick;

    public MainWindow()
    {
      InitializeComponent();

      new Controller(this);

      Title = $"IVAedit GUI v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
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

    private void btnRun_Click(object sender, RoutedEventArgs e)
    {
      OnRunButtonClick?.Invoke();
    }

    private void cbOperationSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      OnOperationSelectorSelectionChanged?.Invoke();
    }

    private void btnStop_Click(object sender, RoutedEventArgs e)
    {
      OnStopButtonClick?.Invoke();
    }
  }
}
