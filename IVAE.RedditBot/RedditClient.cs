﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IVAE.RedditBot
{
  public class RedditClient
  {
    private const string BASE_URL = "https://oauth.reddit.com";

    private HttpClient httpClient;
    private OAuthTokenInfo oAuthTokenInfo;
    private int ratelimitRemaining = int.MaxValue;
    private DateTime ratelimitResetTime = DateTime.MinValue;

    public RedditClient() {}

    public async Task<RedditThing> GetThingInfo(string subreddit, string fullName)
    {
      List<RedditThing> thingsInfo = await GetThingsInfo(subreddit, new List<string> { fullName });

      if (thingsInfo == null)
        throw new Exception($"GetThingsInfo got null response from Reddit.");
      if (thingsInfo.Count != 1)
        throw new Exception($"GetThingsInfo did not get exactly 1 thing from Reddit. Count: {thingsInfo.Count}.");

      return thingsInfo[0];
    }

    public async Task<List<RedditThing>> GetThingsInfo(string subreddit, List<string> fullNames)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("id", string.Join(",", fullNames));

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/r/{subreddit}/api/info.json?id={string.Join(",", fullNames)}");
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      //Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented));

      return GetThingsFromResponse(responseContent);
    }

    public async Task<List<RedditThing>> GetUnreadMessages()
    {
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.GetAsync($"{BASE_URL}/message/unread.json");
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      //Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented));

      return GetThingsFromResponse(responseContent);
    }

    public async Task MarkMessagesAsRead(List<string> messageNames)
    {
      Dictionary<string, string> data = new Dictionary<string, string>();
      data.Add("id", string.Join(",", messageNames));

      HttpContent content = new FormUrlEncodedContent(data);
      await WaitUntilARequestCanBeMade();
      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/api/read_message", content);
      SetRatelimitInfo(response);
      string responseContent = await response.Content.ReadAsStringAsync();

      //Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented));
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
      }
      catch (Exception)
      {
        throw new Exception($"Could not get rate limit headers from Reddit response. Response headers: {JsonConvert.SerializeObject(response.Headers, Formatting.Indented)}.");
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

        Console.WriteLine($"Reddit rate limit hit. Waiting until {ratelimitResetTime.ToShortTimeString()}");
        while (DateTime.Now < ratelimitResetTime) { }
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
      string[] authLines = System.IO.File.ReadAllLines("oauthsec.txt");
      return await GetOAuthToken(
        authLines[0],
        Encoding.Unicode.GetString(Convert.FromBase64String(authLines[1])),
        authLines[2],
        Encoding.Unicode.GetString(Convert.FromBase64String(authLines[3])));
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
        Dictionary<string, string> deserializedResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
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
      RedditListing listing = ((JObject)deserializedResponse["data"]).ToObject<RedditListing>();
      foreach (JObject jObj in (JArray)listing.Children)
      {
        Dictionary<string, object> objectDict = jObj.ToObject<Dictionary<string, object>>();
        if (!objectDict.ContainsKey("data") || objectDict["data"] == null)
          continue;

        things.Add(((JObject)objectDict["data"]).ToObject<RedditThing>());
      }

      return things;
    }
  }
}