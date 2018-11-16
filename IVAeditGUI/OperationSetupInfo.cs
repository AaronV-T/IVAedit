using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAeditGUI
{
  public enum ControlType { CheckBox, ComboBox, TextBox }

  public class OperationSetupInfo
  {
    public IReadOnlyDictionary<string, InputSetupInfo> InputInfo;
    public Func<Task> Func;

    public OperationSetupInfo(IReadOnlyDictionary<string, InputSetupInfo> inputInfo, Func<Task> func)
    {
      InputInfo = inputInfo;
      Func = func;
    }
  }

  public class InputSetupInfo
  {
    public ControlType ControlType;
    public object AdditionalInfo;

    public InputSetupInfo(ControlType controlType, object additionalInfo = null)
    {
      ControlType = controlType;
      AdditionalInfo = additionalInfo;
    }
  }
}
