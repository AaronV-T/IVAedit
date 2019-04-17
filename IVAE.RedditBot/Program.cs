using IVAE.RedditBot.DTO;
using System;
using System.Threading.Tasks;

namespace IVAE.RedditBot
{
  class Program
  {
    static bool exit = false;


    static void Main(string[] args)
    {
      try
      {

        Task loopTask = Task.Run(RunProcessLoop);
        while (Console.ReadKey().Key != ConsoleKey.Escape) {}

        exit = true;
        Console.WriteLine("Waiting to exit until loop is ready...");
        loopTask.Wait();
      }
      catch(Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      Console.WriteLine("Press any key to exit.");
      Console.ReadKey();
    }

    private static async Task RunProcessLoop()
    {
      Settings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText("settings.json"));
      dynamic secrets = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText("secrets.json"));
      RedditClient redditClient = new RedditClient((string)secrets.RedditClient.ID, (string)secrets.RedditClient.EncodedSecret, (string)secrets.RedditBot.Username, (string)secrets.RedditBot.EncodedPassword);
      ImgurClient imgurClient = new ImgurClient((string)secrets.ImgurClient.ID, (string)secrets.ImgurClient.EncodedSecret);

      DatabaseAccessor databaseAccessor = new DatabaseAccessor(System.Configuration.ConfigurationManager.ConnectionStrings["IVAeditDB"].ConnectionString);
      databaseAccessor.EnsureDatabaseIsUpToDate();

      MessageProcessor messageProcessor = new MessageProcessor(databaseAccessor, imgurClient, redditClient, settings);
      CleanupManager cleanupManager = new CleanupManager(databaseAccessor, imgurClient, redditClient, settings);

      while (!exit)
      {
        for (int i = 0; i < 100; i++)
        {
          if (exit)
            break;

          Console.WriteLine("Processing messages...");
          await messageProcessor.ProcessUnreadMessages();

          if (exit)
            break;

          Console.WriteLine("Pausing for 5 minutes...");
          for (int j = 0; j < 300; j++)
          {
            if (exit)
              break;

            await Task.Delay(1000);
          }
        }

        if (exit)
          break;

        Console.WriteLine("Cleaning up posts...");
        cleanupManager.SanitizePosts().Wait();
      }
    }
  }
}
