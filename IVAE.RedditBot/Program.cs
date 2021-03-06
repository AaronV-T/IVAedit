﻿using IVAE.RedditBot.DTO;
using System;
using System.Threading.Tasks;
using Serilog;

namespace IVAE.RedditBot
{
  class Program
  {
    static bool exit = false;

    static void Main(string[] args)
    {
      try
      {
        MediaManipulation.Logger.Init();

        Task loopTask = Task.Run(RunProcessLoop);
        while (Console.ReadKey().Key != ConsoleKey.Escape) { }

        exit = true;
        Console.WriteLine("Waiting to exit until loop is ready...");
        loopTask.Wait();
      }
      catch (Exception ex)
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
        GfycatClient gfycatClient = new GfycatClient((string)secrets.GfycatClient.ID, (string)secrets.GfycatClient.EncodedSecret, (string)secrets.GfycatAccount.Username, (string)secrets.GfycatAccount.EncodedPassword);

        DatabaseAccessor databaseAccessor = new DatabaseAccessor(System.Configuration.ConfigurationManager.ConnectionStrings["IVAeditDB"].ConnectionString);
        databaseAccessor.EnsureDatabaseIsUpToDate();

        CleanupManager cleanupManager = new CleanupManager(databaseAccessor, gfycatClient, imgurClient, redditClient, settings);
        MessageProcessor messageProcessor = new MessageProcessor(cleanupManager, databaseAccessor, gfycatClient, imgurClient, redditClient, settings);

        int shortCleanups = 0;
        while (!exit)
        {
          int processLoopsToRunBetweenCleanups = 20;
          for (int i = 0; i < processLoopsToRunBetweenCleanups; i++)
          {
            if (exit)
              break;

            Log.Information("Processing messages...");
            await messageProcessor.ProcessUnreadMessages();

            if (exit)
              break;

            if (i < processLoopsToRunBetweenCleanups - 1)
            {
              int secondsToPause = 60;
              Log.Information($"Pausing for {secondsToPause / 60} minutes...");
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

          Log.Information($"Cleaning up posts since {cleanupCutoff.ToLocalTime().ToShortDateString()} {cleanupCutoff.ToLocalTime().ToShortTimeString()}...");
          await cleanupManager.CleanupPosts(cleanupCutoff);
            
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Exception caught in {nameof(Program)}.RunProcessLoop().");
      }
    }
  }
}
