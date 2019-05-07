using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace IVAE.RedditBot
{
  class Logger
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
