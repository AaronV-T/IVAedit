using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot.DTO
{
  public class Settings
  {
    public List<string> Administrators { get; set; }
    public string FallbackReplySubreddit { get; set; }
    public FilterSettings FilterSettings { get; set; }
  }

  public class FilterSettings
  {
    public double MaximumDownloadFileSizeInMB { get; set; }
    public double MaximumUploadFileSizeInMB { get; set; }
    public double MaximumUploadFileDurationInSeconds { get; set; }
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
