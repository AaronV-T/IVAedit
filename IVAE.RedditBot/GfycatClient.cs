using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace IVAE.RedditBot
{
  public class GfycatClient
  {
    private string clientID, encodedClientSecret, username, encodedPassword;

    private HttpClient httpClient;
    private OAuthTokenInfo oAuthTokenInfo;

    public GfycatClient(string clientID, string encodedClientSecret, string username, string encodedPassword) : this()
    {
      this.clientID = clientID ?? throw new ArgumentNullException(nameof(clientID));
      this.encodedClientSecret = encodedClientSecret ?? throw new ArgumentNullException(nameof(encodedClientSecret));
      this.username = username ?? throw new ArgumentNullException(nameof(username));
      this.encodedPassword = encodedPassword ?? throw new ArgumentNullException(nameof(encodedPassword));
    }

    public GfycatClient()
    {
      this.httpClient = new HttpClient();
    }

    public async Task<bool> Delete(string gfyname)
    {
      await Authorize();

      HttpResponseMessage response = await httpClient.DeleteAsync($"https://api.gfycat.com/v1/me/gfycats/{gfyname}");
      string responseContent = await response.Content.ReadAsStringAsync();
      Log.Verbose($"Gfycat Delete:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      return response.IsSuccessStatusCode;
    }

    public async Task<string> Upload(byte[] fileBytes)
    {
      await Authorize();

      // Prepare to upload file.
      HttpContent requestContent = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, object>
      {
        { "keepAudio", "true" },
        { "noMd5", "true" },
        { "nsfw", 3 },
        { "title", "Automatic Upload" }
      }));
      requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

      HttpResponseMessage response = await httpClient.PostAsync("https://api.gfycat.com/v1/gfycats", requestContent);
      string responseContent = await response.Content.ReadAsStringAsync();
      Log.Verbose($"Gfycat Upload1 Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");

      //Dictionary<string, string> deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
      DTO.GfycatUploadResponse gfycatUploadResponse = JsonConvert.DeserializeObject<DTO.GfycatUploadResponse>(responseContent);
      if (!response.IsSuccessStatusCode || gfycatUploadResponse == null || string.IsNullOrEmpty(gfycatUploadResponse.Gfyname))
        throw new Exception($"First part of Gfycat upload failed. Status code: {response.StatusCode}.");

      // Upload file.
      requestContent = new ByteArrayContent(fileBytes);

      response = await new HttpClient().PutAsync($"https://filedrop.gfycat.com/{gfycatUploadResponse.Gfyname}", requestContent);
      responseContent = await response.Content.ReadAsStringAsync();
      Log.Verbose($"Gfycat Upload2 Response:{Environment.NewLine}{responseContent}");

      return response.IsSuccessStatusCode ? gfycatUploadResponse.Gfyname : null;
    }

    private async Task Authorize()
    {
      if (!string.IsNullOrEmpty(clientID) && !string.IsNullOrEmpty(encodedClientSecret) && (oAuthTokenInfo == null || oAuthTokenInfo.ExpirationDate <= DateTime.Now))
      {
        oAuthTokenInfo = await GetOAuthToken(clientID, Encoding.Unicode.GetString(Convert.FromBase64String(this.encodedClientSecret)), username, Encoding.Unicode.GetString(Convert.FromBase64String(this.encodedPassword)));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(oAuthTokenInfo.TokenType, oAuthTokenInfo.AccessToken);
      }
    }

    private async Task<OAuthTokenInfo> GetOAuthToken(string clientId, string clientSecret, string username, string password)
    {
      Dictionary<string, string> contentDict = new Dictionary<string, string>
      {
        { "client_id", clientID },
        { "client_secret", clientSecret }
      };

      if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
      {
        contentDict.Add("grant_type", "client_credentials");
      }
      else
      {
        contentDict.Add("grant_type", "password");
        contentDict.Add("username", username);
        contentDict.Add("password", password);
      }

      HttpContent requestContent = new StringContent(JsonConvert.SerializeObject(contentDict));
      requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

      HttpResponseMessage response = await httpClient.PostAsync("https://api.gfycat.com/v1/oauth/token", requestContent);

      string responseContent = await response.Content.ReadAsStringAsync();
      Log.Verbose($"Gfycat OAuth Response:{Environment.NewLine}{JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented)}");
      if (response.IsSuccessStatusCode)
      {
        Dictionary<string, string> deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
        
        if (deserializedResponse.ContainsKey("error"))
        {
          throw new Exception($"Error getting OAuthToken from Gfycat: '{deserializedResponse["error"]}'.");
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
        throw new HttpRequestException($"Error requesting OAuth token: '{response.StatusCode}'.");
    }
  }
}
