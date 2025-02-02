﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace XtremeIdiots.Portal.DataLib;

[Index("GameServerId", Name = "IX_GameServerId")]
public partial class MapPack
{
    [Key]
    public Guid MapPackId { get; set; }

    public Guid? GameServerId { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public string GameMode { get; set; }

    public bool SyncToGameServer { get; set; }

    public bool SyncCompleted { get; set; }

    public bool Deleted { get; set; }

    [ForeignKey("GameServerId")]
    [InverseProperty("MapPacks")]
    public virtual GameServer GameServer { get; set; }

    [InverseProperty("MapPack")]
    public virtual ICollection<MapPackMap> MapPackMaps { get; set; } = new List<MapPackMap>();
}