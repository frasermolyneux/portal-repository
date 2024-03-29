﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace XtremeIdiots.Portal.DataLib;

[Index("BanFileMonitorId", Name = "IX_BanFileMonitorId", IsUnique = true)]
[Index("GameServerId", Name = "IX_GameServerId")]
public partial class BanFileMonitor
{
    [Key]
    public Guid BanFileMonitorId { get; set; }

    public Guid GameServerId { get; set; }

    [Required]
    public string FilePath { get; set; }

    public long? RemoteFileSize { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastSync { get; set; }

    [ForeignKey("GameServerId")]
    [InverseProperty("BanFileMonitors")]
    public virtual GameServer GameServer { get; set; }
}