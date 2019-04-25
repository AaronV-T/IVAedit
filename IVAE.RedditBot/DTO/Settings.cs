using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot.DTO
{
  public class Settings
  {
    public string FallbackReplySubreddit { get; set; }
    public FilterSettings FilterSettings { get; set; }
    public List<string> RequestorWhitelist { get; set; }
    public List<string> RequestorBlacklist { get; set; }
    public List<string> SubredditBlacklist { get; set; }
  }

  public class FilterSettings
  {
    public double MaximumDownloadFileSizeInMB { get; set; }
    public double MinimumPostAgeInMinutes { get; set; }
    public int MinimumPostScore { get; set; }
    public double MinimumPostingAccountAgeInDays { get; set; }
    public int MinimumPostingAccountKarma { get; set; }
    public int MinimumSubredditSubscribers { get; set; }
    public bool ProcessEditedComments { get; set; }
    public bool ProcessNSFWContent { get; set; }
    public bool ProcessPostsInNonPublicSubreddits { get; set; }
  }
}
