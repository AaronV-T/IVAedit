using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot
{
  public static class Extensions
  {
    public static DateTime UnixTimeToDateTime(this long redditTime)
    {
      return DateTimeOffset.FromUnixTimeSeconds(redditTime).DateTime;
    }
  }
}
