namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

public enum DeploymentState
{
    Pending = 0,
    Syncing = 1,
    Synced = 2,
    Removing = 3,
    Removed = 4,
    Failed = 5,
    PartiallyDeployed = 6
}
