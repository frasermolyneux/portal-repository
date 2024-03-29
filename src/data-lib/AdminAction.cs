﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace XtremeIdiots.Portal.DataLib;

[Index("AdminActionId", Name = "IX_AdminActionId", IsUnique = true)]
[Index("PlayerId", Name = "IX_PlayerId")]
[Index("UserProfileId", Name = "IX_UserProfileId")]
public partial class AdminAction
{
    [Key]
    public Guid AdminActionId { get; set; }

    public Guid PlayerId { get; set; }

    public Guid? UserProfileId { get; set; }

    public int? ForumTopicId { get; set; }

    public int Type { get; set; }

    [Required]
    public string Text { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Created { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Expires { get; set; }

    [ForeignKey("PlayerId")]
    [InverseProperty("AdminActions")]
    public virtual Player Player { get; set; }

    [ForeignKey("UserProfileId")]
    [InverseProperty("AdminActions")]
    public virtual UserProfile UserProfile { get; set; }
}