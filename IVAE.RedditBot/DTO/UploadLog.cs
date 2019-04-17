﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot.DTO
{
  public class UploadLog
  {
    public string DeleteKey { get; set; }
    public int Id { get; set; }
    public string PostFullname { get; set; }
    public string ReplyFullname { get; set; }
    public string RequestorUsername { get; set; }
    public DateTime UploadDatetime { get; set; }
    public string UploadDestination { get; set; }
  }
}
