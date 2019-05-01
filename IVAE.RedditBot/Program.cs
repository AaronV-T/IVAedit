﻿using IVAE.RedditBot.DTO;
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
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
      }
    }

    private static async Task RunProcessLoop()
    {
      try
      {
        Settings settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText("settings.json"));
        dynamic secrets = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText("secrets.json"));
        RedditClient redditClient = new RedditClient((string)secrets.RedditClient.ID, (string)secrets.RedditClient.EncodedSecret, (string)secrets.RedditAccount.Username, (string)secrets.RedditAccount.EncodedPassword);
        ImgurClient imgurClient = new ImgurClient((string)secrets.ImgurClient.ID, (string)secrets.ImgurClient.EncodedSecret);

        DatabaseAccessor databaseAccessor = new DatabaseAccessor(System.Configuration.ConfigurationManager.ConnectionStrings["IVAeditDB"].ConnectionString);
        databaseAccessor.EnsureDatabaseIsUpToDate();

        MessageProcessor messageProcessor = new MessageProcessor(databaseAccessor, imgurClient, redditClient, settings);
        CleanupManager cleanupManager = new CleanupManager(databaseAccessor, imgurClient, redditClient, settings);

        int shortCleanups = 0;
        while (!exit)
        {
          int processLoopsToRunBetweenCleanups = 5;
          for (int i = 0; i < processLoopsToRunBetweenCleanups; i++)
          {
            if (exit)
              break;

            Console.WriteLine("Processing messages...");
            await messageProcessor.ProcessUnreadMessages();

            if (exit)
              break;

            if (i < processLoopsToRunBetweenCleanups - 1)
            {
              int secondsToPause = 300;
              Console.WriteLine($"Pausing for {secondsToPause / 60} minutes...");
              for (int j = 0; j < secondsToPause; j++)
              {
                if (exit)
                  break;

                await Task.Delay(1000);
              }
            }
          }

          if (exit)
            break;

          DateTime cleanupCutoff;
          if (shortCleanups < 10)
          {
            cleanupCutoff = DateTime.UtcNow.AddHours(-3);
            shortCleanups++;
          }
          else
          {
            cleanupCutoff = DateTime.UtcNow.AddDays(-7);
            shortCleanups = 0;
          }
          
          Console.WriteLine($"Cleaning up posts since {cleanupCutoff.ToShortDateString()} {cleanupCutoff.ToShortTimeString()}...");
          await cleanupManager.CleanupPosts(cleanupCutoff);
            
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }
  }
}
