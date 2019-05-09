using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot.DTO
{
  public class GfycatUploadResponse
  {
    [JsonProperty("isOk")]
    public bool IsOk { get; set; }

    [JsonProperty("gfyname")]
    public string Gfyname { get; set; }

    [JsonProperty("secret")]
    public string Secret { get; set; }

    [JsonProperty("uploadType")]
    public string UploadType { get; set; }
  }
}
