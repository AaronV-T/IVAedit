﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IVAE.RedditBot.DTO;
using Serilog;

namespace IVAE.RedditBot
{
  public class MessageProcessor
  {
    private const string DOWNLOAD_DIR = "AutomatedFileDownloads";

    private CleanupManager cleanupManager;
    private DatabaseAccessor databaseAccessor;
    private GfycatClient gfycatClient;
    private ImgurClient imgurClient;
    private RedditClient redditClient;
    private Settings settings;

    public MessageProcessor(CleanupManager cleanupManager, DatabaseAccessor databaseAccessor, GfycatClient gfycatClient, ImgurClient imgurClient, RedditClient redditClient, Settings settings)
    {
      this.cleanupManager = cleanupManager ?? throw new ArgumentNullException(nameof(cleanupManager));
      this.databaseAccessor = databaseAccessor ?? throw new ArgumentNullException(nameof(databaseAccessor));
      this.gfycatClient = gfycatClient ?? throw new ArgumentNullException(nameof(gfycatClient));
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
        Log.Information("Getting unread messages.");
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
          Log.Information($"Ignoring {messageNamesToMarkRead.Count} messages.");
          await redditClient.MarkMessagesAsRead(messageNamesToMarkRead);
        }

        if (commandMessages.Count > 0)
        {
          Log.Information($"Processing {commandMessages.Count} command messages.");
          await ProcessCommands(commandMessages);
        }

        if (requestComments.Count > 0)
        {
          Log.Information($"Processing {requestComments.Count} request comments.");
          await ProcessRequests(requestComments);
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Exception caught in {nameof(MessageProcessor)}.ProcessUnreadMessages.");
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
      foreach (RedditThing request in requests)
      {
        RedditThing immediateParent = await redditClient.GetInfoOfCommentOrLink(request.Subreddit, request.ParentId);

        // Determine whether to use the root link or the request's immediate parent.
        RedditThing parentPost;
        List<string> splitRequest = request.GetCommandTextFromMention(redditClient.Username).Split().ToList();
        if (immediateParent.Kind == "t3" || (splitRequest.Count > 1 && splitRequest[1].ToLower() == "!immediate"))
          parentPost = immediateParent;
        else 
          parentPost = await redditClient.GetInfoOfCommentOrLink(request.Subreddit, immediateParent.LinkId);

        requestsWithParents.Add(request, parentPost);
      }

      // Process each request.
      Dictionary<string, List<RedditThing>> processedRequestsByUser = new Dictionary<string, List<RedditThing>>();
      foreach (var kvp in requestsWithParents)
      {
        try
        {
          RedditThing mentionComment = kvp.Key;
          RedditThing parentPost = kvp.Value;

          // If we have already processed a request from this user in this batch of requests:...
          if (processedRequestsByUser.ContainsKey(mentionComment.Author))
          {
            // If this is a duplicate request: discard it.
            if (processedRequestsByUser[mentionComment.Author].Any(rt => rt.ParentId == mentionComment.ParentId && rt.Body == mentionComment.Body))
            {
              Log.Information($"Skipping {mentionComment.Name}: Post is a duplicate. ({mentionComment.Author}: '{mentionComment.Body}')");
              await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });
              continue;
            }
            // If we have already processed enough requests from this user in this batch: temporarily skip it.
            else if (processedRequestsByUser[mentionComment.Author].Count >= 2)
            {
              Log.Information($"Temporarily skipping {mentionComment.Name}: Too many recent requests. ({mentionComment.Author}: '{mentionComment.Body}')");
              continue;
            }

            processedRequestsByUser[mentionComment.Author].Add(mentionComment);
          }
          else
            processedRequestsByUser.Add(mentionComment.Author, new List<RedditThing> { mentionComment });

          bool requestorIsAdmin = settings.Administrators.Contains(mentionComment.Author);

          // Verify that the media post is old enough.
          if (!requestorIsAdmin && parentPost.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Now.ToUniversalTime().AddMinutes(-settings.FilterSettings.MinimumPostAgeInMinutes))
          {
            Log.Information($"Temporarily skipping {mentionComment.Name}: Post is too recent. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });

          // Verify that the requestor isn't blacklisted.
          if (databaseAccessor.GetBlacklistedUser(mentionComment.Author) != null)
          {
            Log.Information($"Skipping {mentionComment.Name}: Requestor is blacklisted. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          Func<string, Task> onFailedToProcessPost = async (reason) =>
          {
            Log.Information($"Skipping {mentionComment.Name}: {reason} ({mentionComment.Author}: '{mentionComment.Body}')");
            await PostReplyToFallbackThread($"/u/{mentionComment.Author} I was unable to process your [request](https://reddit.com{mentionComment.Context}).  \nReason: {reason}");
          };

          // Verify that the post is safe to process.
          Tuple<bool, string> postSafetyInfo = await PostIsSafeToProcess(parentPost, true);
          if (!requestorIsAdmin && !postSafetyInfo.Item1)
          {
            await onFailedToProcessPost($"{postSafetyInfo.Item2} See [here](https://www.reddit.com/r/IVAEbot/wiki/index#wiki_limitations) for more information.");
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
            Log.Warning(ex.ToString());
            await onFailedToProcessPost($"An error occurred while trying to parse commands.  \nSee [here](https://www.reddit.com/r/IVAEbot/wiki/index#wiki_commands) for a list of valid commands.");
            continue;
          }

          if (commands != null && commands.Any(command => commands.Count(cmd => cmd.GetType() == command.GetType()) > 1)) // This is O(n^2), consider something more efficient.
          {
            await onFailedToProcessPost("Multiple commands of same type.");
            continue;
          }

          // Get url to media file.
          Uri mediaUrl = GetMediaUrlFromPost(parentPost);
          if (mediaUrl == null || string.IsNullOrWhiteSpace(mediaUrl.AbsoluteUri))
          {
            await onFailedToProcessPost("Invalid media URL.");
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
            else if (mediaUrl.Host == "v.redd.it")
            {
              // Temporary override for v.redd.it links until youtube-dl is fixed.
              mediaFilePath = $"{filePathWithoutExtension}.mp4";

              using (System.Net.WebClient client = new System.Net.WebClient())
              {
                client.DownloadFile(mediaUrl, mediaFilePath);

                try
                {
                  string audioUrl = $"{mediaUrl.AbsoluteUri.Substring(0, mediaUrl.AbsoluteUri.LastIndexOf("/"))}/audio";
                  string audioFilePath = System.IO.Path.Combine(DOWNLOAD_DIR, $"{System.IO.Path.GetFileNameWithoutExtension(mediaFilePath)}_audio");
                  string combinedFilePath = System.IO.Path.Combine(DOWNLOAD_DIR, "combined.mp4");
                  client.DownloadFile(audioUrl, audioFilePath);
                  new MediaManipulation.FFmpegProcessRunner().Run($"-i {mediaFilePath} -i {audioFilePath} -acodec copy -vcodec copy \"{combinedFilePath}\"");
                  System.IO.File.Delete(audioFilePath);
                  System.IO.File.Delete(mediaFilePath);
                  System.IO.File.Move(combinedFilePath, mediaFilePath);
                }
                catch (Exception) { }
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
            string deleteKey, uploadDestination, uploadPath, uploadLink;
            if (!transformedMFI.HasVideo || transformedMFI.Duration <= 30)
            {
              uploadDestination = "imgur";

              string videoFormat = null;
              if (System.IO.Path.GetExtension(mediaFilePath) == ".mp4")
                videoFormat = "mp4";
              ImgurUploadResponse imgurUploadResponse = await imgurClient.Upload(mediaFileBytes, videoFormat);

              if (imgurUploadResponse == null)
              {
                await onFailedToProcessPost("Failed to upload transformed file.");
                continue;
              }

              deleteKey = imgurUploadResponse.DeleteHash;
              uploadLink = imgurUploadResponse.Link;
              uploadPath = imgurUploadResponse.Name;
            }
            else
            {
              uploadDestination = "gfycat";
              string gfyname = await gfycatClient.Upload(mediaFileBytes);

              if (gfyname == null)
              {
                await onFailedToProcessPost("Failed to upload transformed file.");
                continue;
              }

              deleteKey = gfyname;
              uploadLink = $"https://giant.gfycat.com/{gfyname}.mp4";
              uploadPath = gfyname;
            }

            // Respond with link.
            stopwatch.Stop();
            Guid uploadId = Guid.NewGuid();

            string responseText = $"[Direct File Link]({uploadLink})\n\n" +
              $"***\n" +
              $"Finished in {(stopwatch.Elapsed.TotalMinutes >= 1 ? $"{stopwatch.Elapsed.ToString("mm")} minutes " : "" )}{stopwatch.Elapsed.ToString("ss")} seconds. {((double)origFileSize / 1000000).ToString("N2")}MB -> {transformedFileSizeInMB.ToString("N2")}MB.  \n" +
              $"[How To Use](https://www.reddit.com/r/IVAEbot/wiki/index) | [Submit Feedback](https://www.reddit.com/message/compose/?to=TheTollski&subject=IVAEbot%20Feedback) | [Delete](https://www.reddit.com/message/compose/?to=IVAEbot&subject=Command&message=delete%20{uploadId.ToString()}) (Requestor Only)  \n" +
              $"^^I ^^am ^^a ^^bot ^^in ^^beta ^^testing ^^and ^^need ^^more ^^[testers](https://www.reddit.com/r/IVAEbot/comments/bp3aha/testers_needed/). ^^Feel ^^free ^^to ^^learn ^^what ^^I ^^can ^^do ^^and ^^summon ^^me.";
            string replyCommentName = await redditClient.PostComment(mentionComment.Name, responseText);
            if (replyCommentName == null)
              replyCommentName = await PostReplyToFallbackThread($"/u/{mentionComment.Author} I was unable to repond directly to your [request]({mentionComment.Permalink}) so I have posted my response here.\n\n{responseText}");

            databaseAccessor.InsertUploadLog(new UploadLog
            {
              Id = uploadId,
              PostFullname = parentPost.Name,
              ReplyDeleted = false,
              ReplyFullname = replyCommentName,
              RequestorUsername = mentionComment.Author,
              UploadDatetime = DateTime.UtcNow,
              UploadDeleted = false,
              UploadDeleteKey = deleteKey,
              UploadDestination = uploadDestination,
              UploadPath = uploadPath
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
          Log.Error(ex, "Exception occurred while processing a request.");
        }
      }
    }

    private static Uri GetMediaUrlFromPost(RedditThing post)
    {
      // Comment or Text Post
      if (post.Kind == "t1" || (post.IsSelf != null && post.IsSelf.Value))
      {
        string text = post.Kind == "t1" ? post.Body : post.Selftext;

        // Return the first link found in the body.
        string[] splitText = text.Split(' ', '(', ')', '\n');
        foreach (string s in splitText)
        {
          if (Uri.TryCreate(s, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return new Uri(s);
        }

        return null;
      }
      // Link
      else if (post.Kind == "t3")
      {
        // Temporary override for v.redd.it links until youtube-dl is fixed.
        if (post.Url.ToLower().Contains("v.redd.it") && post.Media != null)
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

    private async Task<Tuple<bool,string>> PostIsSafeToProcess(RedditThing post, bool isRootPost)
    {
      FilterSettings filterSettings = settings.FilterSettings;

      // If this is the post with the media file to manipulate: ...
      if (isRootPost)
      {
        if (post.Score < filterSettings.MinimumPostScore)
          return new Tuple<bool, string>(false, $"Post's score is too low (Post Score: {post.Score}; Minimum Required Score: {filterSettings.MinimumPostScore}).");

        if (post.Kind == "t1")
        {
          if (!filterSettings.ProcessEditedComments && (post.Edited == null || post.Edited.Value))
            return new Tuple<bool, string>(false, $"Post is edited.");
          if (!filterSettings.ProcessNSFWContent && (post.Body.ToLower().Contains("nsfw") || post.Body.ToLower().Contains("nsfl")))
            return new Tuple<bool, string>(false, $"Post is self-marked as NSFW/NSFL.");
        }

        RedditThing user = await redditClient.GetInfoOfUser(post.Author);
        if (user.CommentKarma == null || user.LinkKarma == null || user.CommentKarma + user.LinkKarma < filterSettings.MinimumPostingAccountKarma)
          return new Tuple<bool, string>(false, $"Posting user's account doesn't have enough karma (Karma: {user.CommentKarma + user.LinkKarma}; Minimum Required Karma: {filterSettings.MinimumPostingAccountKarma}).");
        if (user.CreatedUtc == null || user.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Today.AddDays(-filterSettings.MinimumPostingAccountAgeInDays))
          return new Tuple<bool, string>(false, $"Posting user's account isn't old enough (must be at least {filterSettings.MinimumPostingAccountAgeInDays} days old).");
      }

      // If this post is a comment: ...
      if (post.Kind == "t1")
      {
        return await PostIsSafeToProcess(await redditClient.GetInfoOfCommentOrLink(post.Subreddit, post.LinkId), false); // Check this comment's parent post.
      }
      // If this post is a link: ...
      else if (post.Kind == "t3")
      {
        if (!filterSettings.ProcessNSFWContent && (post.Over18 == null || post.Over18.Value))
          return new Tuple<bool, string>(false, $"Link is NSFW.");
        if (databaseAccessor.GetBlacklistedSubreddit(post.Subreddit) != null)
          return new Tuple<bool, string>(false, $"Subreddit is blacklisted.");
        if (post.SubredditSubscribers == null || post.SubredditSubscribers < filterSettings.MinimumSubredditSubscribers)
          return new Tuple<bool, string>(false, $"Subreddit is too small (Subscribers: {post.SubredditSubscribers}; Minimum Required Subscribers: {filterSettings.MinimumSubredditSubscribers}).");
        if (!filterSettings.ProcessPostsInNonPublicSubreddits && (post.SubredditType == null || post.SubredditType != "public"))
          return new Tuple<bool, string>(false, $"Subreddit is not public.");

        return new Tuple<bool, string>(true, "Post seems safe.");
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
