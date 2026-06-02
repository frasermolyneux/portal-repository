namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots
{
    public record GetScreenshotsQuery
    {
        public string? PlayerIdentifier { get; init; }

        public string? PlayerName { get; init; }

        public DateTime? CapturedFromUtc { get; init; }

        public DateTime? CapturedToUtc { get; init; }

        public string? Source { get; init; }

        public bool IncludeDeleted { get; init; }
    }
}
