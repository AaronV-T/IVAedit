using System;
using System.Collections.Generic;
using System.Text;

namespace IVAE.RedditBot
{
  public class OAuthTokenInfo
  {
    public string AccessToken { get; set; }
    public string TokenType { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string Scope { get; set; }
  }
}
