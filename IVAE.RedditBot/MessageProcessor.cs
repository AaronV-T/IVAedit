using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IVAE.RedditBot.DTO;

namespace IVAE.RedditBot
{
  public class MessageProcessor
  {
    private const string DOWNLOAD_DIR = "AutomatedFileDownloads";

    private DatabaseAccessor databaseAccessor;
    private ImgurClient imgurClient;
    private RedditClient redditClient;
    private Settings settings;

    public MessageProcessor(DatabaseAccessor databaseAccessor, ImgurClient imgurClient, RedditClient redditClient, Settings settings)
    {
      this.databaseAccessor = databaseAccessor ?? throw new ArgumentNullException(nameof(databaseAccessor));
      this.imgurClient = imgurClient ?? throw new ArgumentNullException(nameof(imgurClient));
      this.redditClient = redditClient ?? throw new ArgumentNullException(nameof(redditClient));
      this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task ProcessUnreadMessages()
    {
      if (System.IO.Directory.Exists(DOWNLOAD_DIR))
        System.IO.Directory.Delete(DOWNLOAD_DIR, true);

      try
      {
        Console.WriteLine("Getting unread messages...");
        List<RedditThing> messages = await redditClient.GetUnreadMessages();

        Console.WriteLine("Filtering out messages that aren't comments with user mentions...");
        List<RedditThing> comments = await FilterMentionMessagesAndMarkRestRead(messages);

        Console.WriteLine($"Getting parents of {comments.Count} comments...");
        Dictionary<RedditThing, RedditThing> commentsWithParents = await GetParentsOfComments( messages);

        Console.WriteLine($"Processing {commentsWithParents.Count} posts...");
        await ProcessPosts(commentsWithParents);

        //sb.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(commentsWithParents, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore }));
        //System.IO.File.WriteAllText("output.txt", sb.ToString());
        //System.Diagnostics.Process.Start("output.txt");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      //if (System.IO.Directory.Exists(DOWNLOAD_DIR))
        //System.IO.Directory.Delete(DOWNLOAD_DIR, true);
    }

    private async Task<List<RedditThing>> FilterMentionMessagesAndMarkRestRead(List<RedditThing> unreadMessages)
    {
      List<string> messagesToMarkRead = new List<string>();
      List<RedditThing> messagesToProcess = new List<RedditThing>();
      foreach (RedditThing message in unreadMessages)
      {
        if (message.Kind == "t4" || message.Body.Trim().IndexOf("/u/IVAedit") != 0)
        {
          messagesToMarkRead.Add(message.Name);
          continue;
        }

        messagesToProcess.Add(message);
      }

      await redditClient.MarkMessagesAsRead(messagesToMarkRead);

      return messagesToProcess;
    }

    private async Task<Dictionary<RedditThing, RedditThing>> GetParentsOfComments(List<RedditThing> comments)
    {
      Dictionary<RedditThing, RedditThing> messagesWithParents = new Dictionary<RedditThing, RedditThing>();
      foreach (RedditThing message in comments)
        messagesWithParents.Add(message, await redditClient.GetInfoOfCommentOrLink(message.Subreddit, message.ParentId));

      return messagesWithParents;
    }

    private async Task ProcessPosts(Dictionary<RedditThing, RedditThing> commentsWithParents)
    {
      foreach(var kvp in commentsWithParents)
      {
        try
        {
          RedditThing mentionComment = kvp.Key;
          RedditThing parentPost = kvp.Value;

          bool requestorIsWhitelisted = settings.RequestorWhitelist.Contains(mentionComment.Author);

          // Verify that the post is old enough.
          if (!requestorIsWhitelisted && parentPost.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Now.ToUniversalTime().AddMinutes(-settings.FilterSettings.MinimumPostAgeInMinutes))
          {
            Console.WriteLine($"Temporarily skipping {mentionComment.Name}: Post is too recent. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          //await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });

          // Verify that the requestor isn't blacklisted.
          if (settings.RequestorBlacklist.Contains(mentionComment.Author))
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Requestor is blacklisted. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Verify that the post is safe to process.
          if (!requestorIsWhitelisted && !await PostIsSafeToProcess(parentPost, true))
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Post is not safe to process. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Get the commands from the mention comment.
          List<IVAECommand> commands = IVAECommandFactory.CreateCommands(mentionComment.Body.Split('\n')[0]);
          if (commands == null || commands.Count == 0)
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: No valid commands. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }
          else if (commands.Any(command => commands.Count(cmd => cmd.GetType() == command.GetType()) > 1)) // This is O(n^2), consider something more efficient.
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Multiple commands of same type. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Get url to media file.
          Uri mediaUrl = GetMediaUrlFromPost(parentPost);
          if (string.IsNullOrWhiteSpace(mediaUrl.AbsoluteUri))
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Invalid media url. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Verify that the file is not too large.
          if (!TryGetMediaFileSize(mediaUrl.AbsoluteUri, out long fileSize) || fileSize > settings.FilterSettings.MaximumDownloadFileSizeInMB * 10000000)
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Bad media file size. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Ensure that the download directory exists.
          if (!System.IO.Directory.Exists(DOWNLOAD_DIR))
            System.IO.Directory.CreateDirectory(DOWNLOAD_DIR);

          string mediaFilePath = null;
          try
          {
            // Get a good file name.
            int mediaUrlFileNameStartIndex = mediaUrl.AbsoluteUri.LastIndexOf('/') + 1;
            string extension = System.IO.Path.GetExtension(mediaUrl.AbsoluteUri);
            if (string.IsNullOrEmpty(extension))
              extension = ".mp4";
            mediaFilePath = System.IO.Path.Combine(DOWNLOAD_DIR, $"{Guid.NewGuid()}{extension}");

            // Download the media file.
            System.Diagnostics.Stopwatch downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
              client.DownloadFile(mediaUrl, mediaFilePath);

              if (mediaUrl.Host == "v.redd.it")
              {
                try
                {
                  string audioUrl = $"{mediaUrl.AbsoluteUri.Substring(0, mediaUrl.AbsoluteUri.LastIndexOf("/"))}/audio";
                  string audioFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mediaFilePath), $"{System.IO.Path.GetFileNameWithoutExtension(mediaFilePath)}_audio");
                  client.DownloadFile(audioUrl, audioFilePath);
                  new MediaManipulation.FFmpegProcessRunner().Run($"-i {mediaFilePath} -i {audioFilePath} -acodec copy -vcodec copy combined.mp4");
                  System.IO.File.Delete(audioFilePath);
                  System.IO.File.Delete(mediaFilePath);
                  System.IO.File.Move("combined.mp4", mediaFilePath);
                }
                catch (Exception) { }
              }
            }
            downloadStopwatch.Stop();

            // Execute all commands on the media file.
            long origFileSize = new System.IO.FileInfo(mediaFilePath).Length;
            System.Diagnostics.Stopwatch transformStopwatch = System.Diagnostics.Stopwatch.StartNew();
            foreach (IVAECommand command in commands)
            {
              string path = command.Execute(mediaFilePath);
              System.IO.File.Delete(mediaFilePath);
              mediaFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mediaFilePath), $"{System.IO.Path.GetFileNameWithoutExtension(mediaFilePath)}{System.IO.Path.GetExtension(path)}");
              System.IO.File.Move(path, mediaFilePath);
            }
            transformStopwatch.Stop();
            long transformedFileSize = new System.IO.FileInfo(mediaFilePath).Length;

            // Upload altered media file.
            System.Diagnostics.Stopwatch uploadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            string videoFormat = null;
            if (System.IO.Path.GetExtension(mediaFilePath) == ".mp4")
              videoFormat = "mp4";
            ImgurUploadResponse uploadResponse = await imgurClient.Upload(System.IO.File.ReadAllBytes(mediaFilePath), videoFormat);
            uploadStopwatch.Stop();

            if (uploadResponse == null)
            {
              Console.WriteLine("Upload failed.");
              continue;
            }

            // Respond with link.
            string replyCommentName = await redditClient.PostComment(mentionComment.Name, $"[Direct File Link]({uploadResponse.Link}) - [Post Link](https://imgur.com/{uploadResponse.Id})\n\n***\nI am a bot in development.  \nDownload: {downloadStopwatch.Elapsed.TotalMinutes.ToString("N2")}m, transform: {transformStopwatch.Elapsed.TotalMinutes.ToString("N2")}m, upload: {uploadStopwatch.Elapsed.TotalMinutes.ToString("N2")}m. Original size: {((double)origFileSize / 1000000).ToString("N2")}MB, new size: {((double)transformedFileSize / 1000000).ToString("N2")}MB.");
            Console.WriteLine($"Reply Posted: {replyCommentName != null}");

            if (replyCommentName != null)
            {
              databaseAccessor.SaveUploadLog(new UploadLog
              {
                Deleted = false,
                DeleteKey = uploadResponse.DeleteHash,
                PostFullname = parentPost.Name,
                ReplyFullname = replyCommentName,
                RequestorUsername = mentionComment.Author,
                UploadDatetime = DateTime.UtcNow,
                UploadDestination = "imgur"
              });
            }
            else
              await imgurClient.Delete(uploadResponse.DeleteHash);
          }
          catch (Exception)
          {
            //if (!string.IsNullOrWhiteSpace(mediaFilePath) && System.IO.File.Exists(mediaFilePath))
              //System.IO.File.Delete(mediaFilePath);

            throw;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception occurred when processing post. {ex}");
        }
      }
    }

    private static Uri GetMediaUrlFromPost(RedditThing post)
    {
      // Comment
      if (post.Kind == "t1")
      {
        // Return the first link found in the body.
        string[] splitBody = post.Body.Split();
        foreach (string s in splitBody)
        {
          if (Uri.TryCreate(s, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return new Uri(s);
        }

        return null;
      }
      // Link
      else if (post.Kind == "t3")
      {
        if (post.Media != null)
        {
          dynamic mediaObj = post.Media;
          if (mediaObj.reddit_video == null)
          {
            Console.WriteLine($"Can't find reddit_video object in post's media object.");
            return null;
          }

          string url = mediaObj.reddit_video.fallback_url;
          return new Uri(url);
        }
        
        if (post.CrosspostParentList != null && post.CrosspostParentList.Count > 0)
        {
          return GetMediaUrlFromPost(post.CrosspostParentList[0]);
        }

        string mediaUrl;
        if (System.IO.Path.GetExtension(post.Url).ToLower() == ".gifv")
          mediaUrl = $"{post.Url.Substring(0, post.Url.Length - 5)}.mp4";
        else
          mediaUrl = post.Url;

        return new Uri(mediaUrl);
      }
      else
        throw new ArgumentException($"Given post '{post}' is not a valid kind.");
    }

    private static bool TryGetMediaFileSize(string url, out long fileSize)
    {
      System.Net.WebRequest request = System.Net.WebRequest.Create(url);
      request.Method = "HEAD";
      using (System.Net.WebResponse response = request.GetResponse())
      {
        return long.TryParse(response.Headers.Get("Content-Length"), out fileSize);
      }
    }

    private async Task<bool> PostIsSafeToProcess(RedditThing post, bool isRootPost)
    {
      FilterSettings filterSettings = settings.FilterSettings;

      // If this is the post with the media file to manipulate: ...
      if (isRootPost)
      {
        if (post.Score < filterSettings.MinimumPostScore) return false; // Post's score is too low.
        if (post.Kind == "t1")
        {
          if (!filterSettings.ProcessEditedComments && (post.Edited == null || post.Edited.Value)) return false; // Comment is edited.
          if (!filterSettings.ProcessNSFWContent && (post.Body.ToLower().Contains("nsfw") || post.Body.ToLower().Contains("nsfl"))) return false; // Comment is self-marked as NSFW/NSFL.
        }

        RedditThing user = await redditClient.GetInfoOfUser(post.Author);
        if (user.CommentKarma == null || user.LinkKarma == null || user.CommentKarma + user.LinkKarma < filterSettings.MinimumPostingAccountKarma) return false; // Posting user's account doesn't have enough karma.
        if (user.CreatedUtc == null || user.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Today.AddDays(-filterSettings.MinimumPostingAccountAgeInDays)) return false; // Posting user's account isn't old enough.
      }

      // If this post is a comment: ...
      if (post.Kind == "t1")
      {
        return await PostIsSafeToProcess(await redditClient.GetInfoOfCommentOrLink(post.Subreddit, post.LinkId), false); // Check this comment's parent post.
      }
      // If this post is a link: ...
      else if (post.Kind == "t3")
      {
        if (!filterSettings.ProcessNSFWContent && (post.Over18 == null || post.Over18.Value)) return false; // Link is flagged NSFW.
        if (post.SubredditSubscribers == null || post.SubredditSubscribers < filterSettings.MinimumSubredditSubscribers) return false; // Link is in a subreddit that's too small.
        if (!filterSettings.ProcessPostsInNonPublicSubreddits && (post.SubredditType == null || post.SubredditType != "public")) return false; // Link is in a subreddit that's not public.

        return true;
      }
      else
        throw new ArgumentException($"Given post '{post.Name}' is not a valid kind '{post.Kind}'.");
    }
  }
}
