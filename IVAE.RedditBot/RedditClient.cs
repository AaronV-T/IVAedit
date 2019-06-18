using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IVAE.RedditBot.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace IVAE.RedditBot
{
  public class RedditClient
  {
    private const string BASE_URL = "https://oauth.reddit.com";

    private string clientID, encodedClientSecret, username, encodedPassword;
    private HttpClient httpClient;
    private OAuthTokenInfo oAuthTokenInfo;
    private int ratelimitRemaining = int.MaxValue;
    private DateTime ratelimitResetTime = DateTime.MinValue;

    public RedditClient(string clientID, string encodedClientSecret, string username, string encodedPassword)
    {
      this.clientID = clientID ?? throw new ArgumentNullException(nameof(clientID));
      this.encodedClientSecret = encodedClientSecret ?? throw new ArgumentNullException(nameof(encodedClientSecret));
      this.username = username ?? throw new ArgumentNullException(nameof(username));
      this.encodedPassword = encodedPassword ?? throw new ArgumentNullException(nameof(encodedPassword));
    }

    public string Username { get { return username; } }

    public async Task BlockUser(string username)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("name", username);

      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/block_user");
      request.Content = new FormUrlEncodedContent(data);
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit BlockUser Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");
    }

    public async Task DeletePost(string fullName)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("id", fullName);

      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/del");
      request.Content = new FormUrlEncodedContent(data);
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit DeletePost Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");
    }

    public async Task<RedditThing> GetInfoOfCommentOrLink(string subreddit, string fullName)
    {
      List<RedditThing> infos = await GetInfoOfCommentsAndLinks(subreddit, new List<string> { fullName });

      if (infos == null || infos.Count == 0)
        return null;
      if (infos.Count != 1)
        throw new Exception($"GetInfoOfCommentOrLink did not get exactly 1 thing from Reddit. Count: {infos.Count}.");

      return infos[0];
    }

    public async Task<List<RedditThing>> GetInfoOfCommentsAndLinks(string subreddit, List<string> fullNames)
    {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/r/{subreddit}/api/info.json?id={string.Join(",", fullNames)}");
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit Comment/Link Info Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      return GetThingsFromResponse(responseContent);
    }

    public async Task<RedditThing> GetInfoOfUser(string username)
    {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/user/{username}/about");
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit UserAbout Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      List<RedditThing> things = GetThingsFromResponse(responseContent);
      if (things == null || things.Count == 0)
        return null;
      if (things.Count != 1)
        throw new Exception($"GetInfoOfUser did not get exactly 1 thing from Reddit. Count: {things.Count}.");

      return things[0];
    }

    public async Task<List<RedditThing>> GetUnreadMessages()
    {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/message/unread.json");
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit UnreadMessages Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      List<RedditThing> messages = GetThingsFromResponse(responseContent);
      messages.Reverse();
      return messages;
    }

    public async Task MarkMessagesAsRead(List<string> messageNames)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("id", string.Join(",", messageNames));

      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/read_message");
      request.Content = new FormUrlEncodedContent(data);
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit MarkMessagesAsRead Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");
    }

    public async Task<string> PostComment(string parentFullName, string text)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("thing_id", parentFullName);
      data.Add("text", text);

      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/comment");
      request.Content = new FormUrlEncodedContent(data);
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit PostComment Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      dynamic deserializedResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
      if (deserializedResponse == null)
        return null;

      RedditThing commentThing = ((JObject)deserializedResponse.json.data.things[0].data).ToObject<RedditThing>();
      if (commentThing == null)
        return null;

      return commentThing.Name;
    }

    public async Task<string> Submit(string subreddit, string title, string description)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("kind", "self");
      data.Add("sr", subreddit);
      data.Add("text", description);
      data.Add("title", title);

      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/api/submit");
      request.Content = new FormUrlEncodedContent(data); 
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit Submit Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      dynamic deserializedResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
      if (deserializedResponse == null)
        return null;

      return deserializedResponse.json.data.name;
    }

    public async Task<string> Test()
    {
      HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/api/v1/me");
      HttpResponseMessage response = await SendRequestToRedditApi(request);
      string responseContent = await response.Content.ReadAsStringAsync();

      return responseContent;
    }

    private void SetRatelimitInfo(HttpResponseMessage response)
    {
      try
      {
        if (!response.Headers.TryGetValues("x-ratelimit-remaining", out IEnumerable<string> remainingHeaders))
          throw new KeyNotFoundException($"x-ratelimit-remaining header not found.\n\nResponse Headers: {JsonConvert.SerializeObject(response.Headers, Formatting.Indented)}\n\nResponse Content: { JsonConvert.SerializeObject(response.Content, Formatting.Indented) }");

        List<string> ratelimitRemainingHeaders = remainingHeaders.ToList();
        if (ratelimitRemainingHeaders != null && ratelimitRemainingHeaders.Count > 0)
          ratelimitRemaining = (int)float.Parse(ratelimitRemainingHeaders[0]);

        if (!response.Headers.TryGetValues("x-ratelimit-reset", out IEnumerable<string> resetHeaders))
          throw new KeyNotFoundException($"x-ratelimit-reset header not found.\n\nResponse Headers: {JsonConvert.SerializeObject(response.Headers, Formatting.Indented)}\n\nResponse Content: { JsonConvert.SerializeObject(response.Content, Formatting.Indented) }");

        List<string> ratelimitResetHeaders = resetHeaders.ToList();
        if (ratelimitResetHeaders != null && ratelimitResetHeaders.Count > 0)
          ratelimitResetTime = DateTime.Now.AddSeconds(int.Parse(ratelimitResetHeaders[0]));

        Log.Verbose($"RateLimitRemaining: {ratelimitRemaining}.");
      }
      catch (Exception ex)
      {
        if (ex is KeyNotFoundException)
          throw;

        throw new Exception($"Could not get rate limit headers from Reddit response.\n\nResponse Headers: {JsonConvert.SerializeObject(response.Headers, Formatting.Indented)}\n\nResponse Content: { JsonConvert.SerializeObject(response.Content, Formatting.Indented) }");
      }
    }

    private async Task WaitUntilARequestCanBeMade()
    {
      if (httpClient == null || oAuthTokenInfo == null || oAuthTokenInfo.ExpirationDate >= DateTime.Now)
        await CreateNewTokenAndHttpClient();

      if (ratelimitRemaining < 1)
      {
        if (ratelimitResetTime == DateTime.MinValue)
          throw new Exception("Rate limit reset time not correctly set.");

        Log.Information($"Reddit rate limit hit. Waiting until {ratelimitResetTime.ToShortTimeString()}");
        while (DateTime.Now < ratelimitResetTime)
        {
          await Task.Delay(100);
        }
      }
    }

    private async Task CreateNewTokenAndHttpClient()
    {
      oAuthTokenInfo = await GetOAuthToken();

      httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(oAuthTokenInfo.TokenType, oAuthTokenInfo.AccessToken);
      httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("IVAedit", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
    }

    private async Task<OAuthTokenInfo> GetOAuthToken()
    {
      return await GetOAuthToken(
        this.clientID,
        Encoding.Unicode.GetString(Convert.FromBase64String(this.encodedClientSecret)),
        this.username,
        Encoding.Unicode.GetString(Convert.FromBase64String(this.encodedPassword)));
    }

    private async Task<OAuthTokenInfo> GetOAuthToken(string appId, string clientSecret, string userName, string password)
    {
      using (HttpClient client = new HttpClient())
      {
        byte[] authorizationBytes = Encoding.ASCII.GetBytes($"{appId}:{clientSecret}");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", Convert.ToBase64String(authorizationBytes));

        while (true)
        {
          try
          {
            HttpResponseMessage response = await client.PostAsync($"https://www.reddit.com/api/v1/access_token?grant_type=password&username={userName}&password={password}", null);

            string responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
              throw new HttpRequestException($"Error requesting OAuth token: '{responseContent}'.");

            Dictionary<string, string> deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            if (deserializedResponse.ContainsKey("error"))
            {
              throw new Exception($"Error getting OAuthToken from Reddit: '{deserializedResponse["error"]}'.");
            }

            return new OAuthTokenInfo
            {
              AccessToken = deserializedResponse["access_token"],
              TokenType = deserializedResponse["token_type"],
              ExpirationDate = DateTime.Now.AddSeconds(int.Parse(deserializedResponse["expires_in"])),
              Scope = deserializedResponse["scope"]
            };
          }
          catch (Exception ex) // TODO: Figure out what type of exception to catch for when reddit is temporarily down and only catch that type.
          {
            Log.Warning(ex, $"Exception caught in {this.GetType().Name}.GetOAuthToken(). Will retry in a moment.");
            await Task.Delay(5000);
          }
        }
      }
    }

    private List<RedditThing> GetThingsFromResponse(string responseContent)
    {
      Dictionary<string, object> deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
      List<RedditThing> things = new List<RedditThing>();

      if (((JObject)deserializedResponse["data"]).ContainsKey("children"))
      {
        // Response data is a Listing.
        RedditListing listing = ((JObject)deserializedResponse["data"]).ToObject<RedditListing>();
        foreach (JObject jObj in (JArray)listing.Children)
        {
          Dictionary<string, object> objectDict = jObj.ToObject<Dictionary<string, object>>();
          if (!objectDict.ContainsKey("data") || objectDict["data"] == null)
            continue;

          things.Add(((JObject)objectDict["data"]).ToObject<RedditThing>());
        }
      }
      else
      {
        // Response data is probably a Thing.
        things.Add(((JObject)deserializedResponse["data"]).ToObject<RedditThing>());
      }

      return things;
    }

    private async Task<HttpResponseMessage> SendRequestToRedditApi(HttpRequestMessage request)
    {
      while (true)
      {
        await WaitUntilARequestCanBeMade();
        HttpResponseMessage response = await httpClient.SendAsync(request);

        try
        {
          SetRatelimitInfo(response);
          return response;
        }
        catch (Exception ex)
        {
          if (!(ex is KeyNotFoundException))
            throw;

          Log.Warning(ex, "Reddit seems to be temporarily down. Retrying request in a moment.");

          await Task.Delay(5000);
        }
      }
    }
  }
}
