using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot
{
  public class Controller
  {
    private const string DOWNLOAD_DIR = "AutomatedFileDownloads";

    public async void Start()
    {
      if (System.IO.Directory.Exists(DOWNLOAD_DIR))
        System.IO.Directory.Delete(DOWNLOAD_DIR, true);

      try
      {
        dynamic secrets = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText("secrets.json"));
        RedditClient redditClient = new RedditClient((string)secrets.RedditClient.ID, (string)secrets.RedditClient.EncodedSecret, (string)secrets.RedditBot.Username, (string)secrets.RedditBot.EncodedPassword);
        ImgurClient imgurClient = new ImgurClient((string)secrets.ImgurClient.ID, (string)secrets.ImgurClient.EncodedSecret);

        Console.WriteLine("Getting unread messages...");
        List<RedditThing> messages = await redditClient.GetUnreadMessages();

        Console.WriteLine("Filtering out messages that aren't comments with user mentions...");
        List<RedditThing> comments = await FilterMentionMessagesAndMarkRestRead(redditClient, messages);

        Console.WriteLine($"Getting parents of {comments.Count} comments...");
        Dictionary<RedditThing, RedditThing> commentsWithParents = await GetParentsOfComments(redditClient, messages);

        Console.WriteLine($"Processing {commentsWithParents.Count} posts...");
        await ProcessPosts(redditClient, commentsWithParents, imgurClient);

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

      Console.Read();
    }

    private async Task<List<RedditThing>> FilterMentionMessagesAndMarkRestRead(RedditClient redditClient, List<RedditThing> unreadMessages)
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

    private async Task<Dictionary<RedditThing, RedditThing>> GetParentsOfComments(RedditClient redditClient, List<RedditThing> comments)
    {
      Dictionary<RedditThing, RedditThing> messagesWithParents = new Dictionary<RedditThing, RedditThing>();
      foreach (RedditThing message in comments)
        messagesWithParents.Add(message, await redditClient.GetInfoOfCommentOrLink(message.Subreddit, message.ParentId));

      return messagesWithParents;
    }

    private async Task ProcessPosts(RedditClient redditClient, Dictionary<RedditThing, RedditThing> commentsWithParents, ImgurClient imgurClient)
    {
      foreach(var kvp in commentsWithParents)
      {
        try
        {
          RedditThing mentionComment = kvp.Key;
          RedditThing parentPost = kvp.Value;

          // Verify that the post is old enough.
          if (parentPost.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Now.AddMinutes(-30))
          {
            Console.WriteLine($"Temporarily skipping {mentionComment.Name}: Post is too recent. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          //await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });

          // Verify that the post is safe to process.
          if (!await PostIsSafeToProcess(redditClient, parentPost, true))
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Post is not safe to process. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Get the commands from the mention comment.
          List<IVAECommand> commands = IVAECommandFactory.CreateCommands(mentionComment.Body);
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
          string mediaUrl = GetMediaUrlFromPost(parentPost);
          if (string.IsNullOrWhiteSpace(mediaUrl))
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Invalid media url. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Verify that the file is not too large.
          if (!TryGetMediaFileSize(mediaUrl, out long fileSize) || fileSize > 100000000)
          {
            Console.WriteLine($"Skipping {mentionComment.Name}: Bad media file size. ({mentionComment.Author}: '{mentionComment.Body}')");
            continue;
          }

          // Ensure that the download directory exists.
          if (!System.IO.Directory.Exists(DOWNLOAD_DIR))
            System.IO.Directory.CreateDirectory(DOWNLOAD_DIR);

          string downloadFilePath = null;
          try
          {
            System.Diagnostics.Stopwatch downloadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Download the media file.
            int mediaUrlFileNameStartIndex = mediaUrl.LastIndexOf('/') + 1;
            downloadFilePath = System.IO.Path.Combine(
              DOWNLOAD_DIR,
              $"{Guid.NewGuid()}_" +
              $"{mediaUrl.Substring(mediaUrlFileNameStartIndex, Math.Min(mediaUrl.Length - mediaUrlFileNameStartIndex, 20))}" +
              $"{System.IO.Path.GetExtension(mediaUrl)}");
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
              client.DownloadFile(mediaUrl, downloadFilePath);
            }
            downloadStopwatch.Stop();

            // Execute all commands on the media file.
            System.Diagnostics.Stopwatch transformStopwatch = System.Diagnostics.Stopwatch.StartNew();
            foreach (IVAECommand command in commands)
            {
              string path = command.Execute(downloadFilePath);
              System.IO.File.Delete(downloadFilePath);
              System.IO.File.Move(path, downloadFilePath);
            }
            transformStopwatch.Stop();

            // Upload altered media file.
            System.Diagnostics.Stopwatch uploadStopwatch = System.Diagnostics.Stopwatch.StartNew();
            string videoFormat = null;
            if (System.IO.Path.GetExtension(downloadFilePath) == ".mp4")
              videoFormat = "mp4";
            var res = await imgurClient.Upload(System.IO.File.ReadAllBytes(downloadFilePath), videoFormat);
            uploadStopwatch.Stop();

            // Respond with link.
            bool? commentPosted = await redditClient.PostComment(mentionComment.Name, $"{res.Link}\n\nThis is a bot in development. Downloaded in {downloadStopwatch.Elapsed.TotalMinutes.ToString("N2")}m, transfomed in {transformStopwatch.Elapsed.TotalMinutes.ToString("N2")}m, uploaded in {uploadStopwatch.Elapsed.TotalMinutes.ToString("N2")}m.");
            Console.WriteLine($"Posted: {commentPosted != null && commentPosted.Value}");

            if (commentPosted != null && commentPosted.Value)
              await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });
            else
              await imgurClient.Delete(res.DeleteHash);
          }
          catch (Exception)
          {
            if (!string.IsNullOrWhiteSpace(downloadFilePath) && System.IO.File.Exists(downloadFilePath))
              System.IO.File.Delete(downloadFilePath);

            throw;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Exception occurred when processing post. {ex}");
        }
      }
    }

    private string GetMediaUrlFromPost(RedditThing post)
    {
      // Comment
      if (post.Kind == "t1")
      {
        // Return the first link found in the body.
        string[] splitBody = post.Body.Split();
        foreach(string s in splitBody)
        {
          if (Uri.TryCreate(s, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return s;
        }
        return null;
      }
      // Link
      else if (post.Kind == "t3")
      {
        return post.Url;
      }
      else
        throw new ArgumentException($"Given post '{post}' is not a valid kind.");
    }

    private async Task<bool> PostIsSafeToProcess(RedditClient redditClient, RedditThing post, bool isRootPost)
    {
      // If this is the post with the media file to manipulate: ...
      if (isRootPost)
      {
        if (post.Score < 1) return false; // Post's score is too low.
        if (post.Kind == "t1") {
          if (post.Edited == null || post.Edited.Value) return false; // Comment is edited.
          if (post.Body.ToLower().Contains("nsfw") || post.Body.ToLower().Contains("nsfl")) return false; // Comment is self-marked as NSFW/NSFL.
        }

        RedditThing user = await redditClient.GetInfoOfUser(post.Author);
        if (user.CommentKarma == null || user.LinkKarma == null || user.CommentKarma + user.LinkKarma < 1000) return false; // Posting user's account doesn't have enough karma.
        if (user.CreatedUtc == null || user.CreatedUtc.Value.UnixTimeToDateTime() > DateTime.Today.AddMonths(-1)) return false; // Posting user's account isn't old enough.
      }

      // If this post is a comment: ...
      if (post.Kind == "t1")
      {
        return await PostIsSafeToProcess(redditClient, await redditClient.GetInfoOfCommentOrLink(post.Subreddit, post.LinkId), false); // Check this comment's parent post.
      }
      // If this post is a link: ...
      else if (post.Kind == "t3")
      {
        if (post.Over18 == null || post.Over18.Value) return false; // Link is flagged NSFW.
        if (post.SubredditSubscribers == null || post.SubredditSubscribers < 5000) return false; // Link is in a subreddit that's too small.
        if (post.SubredditType == null || post.SubredditType != "public") return false; // Link is in a subreddit that's not public.

        return true;
      }
      else
        throw new ArgumentException($"Given post '{post.Name}' is not a valid kind '{post.Kind}'.");
    }

    private bool TryGetMediaFileSize(string url, out long fileSize)
    {
      System.Net.WebRequest request = System.Net.WebRequest.Create(url);
      request.Method = "HEAD";
      using (System.Net.WebResponse response = request.GetResponse())
      {
        return long.TryParse(response.Headers.Get("Content-Length"), out fileSize);
      }
    }
  }
}
