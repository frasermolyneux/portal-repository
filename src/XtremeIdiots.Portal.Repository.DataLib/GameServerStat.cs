﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace XtremeIdiots.Portal.Repository.DataLib;

[Index("GameServerId", Name = "IX_GameServerId")]
[Index("GameServerStatId", Name = "IX_GameServerStatId", IsUnique = true)]
public partial class GameServerStat
{
    [Key]
    public Guid GameServerStatId { get; set; }

    public Guid? GameServerId { get; set; }

    public int PlayerCount { get; set; }

    public string MapName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime Timestamp { get; set; }

    [ForeignKey("GameServerId")]
    [InverseProperty("GameServerStats")]
    public virtual GameServer? GameServer { get; set; }
}