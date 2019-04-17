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

    public async Task SanitizePosts()
    {
      List<UploadLog> uploadLogs = databaseAccessor.GetAllUploadLogs();

      foreach (UploadLog uploadLog in uploadLogs)
      {
        if (uploadLog.UploadDatetime < DateTime.UtcNow.AddDays(-7))
          continue;

        List<RedditThing> postAndReply = await redditClient.GetInfoOfCommentsAndLinks("all", new List<string> { uploadLog.PostFullname, uploadLog.ReplyFullname });
        RedditThing originalFilePost = postAndReply[0];
        RedditThing replyComment = postAndReply[1];

        if ((originalFilePost.Over18 != null && originalFilePost.Over18.Value) ||
            replyComment.Score < 0 ||
            originalFilePost.Author == "[deleted]" ||
            originalFilePost.BannedAtUtc != null ||
            originalFilePost.Body.ToLower().Contains("nsfw") ||
            originalFilePost.Body.ToLower().Contains("nsfl"))
        {
          await imgurClient.Delete(uploadLog.DeleteKey);
          await redditClient.DeletePost(uploadLog.ReplyFullname);
          databaseAccessor.DeleteUploadLog(uploadLog);

          Console.WriteLine($"Deleted post '{uploadLog.ReplyFullname}'.");
        }
      }
    }
  }
}
