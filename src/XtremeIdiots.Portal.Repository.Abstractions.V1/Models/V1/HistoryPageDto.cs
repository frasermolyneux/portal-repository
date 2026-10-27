namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

public record HistoryPageDto<T>(IReadOnlyList<T> Items, bool HasMore);
