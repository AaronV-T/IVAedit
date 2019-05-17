using System;
using System.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace IVAE.MediaManipulation
{
  public class Logger
  {
    public static void Init()
    {
      LoggingLevelSwitch loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

      LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
        .MinimumLevel.ControlledBy(loggingLevelSwitch)
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.File("log.txt");

      Log.Logger = loggerConfiguration.CreateLogger();

      LogEventLevel logEventLevel;
      if (Enum.TryParse(ConfigurationManager.AppSettings["LogEventLevel"], true, out logEventLevel))
      {
        loggingLevelSwitch.MinimumLevel = logEventLevel;
      }
    }
  }
}
