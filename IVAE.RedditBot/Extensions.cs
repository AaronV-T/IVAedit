using IVAE.RedditBot.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot
{
  public static class Extensions
  {
    public static void AddParameter(this System.Data.IDbCommand dbCommand, string name, object value)
    {
      System.Data.IDbDataParameter parameter = dbCommand.CreateParameter();
      parameter.ParameterName = name;
      parameter.Value = value;
      dbCommand.Parameters.Add(parameter);
    }

    public static string GetCommandTextFromMention(this RedditThing mention, string myUsername)
    {
      List<string> messageBodyLines = mention.Body.Split(new string[] { "  \n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
      foreach (string line in messageBodyLines)
      {
        int mentionIndex = line.ToLower().Trim().IndexOf($"u/{myUsername.ToLower()}");
        if (mentionIndex == 0 || mentionIndex == 1)
          return line;
      }

      return null;
    }

    public static DateTime UnixTimeToDateTime(this long redditTime)
    {
      return DateTimeOffset.FromUnixTimeSeconds(redditTime).DateTime;
    }
  }
}
