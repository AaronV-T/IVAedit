using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IVAE.RedditBot.DTO;

namespace IVAE.RedditBot
{
  public class CleanupManager
  {
    private DatabaseAccessor databaseAccessor;
    private ImgurClient imgurClient;
    private RedditClient redditClient;
    private Settings settings;

    public CleanupManager(DatabaseAccessor databaseAccessor, ImgurClient imgurClient, RedditClient redditClient, Settings settings)
    {
      this.databaseAccessor = databaseAccessor ?? throw new ArgumentNullException(nameof(databaseAccessor));
      this.imgurClient = imgurClient ?? throw new ArgumentNullException(nameof(imgurClient));
      this.redditClient = redditClient ?? throw new ArgumentNullException(nameof(redditClient));
      this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task CleanupPosts(DateTime earliestTimeToCleanup)
    {
      List<UploadLog> uploadLogs = databaseAccessor.GetAllUploadLogs();

      foreach (UploadLog uploadLog in uploadLogs)
      {
        if (uploadLog.Deleted || uploadLog.UploadDatetime < earliestTimeToCleanup)
          continue;

        List<RedditThing> postAndReply = await redditClient.GetInfoOfCommentsAndLinks("all", new List<string> { uploadLog.PostFullname, uploadLog.ReplyFullname });
        RedditThing originalFilePost = postAndReply[0];
        RedditThing replyComment = postAndReply[1];

        if (!settings.FilterSettings.ProcessNSFWContent && originalFilePost.Over18 != null && originalFilePost.Over18.Value)
          await DeleteUpload(uploadLog, "Post was NSFW.");
        else if (!settings.FilterSettings.ProcessNSFWContent && originalFilePost.Body != null && (originalFilePost.Body.ToLower().Contains("nsfw") || originalFilePost.Body.ToLower().Contains("nsfl")))
          await DeleteUpload(uploadLog, $"Post was self-marked as NSFW.");
        else if (replyComment.Score < 0)
          await DeleteUpload(uploadLog, $"Reply score ({replyComment.Score}) was too low.");
        else if (originalFilePost.Author == "[deleted]")
          await DeleteUpload(uploadLog, $"Post was deleted or removed.");
        else if (originalFilePost.BannedAtUtc != null)
          await DeleteUpload(uploadLog, $"Post was removed.");
      }
    }

    private async Task DeleteUpload(UploadLog uploadLog, string reason)
    {
      Console.WriteLine($"Deleting UploadLog with ID '{uploadLog.Id}' (reply '{uploadLog.ReplyFullname}')");

      if (uploadLog.UploadDestination.ToLower() == "imgur")
        await imgurClient.Delete(uploadLog.DeleteKey);

      await redditClient.DeletePost(uploadLog.ReplyFullname);

      uploadLog.Deleted = true;
      uploadLog.DeleteDatetime = DateTime.UtcNow;
      uploadLog.DeleteReason = reason;
      databaseAccessor.SaveUploadLog(uploadLog);

      Console.WriteLine($"Deleted post '{uploadLog.ReplyFullname}'.");
    }
  }
}
