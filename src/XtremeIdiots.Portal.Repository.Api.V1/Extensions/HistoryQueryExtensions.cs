using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Api.V1.Extensions;

public static class HistoryQueryExtensions
{
    public static async Task<HistoryPageDto<TDto>> ToHistoryPageAsync<TEntity, TDto>(
        this IOrderedQueryable<TEntity> query, int skipEntries, int takeEntries,
        Func<TEntity, TDto> map, CancellationToken cancellationToken)
    {
        skipEntries = Math.Clamp(skipEntries, 0, int.MaxValue - 201);
        takeEntries = Math.Clamp(takeEntries, 1, 200);
        var items = await query.Skip(skipEntries).Take(takeEntries + 1)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new HistoryPageDto<TDto>(
            items.Take(takeEntries).Select(map).ToList(),
            items.Count > takeEntries);
    }
}
