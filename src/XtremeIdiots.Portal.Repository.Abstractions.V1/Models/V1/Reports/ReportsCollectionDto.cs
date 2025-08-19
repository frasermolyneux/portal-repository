using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;

[Obsolete("Use CollectionModel<ReportDto> from MX.Api.Abstractions instead")]
public record ReportsCollectionDto
{
    public List<ReportDto> Entries { get; set; } = new List<ReportDto>();
    public int TotalRecords { get; set; }
}
