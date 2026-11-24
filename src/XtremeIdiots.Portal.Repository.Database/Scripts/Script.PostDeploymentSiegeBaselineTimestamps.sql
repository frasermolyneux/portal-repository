UPDATE baseline
SET FirstSeenAtUtc = COALESCE(history.FirstSeenAtUtc, session.StartedAtUtc)
FROM dbo.SiegeBaselineGuids AS baseline
INNER JOIN dbo.SiegeSessions AS session ON session.SessionId = baseline.SessionId
OUTER APPLY
(
    SELECT MIN(player.FirstSeen) AS FirstSeenAtUtc
    FROM dbo.SiegeBaselineGuidOwners AS owner
    INNER JOIN dbo.Players AS player ON player.PlayerId = owner.PlayerId
    WHERE owner.SessionId = baseline.SessionId
      AND owner.PlayerGuid = baseline.PlayerGuid
      AND player.FirstSeen <= session.StartedAtUtc
) AS history
WHERE baseline.FirstSeenAtUtc IS NULL;

UPDATE baseline
SET FirstSeenAtUtc = COALESCE(history.FirstSeenAtUtc, session.StartedAtUtc)
FROM dbo.SiegeBaselineIps AS baseline
INNER JOIN dbo.SiegeSessions AS session ON session.SessionId = baseline.SessionId
OUTER APPLY
(
    SELECT MIN(address.Added) AS FirstSeenAtUtc
    FROM dbo.PlayerIpAddresses AS address
    WHERE address.Address = baseline.Address
      AND address.Added <= session.StartedAtUtc
) AS history
WHERE baseline.FirstSeenAtUtc IS NULL;
