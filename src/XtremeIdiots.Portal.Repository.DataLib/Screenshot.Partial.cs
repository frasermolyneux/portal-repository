namespace XtremeIdiots.Portal.Repository.DataLib;

// Ensures in-memory test-created screenshots satisfy required link metadata defaults.
public partial class Screenshot
{
    public Screenshot()
    {
        LinkSource ??= "unlinked";
        LinkConfidence ??= "low";
    }
}