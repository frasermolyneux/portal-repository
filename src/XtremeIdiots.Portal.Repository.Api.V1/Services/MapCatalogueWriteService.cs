using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class MapCatalogueWriteService
{
    public static async Task LockAsync(PortalDbContext context, CancellationToken cancellationToken)
    {
        if (context.Database.IsSqlServer())
        {
            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = N'Portal.MapCatalogue',
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Transaction',
                    @LockTimeout = 30000;
                IF @result < 0 THROW 51000, 'Unable to lock the map catalogue.', 1;
                """, cancellationToken).ConfigureAwait(false);
        }
    }
}
