using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IVAE.RedditBot.DTO
{
  public class RedditThing
  {
    [JsonProperty("approved_at_utc")]
    public long? ApprovedAtUtc { get; set; }

    [JsonProperty("approved_by")]
    public string ApprovedBy { get; set; }

    [JsonProperty("archived")]
    public bool? Archived { get; set; }

    [JsonProperty("author")]
    public string Author { get; set; }

    [JsonProperty("author_flair_background_color")]
    public object AuthorFlairBackgroundColor { get; set; }

    [JsonProperty("author_flair_css_class")]
    public object AuthorFlairCssClass { get; set; }

    [JsonProperty("author_flair_richtext")]
    public object AuthorFlairRichtext { get; set; }

    [JsonProperty("author_flair_template_id")]
    public object AuthorFlairTemplateId { get; set; }

    [JsonProperty("author_flair_text")]
    public object AuthorFlairText { get; set; }

    [JsonProperty("author_flair_text_color")]
    public object AuthorFlairTextColor { get; set; }

    [JsonProperty("author_flair_type")]
    public string AuthorFlairType { get; set; }

    [JsonProperty("author_fullname")]
    public string AuthorFullname { get; set; }

    [JsonProperty("author_patreon_flair")]
    public bool? AuthorPatreonFlair { get; set; }

    [JsonProperty("banned_at_utc")]
    public long? BannedAtUtc { get; set; }

    [JsonProperty("banned_by")]
    public object BannedBy { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; }

    [JsonProperty("body_html")]
    public string BodyHtml { get; set; }

    [JsonProperty("can_gild")]
    public bool? CanGild { get; set; }

    [JsonProperty("can_mod_post")]
    public bool? CanModPost { get; set; }

    [JsonProperty("category")]
    public object Category { get; set; }

    [JsonProperty("clicked")]
    public bool? Clicked { get; set; }

    [JsonProperty("collapsed")]
    public bool? Collapsed { get; set; }

    [JsonProperty("collapsed_reason")]
    public object CollapsedReason { get; set; }

    [JsonProperty("comment_karma")]
    public int? CommentKarma { get; set; }

    [JsonProperty("content_categories")]
    public object ContentCategories { get; set; }

    [JsonProperty("contest_mode")]
    public bool? ContestMode { get; set; }

    [JsonProperty("context")]
    public string Context { get; set; }

    [JsonProperty("controversiality")]
    public int? Controversiality { get; set; }

    [JsonProperty("created")]
    public long? Created { get; set; }

    [JsonProperty("crosspost_parent_list")]
    public List<RedditThing> CrosspostParentList { get; set; }

    [JsonProperty("created_utc")]
    public long? CreatedUtc { get; set; }

    [JsonProperty("dest")]
    public string Dest { get; set; }

    [JsonProperty("distinguished")]
    public string Distinguished { get; set; }

    [JsonProperty("domain")]
    public string Domain { get; set; }

    [JsonProperty("downs")]
    public int? Downs { get; set; }

    [JsonProperty("edited")]
    public bool? Edited { get; set; }

    [JsonProperty("first_message")]
    public string FirstMessage { get; set; }

    [JsonProperty("first_message_name")]
    public string FirstMessageName { get; set; }

    [JsonProperty("gilded")]
    public int? Gilded { get; set; }

    [JsonProperty("gildings")]
    public Dictionary<string, int> Gildings { get; set; }

    [JsonProperty("hidden")]
    public bool? Hidden { get; set; }

    [JsonProperty("hide_score")]
    public bool? HideScore { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("is_crosspostable")]
    public bool? IsCrosspostable { get; set; }

    [JsonProperty("is_employee")]
    public bool? IsEmployee { get; set; }

    [JsonProperty("is_meta")]
    public bool? IsMeta { get; set; }

    [JsonProperty("is_original_content")]
    public bool? IsOriginalContent { get; set; }

    [JsonProperty("is_reddit_media_domain")]
    public bool? IsRedditMediaDomain { get; set; }

    [JsonProperty("is_robot_indexable")]
    public bool? IsRobotIndexable { get; set; }

    [JsonProperty("is_self")]
    public bool? IsSelf { get; set; }

    [JsonProperty("is_submitter")]
    public bool? IsSubmitter { get; set; }

    [JsonProperty("is_video")]
    public bool? IsVideo { get; set; }

    public string Kind { get { return Name?.Substring(0, 2); } }

    [JsonProperty("likes")]
    public string Likes { get; set; }

    [JsonProperty("link_flair_background_color")]
    public string LinkFlairBackgroundColor { get; set; }

    [JsonProperty("link_flair_css_class")]
    public object LinkFlairCssClass { get; set; }

    [JsonProperty("link_flair_richtext")]
    public object LinkFlairRichtext { get; set; }

    [JsonProperty("link_flair_template_id")]
    public object LinkFlairTemplateId { get; set; }

    [JsonProperty("link_flair_text")]
    public object LinkFlairText { get; set; }

    [JsonProperty("link_flair_text_color")]
    public string LinkFlairTextColor { get; set; }

    [JsonProperty("link_flair_type")]
    public object LinkFlairType { get; set; }

    [JsonProperty("link_id")]
    public string LinkId { get; set; }

    [JsonProperty("link_karma")]
    public int? LinkKarma { get; set; }

    [JsonProperty("locked")]
    public bool? Locked { get; set; }

    [JsonProperty("media")]
    public object Media { get; set; }

    [JsonProperty("media_embed")]
    public object MediaEmbed { get; set; }

    [JsonProperty("media_only")]
    public bool? MediaOnly { get; set; }

    [JsonProperty("mod_note")]
    public object ModNote { get; set; }

    [JsonProperty("mod_reason_by")]
    public object ModReasonBy { get; set; }

    [JsonProperty("mod_reason_title")]
    public object ModReasonTitle { get; set; }

    [JsonProperty("mod_reports")]
    public object ModReports { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("new")]
    public bool? New { get; set; }

    [JsonProperty("no_follow")]
    public bool? NoFollow { get; set; }

    [JsonProperty("num_comments")]
    public int? NumComments { get; set; }

    [JsonProperty("num_crossposts")]
    public int? NumCrossposts { get; set; }

    [JsonProperty("num_reports")]
    public int? NumReports { get; set; }

    [JsonProperty("over_18")]
    public bool? Over18 { get; set; }

    [JsonProperty("parent_id")]
    public string ParentId { get; set; }

    [JsonProperty("parent_whitelist_status")]
    public string ParentWhitelistStatus { get; set; }

    [JsonProperty("permalink")]
    public string Permalink { get; set; }

    [JsonProperty("pinned")]
    public bool? Pinned { get; set; }

    [JsonProperty("post_hint")]
    public string PostHint { get; set; }

    [JsonProperty("preview")]
    public Dictionary<string, object> Preview { get; set; }

    [JsonProperty("pwls")]
    public int? Pwls { get; set; }

    [JsonProperty("quarantine")]
    public bool? Quarantine { get; set; }

    [JsonProperty("removal_reason")]
    public string RemovalReason { get; set; }

    [JsonProperty("replies")]
    public string Replies { get; set; }

    [JsonProperty("report_reasons")]
    public object ReportReasons { get; set; }

    [JsonProperty("saved")]
    public bool? Saved { get; set; }

    [JsonProperty("score")]
    public int? Score { get; set; }

    [JsonProperty("score_hidden")]
    public bool? ScoreHidden { get; set; }

    [JsonProperty("secure_media")]
    public object SecureMedia { get; set; }

    [JsonProperty("secure_media_embed")]
    public object SecureMediaEmbed { get; set; }

    [JsonProperty("selftext")]
    public string Selftext { get; set; }

    [JsonProperty("selftext_html")]
    public string SelftextHtml { get; set; }

    [JsonProperty("send_replies")]
    public bool? SendReplies { get; set; }

    [JsonProperty("spoiler")]
    public bool? Spoiler { get; set; }

    [JsonProperty("stickied")]
    public bool? Stickied { get; set; }

    [JsonProperty("subject")]
    public string Subject { get; set; }

    [JsonProperty("subreddit")]
    public string Subreddit { get; set; }

    [JsonProperty("subreddit_id")]
    public string SubredditId { get; set; }

    [JsonProperty("subreddit_name_prefixed")]
    public string SubredditNamePrefixed { get; set; }

    [JsonProperty("subreddit_subscribers")]
    public int? SubredditSubscribers { get; set; }

    [JsonProperty("subreddit_type")]
    public string SubredditType { get; set; }

    [JsonProperty("suggested_sort")]
    public object SuggestedSort { get; set; }

    [JsonProperty("thumbnail")]
    public string Thumbnail { get; set; }

    [JsonProperty("thumbnail_height")]
    public int? ThumbnailHeight { get; set; }

    [JsonProperty("thumbnail_width")]
    public int? ThumbnailWidth { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("ups")]
    public int? Ups { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("user_reports")]
    public object UserReports { get; set; }

    [JsonProperty("view_count")]
    public int? ViewCount { get; set; }

    [JsonProperty("visited")]
    public bool? Visited { get; set; }

    [JsonProperty("was_comment")]
    public bool? WasComment { get; set; }

    [JsonProperty("whitelist_status")]
    public string WhitelistStatus { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
