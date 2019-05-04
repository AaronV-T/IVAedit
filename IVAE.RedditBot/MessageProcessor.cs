using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IVAE.RedditBot.DTO;

namespace IVAE.RedditBot
{
  public class MessageProcessor
  {
    private const string DOWNLOAD_DIR = "AutomatedFileDownloads";

    private CleanupManager cleanupManager;
    private DatabaseAccessor databaseAccessor;
    private ImgurClient imgurClient;
    private RedditClient redditClient;
    private Settings settings;

    public MessageProcessor(CleanupManager cleanupManager, DatabaseAccessor databaseAccessor, ImgurClient imgurClient, RedditClient redditClient, Settings settings)
    {
      this.cleanupManager = cleanupManager ?? throw new ArgumentNullException(nameof(cleanupManager));
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
        Console.WriteLine("Getting unread messages.");
        List<RedditThing> unreadMessages = await redditClient.GetUnreadMessages();

        List<string> messageNamesToMarkRead = new List<string>();
        List<RedditThing> commandMessages = new List<RedditThing>();
        List<RedditThing> requestComments = new List<RedditThing>();
        foreach (RedditThing message in unreadMessages)
        {
          if (message.Kind == "t4" && message.Subject.ToLower() == "command")
            commandMessages.Add(message);
          else if (message.Kind == "t1" && message.GetCommandTextFromMention(redditClient.Username) != null)
            requestComments.Add(message);
          else
            messageNamesToMarkRead.Add(message.Name);
        }

        if (messageNamesToMarkRead.Count > 0)
        {
          Console.WriteLine($"Ignoring {messageNamesToMarkRead.Count} messages.");
          await redditClient.MarkMessagesAsRead(messageNamesToMarkRead);
        }

        if (commandMessages.Count > 0)
        {
          Console.WriteLine($"Processing {commandMessages.Count} command messages.");
          await ProcessCommands(commandMessages);
        }

        if (requestComments.Count > 0)
        {
          Console.WriteLine($"Processing {requestComments.Count} request comments.");
          await ProcessRequests(requestComments);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      if (System.IO.Directory.Exists(DOWNLOAD_DIR))
        System.IO.Directory.Delete(DOWNLOAD_DIR, true);
    }


    private async Task ProcessCommands(List<RedditThing> commands)
    {
      foreach (RedditThing command in commands)
      {
        await redditClient.MarkMessagesAsRead(new List<string> { command.Name });

        List<string> splitCommandText = command.Body.Trim().Split().ToList();

        if (splitCommandText.Count < 1 || string.IsNullOrEmpty(splitCommandText[0]))
          continue;

        switch(splitCommandText[0].ToLower())
        {
          case "ban":
          case "blacklist":
            if (!settings.Administrators.Contains(command.Author))
              break;

            StringBuilder replyText = new StringBuilder();
            for (int i = 1; i < splitCommandText.Count; i++)
            {
              if (Regex.IsMatch(splitCommandText[i], @"^/?r/\w+"))
              {
                string subreddit = splitCommandText[i].Substring(splitCommandText[1].LastIndexOf("/") + 1);
                if (databaseAccessor.GetBlacklistedSubreddit(subreddit) == null)
                {
                  databaseAccessor.InsertBlacklistedSubreddit(subreddit, command.Author);
                  replyText.Append($"/r/{subreddit} has been blacklisted.  \n");
                }
                else
                  replyText.Append($"/r/{subreddit} is already blacklisted.  \n");

              }
              else if (Regex.IsMatch(splitCommandText[i], @"^/?u/\w+"))
              {
                string username = splitCommandText[i].Substring(splitCommandText[1].LastIndexOf("/") + 1);
                if (databaseAccessor.GetBlacklistedUser(username) == null)
                {
                  databaseAccessor.InsertBlacklistedUser(username, command.Author);
                  await redditClient.BlockUser(username);
                  replyText.Append($"/u/{username} has been blacklisted.  \n");
                }
                else
                  replyText.Append($"/u/{username} is already blacklisted.  \n");
              }
            }

            if (!string.IsNullOrEmpty(replyText.ToString()))
              await redditClient.PostComment(command.Name, replyText.ToString());

            break;
          case "delete":
            if (splitCommandText.Count < 2)
              break;

            UploadLog uploadLog = databaseAccessor.GetUploadLog(new Guid(splitCommandText[1]));
            if (uploadLog == null)
              break;

            if (settings.Administrators.Contains(command.Author) || uploadLog.RequestorUsername == command.Author)
              await cleanupManager.DeleteUpload(uploadLog, $"Requested by /u/{command.Author}");

            break;
        }
      }
    }

    private async Task ProcessRequests(List<RedditThing> requests)
    {
      // Get parent of each request.
      Dictionary<RedditThing, RedditThing> requestsWithParents = new Dictionary<RedditThing, RedditThing>();
      foreach (RedditThing message in requests)
        requestsWithParents.Add(message, await redditClient.GetInfoOfCommentOrLink(message.Subreddit, message.ParentId));

      // Process each request.
      foreach (var kvp in requestsWithParents)
      {
        try
        {
          RedditThing mentionComment = kvp.Key;
          RedditThing parentPost = kvp.Value;

          bool requestorIsAdmin = settings.Administrators.Contains(mentionComment.Author);

          // Verify that the post is old enough.
          if (!requestorIsAdmin && parentPost.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Now.ToUniversalTime().AddMinutes(-settings.FilterSettings.MinimumPostAgeInMinutes))
          {
            Console.WriteLine($"Temporarily skipping {mentionComment.Name}: Post is too recent. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });

          // Verify that the requestor isn't blacklisted.
          if (databaseAccessor.GetBlacklistedUser(mentionComment.Author) != null)
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Requestor is blacklisted. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          Func<string, Task> onFailedToProcessPost = async (reason) =>
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: {reason} ({mentionComment.Author}: '{mentionComment.Body}')");
            await PostReplyToFallbackThread($"/u/{mentionComment.Author} I was unable to process your [request](https://reddit.com{mentionComment.Context}). Reason: {reason}");
          };

          // Verify that the post is safe to process.
          if (!requestorIsAdmin && !await PostIsSafeToProcess(parentPost, true))
          {
            await onFailedToProcessPost("Post is not safe. See [here](https://www.reddit.com/r/IVAEbot/wiki/index#wiki_limitations) for more information.");
            continue;
          }

          // Get the commands from the mention comment.
          List<IVAECommand> commands;
          try
          {
            commands = IVAECommandFactory.CreateCommands(mentionComment.GetCommandTextFromMention(redditClient.Username));

            IVAECommand speedupCommand = commands.FirstOrDefault(c => c.GetType() == typeof(AdjustSpeedCommand) && ((AdjustSpeedCommand)c).FrameRate > 1);
            if (speedupCommand != null)
            {
              commands.Remove(speedupCommand);
              commands.Insert(0, speedupCommand);
            }

            IVAECommand trimCommand = commands.FirstOrDefault(c => c.GetType() == typeof(TrimCommand));
            if (trimCommand != null)
            {
              commands.Remove(trimCommand);
              commands.Insert(0, trimCommand);
            }
          }
          catch (ArgumentException ex)
          {
            await onFailedToProcessPost($"{ex.Message}  \nSee [here](https://www.reddit.com/r/IVAEbot/wiki/index#wiki_commands) for a list of valid commands.");
            continue;
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.ToString());
            await onFailedToProcessPost($"An error occurred while trying to parse commands.  \nSee [here](https://www.reddit.com/r/IVAEbot/wiki/index#wiki_commands) for a list of valid commands.");
            continue;
          }

          if (commands == null || commands.Count == 0)
          {
            await onFailedToProcessPost("No valid commands.  \nSee [here](https://www.reddit.com/r/IVAEbot/wiki/index#wiki_commands) for a list of valid commands.");
            continue;
          }
          else if (commands.Any(command => commands.Count(cmd => cmd.GetType() == command.GetType()) > 1)) // This is O(n^2), consider something more efficient.
          {
            await onFailedToProcessPost("Multiple commands of same type.");
            continue;
          }

          // Get url to media file.
          Uri mediaUrl = GetMediaUrlFromPost(parentPost);
          if (mediaUrl == null || string.IsNullOrWhiteSpace(mediaUrl.AbsoluteUri))
          {
            await onFailedToProcessPost("Invalid media URL. Remember to reply directly to the link/comment that has the media file.");
            continue;
          }

          // Ensure that the download directory exists.
          if (!System.IO.Directory.Exists(DOWNLOAD_DIR))
            System.IO.Directory.CreateDirectory(DOWNLOAD_DIR);

          string mediaFilePath = null;
          try
          {
            // Download the media file.
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string fileNameWithoutExtension = Guid.NewGuid().ToString();
            string filePathWithoutExtension = System.IO.Path.Combine(DOWNLOAD_DIR, fileNameWithoutExtension);
            string mediaUrlFileExtension = System.IO.Path.GetExtension(mediaUrl.AbsoluteUri).ToLower();
            if (mediaUrlFileExtension == ".jpg" || mediaUrlFileExtension == ".png")
            {
              // Verify that the file is not too large.
              if (!TryGetMediaFileSize(mediaUrl.AbsoluteUri, out long fileSize))
              {
                await onFailedToProcessPost("Failed to get media file size.");
                continue;
              }
              else if (fileSize > settings.FilterSettings.MaximumDownloadFileSizeInMB * 10000000)
              {
                await onFailedToProcessPost("Media file too large.");
                continue;
              }

              mediaFilePath = $"{filePathWithoutExtension}{mediaUrlFileExtension}";

              using (System.Net.WebClient client = new System.Net.WebClient())
              {
                client.DownloadFile(mediaUrl, mediaFilePath);
              }
            }
            else
            {
              YoutubedlProcessRunner youtubedlProcessRunner = new YoutubedlProcessRunner();
              List<string> downloadOutput = youtubedlProcessRunner.Run($"\"{mediaUrl.AbsoluteUri}\" --max-filesize {settings.FilterSettings.MaximumDownloadFileSizeInMB}m -o \"{filePathWithoutExtension}.%(ext)s\" -f mp4");
              mediaFilePath = System.IO.Directory.GetFiles(DOWNLOAD_DIR, $"{fileNameWithoutExtension}*").SingleOrDefault();
            }
            
            if (mediaFilePath == null)
            {
              await onFailedToProcessPost("Failed to download media file. (The file may have been too big.)");
              continue;
            }

            // Execute all commands on the media file.
            long origFileSize = new System.IO.FileInfo(mediaFilePath).Length;
            foreach (IVAECommand command in commands)
            {
              string path = command.Execute(mediaFilePath);
              System.IO.File.Delete(mediaFilePath);

              if (System.IO.File.Exists(path))
              {
                mediaFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mediaFilePath), $"{System.IO.Path.GetFileNameWithoutExtension(mediaFilePath)}{System.IO.Path.GetExtension(path)}");
                System.IO.File.Move(path, mediaFilePath);
              }
            }

            if (!System.IO.File.Exists(mediaFilePath))
            {
              await onFailedToProcessPost($"Failed to create output file.");
              continue;
            }

            double transformedFileSizeInMB = ((double)new System.IO.FileInfo(mediaFilePath).Length) / 1000000;
            MediaManipulation.MediaFileInfo transformedMFI = new MediaManipulation.MediaFileInfo(mediaFilePath);
            if (!transformedMFI.IsValidMediaFile)
            {
              await onFailedToProcessPost($"Output file was broken.");
              continue;
            }
            if (transformedFileSizeInMB > settings.FilterSettings.MaximumUploadFileSizeInMB)
            {
              await onFailedToProcessPost($"Output file ({transformedFileSizeInMB.ToString("N2")}MB) can not be larger than {settings.FilterSettings.MaximumUploadFileSizeInMB}MB.");
              continue;
            }
            else if (transformedMFI.Duration > settings.FilterSettings.MaximumUploadFileDurationInSeconds)
            {
              await onFailedToProcessPost($"Output file ({transformedMFI.Duration.Value.ToString("N2")}s) can not be longer than {settings.FilterSettings.MaximumUploadFileDurationInSeconds} seconds.");
              continue;
            }

            // Upload transformed media file.
            byte[] mediaFileBytes = System.IO.File.ReadAllBytes(mediaFilePath);
            string deleteKey, uploadDestination, uploadLink;
            uploadDestination = "imgur";

            string videoFormat = null;
            if (System.IO.Path.GetExtension(mediaFilePath) == ".mp4")
              videoFormat = "mp4";
            ImgurUploadResponse uploadResponse = await imgurClient.Upload(mediaFileBytes, videoFormat);

            if (uploadResponse == null)
            {
              await onFailedToProcessPost("Failed to upload transformed file.");
              continue;
            }

            deleteKey = uploadResponse.DeleteHash;
            uploadLink = uploadResponse.Link;

            // Respond with link.
            stopwatch.Stop();
            Guid uploadId = Guid.NewGuid();

            string responseText = $"[Direct File Link]({uploadLink})\n\n" +
              $"***\n" +
              $"{stopwatch.Elapsed.TotalMinutes.ToString("N2")} minutes. {((double)origFileSize / 1000000).ToString("N2")}MB -> {transformedFileSizeInMB.ToString("N2")}MB.  \n" +
              $"[More Info](https://www.reddit.com/r/IVAEbot/wiki/index) | [Submit Feedback](https://www.reddit.com/message/compose/?to=TheTollski&subject=IVAEbot%20Feedback) | [Delete](https://www.reddit.com/message/compose/?to=IVAEbot&subject=Command&message=delete%20{uploadId.ToString()})(Requestor Only)";
            string replyCommentName = await redditClient.PostComment(mentionComment.Name, responseText);
            if (replyCommentName == null)
              replyCommentName = await PostReplyToFallbackThread($"/u/{mentionComment.Author} I was unable to repond directly to your [request]({mentionComment.Permalink}) so I have posted my response here.\n\n{responseText}");

            databaseAccessor.InsertUploadLog(new UploadLog
            {
              Deleted = false,
              DeleteKey = deleteKey,
              Id = uploadId,
              PostFullname = parentPost.Name,
              ReplyFullname = replyCommentName,
              RequestorUsername = mentionComment.Author,
              UploadDatetime = DateTime.UtcNow,
              UploadDestination = uploadDestination
            });
          }
          catch (Exception)
          {
            if (!string.IsNullOrWhiteSpace(mediaFilePath) && System.IO.File.Exists(mediaFilePath))
              System.IO.File.Delete(mediaFilePath);

            throw;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception occurred while processing post.");
          Console.WriteLine(ex.ToString());
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
        return new Uri(post.Url);
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
        if (databaseAccessor.GetBlacklistedSubreddit(post.Subreddit) != null) return false; // Subreddit is blacklisted.
        if (post.SubredditSubscribers == null || post.SubredditSubscribers < filterSettings.MinimumSubredditSubscribers) return false; // Link is in a subreddit that's too small.
        if (!filterSettings.ProcessPostsInNonPublicSubreddits && (post.SubredditType == null || post.SubredditType != "public")) return false; // Link is in a subreddit that's not public.

        return true;
      }
      else
        throw new ArgumentException($"Given post '{post.Name}' is not a valid kind '{post.Kind}'.");
    }

    private async Task<string> PostReplyToFallbackThread(string text)
    {
      Tuple<string, DateTime> mostRecentFallbackReplyPost = databaseAccessor.GetMostRecentFallbackRepliesLink();
      string fallbackRepliesLinkName;
      if (mostRecentFallbackReplyPost != null && mostRecentFallbackReplyPost.Item2 > DateTime.Today.AddDays(-6))
        fallbackRepliesLinkName = mostRecentFallbackReplyPost.Item1;
      else
      {
        fallbackRepliesLinkName = await redditClient.Submit(settings.FallbackReplySubreddit, $"Offical {redditClient.Username} Replies ({DateTime.Today.ToLongDateString()})", $"This is for {redditClient.Username} to respond to requests.");
        databaseAccessor.AddFallbackReplyLink(fallbackRepliesLinkName, DateTime.UtcNow);
      }

      return await redditClient.PostComment(fallbackRepliesLinkName, text);
    }
  }
}
