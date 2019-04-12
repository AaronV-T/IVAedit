using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IVAE.RedditBot
{
  public class ImgurUploadResponse
  {
    [JsonProperty("ad_type")]
    public int? AdType { get; set; }

    [JsonProperty("ad_url")]
    public string AdUrl { get; set; }

    [JsonProperty("account_id")]
    public int? AccountId { get; set; }

    [JsonProperty("account_url")]
    public string AccountUrl { get; set; }

    [JsonProperty("animated")]
    public bool? Animated { get; set; }

    [JsonProperty("bandwidth")]
    public long? Bandwidth { get; set; }

    [JsonProperty("datetime")]
    public long? Datetime { get; set; }

    [JsonProperty("deletehash")]
    public string DeleteHash { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("edited")]
    public int? Edited { get; set; }

    [JsonProperty("favorite")]
    public bool? Favorite { get; set; }

    [JsonProperty("has_sound")]
    public bool? has_sound { get; set; }

    [JsonProperty("height")]
    public int? Height { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("in_gallery")]
    public bool? InGallery { get; set; }

    [JsonProperty("in_most_viral")]
    public bool? InMostViral { get; set; }

    [JsonProperty("is_ad")]
    public bool? IsAd { get; set; }

    [JsonProperty("link")]
    public string Link { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("nsfw")]
    public object NSFW { get; set; }

    [JsonProperty("section")]
    public object Section { get; set; }

    [JsonProperty("size")]
    public long? Size { get; set; }

    [JsonProperty("tags")]
    public List<object> Tags { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("views")]
    public int? Views { get; set; }

    [JsonProperty("vote")]
    public object Vote { get; set; }

    [JsonProperty("width")]
    public int? Width { get; set; }
  }
}
