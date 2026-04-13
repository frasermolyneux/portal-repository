using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

[Obsolete("Use CollectionModel<PermissionReportEntryDto> from MX.Api.Abstractions instead")]
public record PermissionReportCollectionDto
{
    public List<PermissionReportEntryDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
