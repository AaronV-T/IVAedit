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

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/block_user", content);
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit BlockUser Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");
    }

    public async Task DeletePost(string fullName)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("id", fullName);

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/del", content);
      SetRatelimitInfo(response);
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
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/r/{subreddit}/api/info.json?id={string.Join(",", fullNames)}");
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit Comment/Link Info Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      return GetThingsFromResponse(responseContent);
    }

    public async Task<RedditThing> GetInfoOfUser(string username)
    {
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/user/{username}/about");
      SetRatelimitInfo(response);
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
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/message/unread.json");
      SetRatelimitInfo(response);
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

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/read_message", content);
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit MarkMessagesAsRead Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");
    }

    public async Task<string> PostComment(string parentFullName, string text)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("api_type", "json");
      data.Add("thing_id", parentFullName);
      data.Add("text", text);

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/comment", content);
      SetRatelimitInfo(response);
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

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/submit", content);
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      Log.Verbose($"Reddit Submit Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      dynamic deserializedResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
      if (deserializedResponse == null)
        return null;

      return deserializedResponse.json.data.name;
    }

    public async Task<string> Test()
    {
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/api/v1/me");
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      return responseContent;
    }

    private void SetRatelimitInfo(HttpResponseMessage response)
    {
      try
      {
        List<string> ratelimitRemainingHeaders = response.Headers.GetValues("x-ratelimit-remaining").ToList();
        if (ratelimitRemainingHeaders != null && ratelimitRemainingHeaders.Count > 0)
          ratelimitRemaining = (int)float.Parse(ratelimitRemainingHeaders[0]);

        List<string> ratelimitResetHeaders = response.Headers.GetValues("x-ratelimit-reset").ToList();
        if (ratelimitResetHeaders != null && ratelimitResetHeaders.Count > 0)
          ratelimitResetTime = DateTime.Now.AddSeconds(int.Parse(ratelimitResetHeaders[0]));

        Log.Verbose($"RateLimitRemaining: {ratelimitRemaining}.");
      }
      catch (Exception)
      {
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
      HttpClient client = new HttpClient();

      byte[] authorizationBytes = Encoding.ASCII.GetBytes($"{appId}:{clientSecret}");
      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", Convert.ToBase64String(authorizationBytes));

      HttpResponseMessage response = await client.PostAsync($"https://www.reddit.com/api/v1/access_token?grant_type=password&username={userName}&password={password}", null);

      string responseContent = await response.Content.ReadAsStringAsync();
      if (response.IsSuccessStatusCode)
      {
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
      else
        throw new HttpRequestException($"Error requesting OAuth token: '{responseContent}'.");
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
  }
}
