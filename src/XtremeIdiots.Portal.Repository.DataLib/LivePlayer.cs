﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace XtremeIdiots.Portal.Repository.DataLib;

[Index("GameServerId", Name = "IX_GameServer_GameServerId")]
[Index("LivePlayerId", Name = "IX_LivePlayerId", IsUnique = true)]
[Index("PlayerId", Name = "IX_Players_PlayerId")]
public partial class LivePlayer
{
    [Key]
    public Guid LivePlayerId { get; set; }

    public Guid? PlayerId { get; set; }

    public Guid? GameServerId { get; set; }

    [StringLength(60)]
    public string Name { get; set; } = null!;

    public int Score { get; set; }

    public int Ping { get; set; }

    public int Num { get; set; }

    public int Rate { get; set; }

    [StringLength(10)]
    public string? Team { get; set; }

    public TimeSpan Time { get; set; }

    [StringLength(60)]
    public string? IpAddress { get; set; }

    public double? Lat { get; set; }

    public double? Long { get; set; }

    [StringLength(60)]
    public string? CountryCode { get; set; }

    public int GameType { get; set; }

    [ForeignKey("GameServerId")]
    [InverseProperty("LivePlayers")]
    public virtual GameServer? GameServer { get; set; }

    [ForeignKey("PlayerId")]
    [InverseProperty("LivePlayers")]
    public virtual Player? Player { get; set; }
}