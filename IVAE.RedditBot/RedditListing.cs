using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IVAE.RedditBot
{
  public class RedditListing
  {
    [JsonProperty("modhash")]
    public string Modhash { get; set; }

    [JsonProperty("dist")]
    public int Dist { get; set; }

    [JsonProperty("children")]
    public object Children { get; set; }

    [JsonProperty("after")]
    public string After { get; set; }

    [JsonProperty("before")]
    public string Before { get; set; }
  }
}
