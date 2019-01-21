using System;
using System.Collections.Generic;
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
        RedditClient redditClient = new RedditClient();
        StringBuilder sb = new StringBuilder();

        Console.WriteLine("Getting unread messages...");
        List<RedditThing> messages = await redditClient.GetUnreadMessages();

        Console.WriteLine("Filtering out messages that aren't comments with user mentions...");
        List<RedditThing> comments = await FilterMentionMessagesAndMarkRestRead(redditClient, messages);

        Console.WriteLine($"Getting parents of {comments.Count} comments...");
        Dictionary<RedditThing, RedditThing> commentsWithParents = await GetParentsOfComments(redditClient, messages);

        Console.WriteLine($"Processing {commentsWithParents.Count} posts...");
        await ProcessPosts(redditClient, commentsWithParents);

        sb.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(commentsWithParents, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore }));
        System.IO.File.WriteAllText("output.txt", sb.ToString());
        System.Diagnostics.Process.Start("output.txt");
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
        messagesWithParents.Add(message, await redditClient.GetThingInfo(message.Subreddit, message.ParentId));

      return messagesWithParents;
    }

    private async Task ProcessPosts(RedditClient redditClient, Dictionary<RedditThing, RedditThing> commentsWithParents)
    {
      foreach(var kvp in commentsWithParents)
      {
        RedditThing mentionComment = kvp.Key;
        RedditThing parentPost = kvp.Value;

        //await redditClient.MarkMessagesAsRead(new List<string> { mentionComment.Name });

        // Verify that the post is safe to process.
        if (!await PostIsSafeToProcess(redditClient, parentPost))
          continue;

        // Verify that the mention comment has at least one valid command.
        List<IVAECommand> commands = IVAECommandFactory.CreateCommands(kvp.Key.Body);
        if (commands == null || commands.Count == 0)
        {
          Console.WriteLine("No valid commands.");
          continue;
        }

        // Get url to media file.
        string mediaUrl = GetMediaUrlFromPost(parentPost);
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
          Console.WriteLine("Invalid media url.");
          continue;
        }

        // Verify that the file is not too large.
        if (!TryGetMediaFileSize(mediaUrl, out long fileSize) || fileSize > 100000000)
        {
          Console.WriteLine("Bad media file size.");
          continue;
        }

        // Ensure that the download directory exists.
        if (!System.IO.Directory.Exists(DOWNLOAD_DIR))
          System.IO.Directory.CreateDirectory(DOWNLOAD_DIR);

        string downloadFilePath = null;
        try
        {
          // Download the media file.
          int mediaUrlFileNameStartIndex = mediaUrl.LastIndexOf('/') + 1;
          downloadFilePath = $@"{DOWNLOAD_DIR}\{Guid.NewGuid()}_{mediaUrl.Substring(mediaUrlFileNameStartIndex, Math.Min(mediaUrl.Length - mediaUrlFileNameStartIndex, 20))}";
          using (System.Net.WebClient client = new System.Net.WebClient())
          {
            client.DownloadFile(mediaUrl, downloadFilePath);
          }

          foreach (IVAECommand command in commands)
          {
            string path = command.Execute(downloadFilePath);
            System.IO.File.Delete(downloadFilePath);
            System.IO.File.Move(path, downloadFilePath);
          }
        }
        catch(Exception)
        {
          if (!string.IsNullOrWhiteSpace(downloadFilePath) && System.IO.File.Exists(downloadFilePath))
            System.IO.File.Delete(downloadFilePath);

          throw;
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

    private async Task<bool> PostIsSafeToProcess(RedditClient redditClient, RedditThing post)
    {
      // Comment
      if (post.Kind == "t1")
      {
        if (post.Body.ToLower().Contains("nsfw") || post.Body.ToLower().Contains("nsfl"))
          return false;

        return await PostIsSafeToProcess(redditClient, await redditClient.GetThingInfo(post.Subreddit, post.LinkId));
      }
      // Link
      else if (post.Kind == "t3")
      {
        return !post.Over18.Value;
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
