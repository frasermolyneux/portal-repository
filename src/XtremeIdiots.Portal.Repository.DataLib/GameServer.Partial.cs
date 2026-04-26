// Partial class that sets sensible C# defaults on properties that have SQL DEFAULT
// constraints. EF Core's in-memory provider does not honour SQL DEFAULTs, so non-nullable
// reference-type properties initialised by the auto-generated entity to `null!` would
// otherwise fail required-property validation in unit tests that construct GameServers
// directly. This file is hand-written and is preserved across `efcpt` regenerations.
#nullable enable

namespace XtremeIdiots.Portal.Repository.DataLib;

public partial class GameServer
{
    public GameServer()
    {
        // Mirrors the SQL DEFAULT '/' on dbo.GameServers.BanFileRootPath.
        BanFileRootPath = "/";
    }
}
