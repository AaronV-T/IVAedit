using System;


namespace IVAE.RedditBot
{
  class Program
  {
    static void Main(string[] args)
    {
      try
      {
        Controller controller = new Controller();
        controller.Start();
      }
      catch(Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      Console.Read();
    }
  }
}
