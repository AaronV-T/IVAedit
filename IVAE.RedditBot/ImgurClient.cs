using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IVAE.RedditBot.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IVAE.RedditBot
{
  public class ImgurClient
  {
    private const string BASE_URL = "https://api.imgur.com";

    private string clientID, encodedClientSecret;
    private HttpClient httpClient;

    public ImgurClient(string clientID, string encodedClientSecret)
    {
      this.clientID = clientID ?? throw new ArgumentNullException(nameof(clientID));
      this.encodedClientSecret = encodedClientSecret ?? throw new ArgumentNullException(nameof(encodedClientSecret));

      this.httpClient = new HttpClient();
      this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Client-ID {clientID}");
    }

    public async Task<bool?> Delete(string deleteHash)
    {
      HttpResponseMessage response = await httpClient.DeleteAsync($"{BASE_URL}/3/image/{deleteHash}");
      string responseContent = await response.Content.ReadAsStringAsync();

      //Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented));

      Dictionary<string, object> deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
      if (!deserializedResponse.ContainsKey("success"))
        return null;

      return (bool)deserializedResponse["success"];
    }

    public async Task<ImgurUploadResponse> Upload(byte[] fileBytes, string videoFormatIfNotImage)
    {
      MultipartFormDataContent requestContent = new MultipartFormDataContent("----WebKitFormBoundary7MA4YWxkTrZu0gW");

      if (videoFormatIfNotImage == null)
      {
        requestContent.Add(new ByteArrayContent(fileBytes), "image");
      }
      else
      {
        ByteArrayContent bac = new ByteArrayContent(fileBytes);
        bac.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data");
        bac.Headers.ContentDisposition.Name = "video";
        bac.Headers.ContentDisposition.FileName = $"AutoUpload.{videoFormatIfNotImage}";
        bac.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue($"video/{videoFormatIfNotImage}");
        requestContent.Add(bac);
      }

      foreach (var kvp in requestContent.Headers)
        System.Diagnostics.Debug.WriteLine($"{kvp.Key}: {string.Join("; ", kvp.Value)}");
      System.Diagnostics.Debug.WriteLine(await requestContent.ReadAsStringAsync());

      HttpResponseMessage response = await httpClient.PostAsync($"{BASE_URL}/3/upload", requestContent);
      string responseContent = await response.Content.ReadAsStringAsync();

      System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented));

      Dictionary<string, object> deserializedResponse = JsonConvert.DeserializeObject<Dictionary<string,object>>(responseContent);
      if (!deserializedResponse.ContainsKey("success") || !(bool)deserializedResponse["success"])
        return null;

      if (!deserializedResponse.ContainsKey("data"))
        return null;

      return ((JObject)deserializedResponse["data"]).ToObject<ImgurUploadResponse>();
    }
  }
}
