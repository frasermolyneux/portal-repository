#!/usr/bin/env python3
"""Temporary, manually dispatched VPN settings verification and cutover operation."""

from __future__ import annotations

import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any


STATUS_SQL = r"""
SET NOCOUNT ON;
WITH ExpectedNamespaces AS (
    SELECT CAST(N'vpnProtection' AS nvarchar(128)) AS [Namespace]
    UNION ALL
    SELECT CAST(N'cod4xPlugin' AS nvarchar(128))
),
ConfigurationRows AS (
    SELECT
        CAST(N'global' AS nvarchar(8)) AS [Scope],
        CAST(NULL AS varchar(36)) AS GameServerId,
        CAST(NULL AS int) AS GameType,
        CAST(NULL AS bit) AS ServerListEnabled,
        expected.[Namespace],
        config.Configuration,
        config.LastModifiedUtc
    FROM ExpectedNamespaces AS expected
    LEFT JOIN dbo.GlobalConfigurations AS config
        ON config.[Namespace] = expected.[Namespace]

    UNION ALL

    SELECT
        CAST(N'server' AS nvarchar(8)),
        CONVERT(varchar(36), server.GameServerId),
        server.GameType,
        server.ServerListEnabled,
        expected.[Namespace],
        config.Configuration,
        config.LastModifiedUtc
    FROM dbo.GameServers AS server
    CROSS JOIN ExpectedNamespaces AS expected
    LEFT JOIN dbo.GameServerConfigurations AS config
        ON config.GameServerId = server.GameServerId
        AND config.[Namespace] = expected.[Namespace]
    WHERE server.Deleted = 0
),
Parsed AS (
    SELECT
        configRows.*,
        CASE WHEN ISJSON(configRows.Configuration) = 1 THEN 1 ELSE 0 END AS IsValidJson,
        fields.EnabledValue,
        fields.VpnProtectionEnabledValue,
        fields.SchemaVersionValue,
        fields.RulesJson,
        fields.RuleOverridesJson,
        fields.ExcludedPlayerTagsJson
    FROM ConfigurationRows AS configRows
    OUTER APPLY (
        SELECT
            MAX(CASE WHEN LOWER([key]) = N'enabled' THEN [value] END) AS EnabledValue,
            MAX(CASE WHEN LOWER([key]) = N'vpnprotectionenabled' THEN [value] END) AS VpnProtectionEnabledValue,
            MAX(CASE WHEN LOWER([key]) = N'schemaversion' THEN [value] END) AS SchemaVersionValue,
            MAX(CASE WHEN LOWER([key]) = N'rules' THEN [value] END) AS RulesJson,
            MAX(CASE WHEN LOWER([key]) = N'ruleoverrides' THEN [value] END) AS RuleOverridesJson,
            MAX(CASE WHEN LOWER([key]) = N'excludedplayertags' THEN [value] END) AS ExcludedPlayerTagsJson
        FROM OPENJSON(
            CASE
                WHEN ISJSON(configRows.Configuration) = 1 THEN configRows.Configuration
                ELSE N'{}'
            END
        )
    ) AS fields
),
StatusRows AS (
    SELECT
        Parsed.*,
        CASE
            WHEN Configuration IS NULL THEN N'missing-document'
            WHEN IsValidJson = 0 THEN N'invalid-json'
            WHEN SchemaVersionValue IS NULL THEN N'missing'
            WHEN TRY_CONVERT(int, SchemaVersionValue) IS NULL THEN N'invalid'
            ELSE CONVERT(nvarchar(12), TRY_CONVERT(int, SchemaVersionValue))
        END AS SchemaVersionStatus,
        CASE
            WHEN Configuration IS NULL THEN N'missing-document'
            WHEN IsValidJson = 0 THEN N'invalid-json'
            WHEN EnabledValue IS NULL THEN N'null-or-missing'
            WHEN LOWER(LTRIM(RTRIM(EnabledValue))) = N'true' THEN N'true'
            WHEN LOWER(LTRIM(RTRIM(EnabledValue))) = N'false' THEN N'false'
            ELSE N'invalid-value'
        END AS EnabledStatus,
        CASE
            WHEN [Namespace] <> N'cod4xPlugin' THEN N'not-applicable'
            WHEN Configuration IS NULL THEN N'missing-document'
            WHEN IsValidJson = 0 THEN N'invalid-json'
            WHEN VpnProtectionEnabledValue IS NULL THEN N'null-or-missing'
            WHEN LOWER(LTRIM(RTRIM(VpnProtectionEnabledValue))) = N'true' THEN N'true'
            WHEN LOWER(LTRIM(RTRIM(VpnProtectionEnabledValue))) = N'false' THEN N'false'
            ELSE N'invalid-value'
        END AS PluginVpnEnabledStatus
    FROM Parsed
)
SELECT CONCAT(
    [Scope], N'|',
    COALESCE(GameServerId, ''), N'|',
    COALESCE(CONVERT(varchar(12), GameType), ''), N'|',
    COALESCE(CASE WHEN ServerListEnabled = 1 THEN 'true' WHEN ServerListEnabled = 0 THEN 'false' END, ''), N'|',
    [Namespace], N'|',
    CASE WHEN Configuration IS NULL THEN N'missing-document' WHEN IsValidJson = 0 THEN N'invalid-json' ELSE N'valid-json' END, N'|',
    SchemaVersionStatus, N'|',
    EnabledStatus, N'|',
    PluginVpnEnabledStatus, N'|',
    CASE WHEN [Namespace] = N'vpnProtection'
        THEN COALESCE((SELECT COUNT(*) FROM OPENJSON(CASE WHEN ISJSON(RulesJson) = 1 THEN RulesJson ELSE N'[]' END)), 0)
        ELSE 0
    END, N'|',
    CASE WHEN [Namespace] = N'vpnProtection'
        THEN COALESCE((SELECT COUNT(*) FROM OPENJSON(CASE WHEN ISJSON(RuleOverridesJson) = 1 THEN RuleOverridesJson ELSE N'[]' END)), 0)
        ELSE 0
    END, N'|',
    CASE WHEN [Namespace] = N'vpnProtection'
        THEN COALESCE((SELECT COUNT(*) FROM OPENJSON(CASE WHEN ISJSON(ExcludedPlayerTagsJson) = 1 THEN ExcludedPlayerTagsJson ELSE N'[]' END)), 0)
        ELSE 0
    END, N'|',
    COALESCE(CONVERT(varchar(33), LastModifiedUtc, 126) + 'Z', '')
) AS StatusLine
FROM StatusRows
ORDER BY [Scope], GameServerId, [Namespace];
"""

VPN_SNAPSHOT_ROWS = r"""
SELECT
    N'global' AS [Scope],
    CAST(NULL AS uniqueidentifier) AS GameServerId,
    [Namespace],
    Configuration,
    LastModifiedUtc
FROM dbo.GlobalConfigurations
WHERE [Namespace] = N'vpnProtection'

UNION ALL

SELECT
    N'server',
    GameServerId,
    [Namespace],
    Configuration,
    LastModifiedUtc
FROM dbo.GameServerConfigurations
WHERE [Namespace] = N'vpnProtection'
"""

BACKUP_SQL = f"""
SET NOCOUNT ON;
DECLARE @payload nvarchar(max) = (
    SELECT [Scope], GameServerId, [Namespace], Configuration, LastModifiedUtc
    FROM ({VPN_SNAPSHOT_ROWS}) AS vpnRows
    ORDER BY [Scope], GameServerId
    FOR JSON PATH, INCLUDE_NULL_VALUES
);
SET @payload = COALESCE(@payload, N'[]');
DECLARE @payloadHex varchar(max) =
    CONVERT(varchar(max), CONVERT(varbinary(max), @payload), 2);

SELECT CONCAT(N'HASH|', CONVERT(varchar(64),
    HASHBYTES('SHA2_256', CONVERT(varbinary(max), @payload)), 2));

;WITH Chunks AS (
    SELECT 1 AS ChunkIndex, SUBSTRING(@payloadHex, 1, 30000) AS ChunkText
    UNION ALL
    SELECT ChunkIndex + 1, SUBSTRING(@payloadHex, ChunkIndex * 30000 + 1, 30000)
    FROM Chunks
    WHERE ChunkIndex * 30000 < LEN(@payloadHex)
)
SELECT CONCAT(N'DATA|', ChunkText)
FROM Chunks
ORDER BY ChunkIndex
OPTION (MAXRECURSION 0);
"""

CUTOVER_SQL = f"""
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

DECLARE @expectedHash varchar(64) = N'__EXPECTED_HASH__';
DECLARE @expectedHashBytes varbinary(32) = CONVERT(varbinary(32), @expectedHash, 2);
DECLARE @newConfiguration nvarchar(max) =
    N'{{"schemaVersion":2,"enabled":false,"rules":[],"ruleOverrides":[],"excludedPlayerTags":[]}}';

IF ISJSON(@newConfiguration) <> 1
    THROW 51000, 'The replacement VPN settings document is not valid JSON.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @currentPayload nvarchar(max) = (
        SELECT [Scope], GameServerId, [Namespace], Configuration, LastModifiedUtc
        FROM (
            SELECT
                N'global' AS [Scope],
                CAST(NULL AS uniqueidentifier) AS GameServerId,
                [Namespace],
                Configuration,
                LastModifiedUtc
            FROM dbo.GlobalConfigurations WITH (UPDLOCK, HOLDLOCK)
            WHERE [Namespace] = N'vpnProtection'

            UNION ALL

            SELECT
                N'server',
                GameServerId,
                [Namespace],
                Configuration,
                LastModifiedUtc
            FROM dbo.GameServerConfigurations WITH (UPDLOCK, HOLDLOCK)
            WHERE [Namespace] = N'vpnProtection'
        ) AS vpnRows
        ORDER BY [Scope], GameServerId
        FOR JSON PATH, INCLUDE_NULL_VALUES
    );
    SET @currentPayload = COALESCE(@currentPayload, N'[]');

    DECLARE @currentHashBytes varbinary(32) =
        HASHBYTES('SHA2_256', CONVERT(varbinary(max), @currentPayload));
    IF @currentHashBytes <> @expectedHashBytes
        THROW 51001, 'VPN settings changed after the secure backup; no cutover was applied.', 1;

    UPDATE dbo.GlobalConfigurations
    SET Configuration = @newConfiguration,
        LastModifiedUtc = SYSUTCDATETIME()
    WHERE [Namespace] = N'vpnProtection';

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.GlobalConfigurations ([Namespace], Configuration, LastModifiedUtc)
        VALUES (N'vpnProtection', @newConfiguration, SYSUTCDATETIME());
    END;

    DELETE FROM dbo.GameServerConfigurations
    WHERE [Namespace] = N'vpnProtection';
    DECLARE @deletedOverrideCount int = @@ROWCOUNT;

    COMMIT TRANSACTION;
    SELECT CONCAT(N'CUTOVER|', @deletedOverrideCount) AS CutoverResult;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
"""


def _environment(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise RuntimeError(f"Required environment variable {name} is missing.")
    return value


def _run_sql(query: str) -> list[str]:
    server = _environment("SQL_SERVER_FQDN")
    database = _environment("SQL_DATABASE_NAME")
    result = subprocess.run(
        [
            "sqlcmd",
            "-S",
            f"{server},1433",
            "-d",
            database,
            "--authentication-method=ActiveDirectoryDefault",
            "-b",
            "-h",
            "-1",
            "-W",
            "-w",
            "65535",
            "-y",
            "0",
            "-Q",
            query,
        ],
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"sqlcmd failed with exit code {result.returncode}; query output was suppressed."
        )
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def _parse_status() -> list[dict[str, str]]:
    rows = []
    for line in _run_sql(STATUS_SQL):
        fields = line.split("|")
        if len(fields) != 13:
            raise RuntimeError("Unexpected sanitized status row; refusing to infer settings.")
        (
            scope,
            server_id,
            game_type,
            server_list_enabled,
            namespace,
            document_status,
            schema_version,
            enabled,
            plugin_vpn_enabled,
            rule_count,
            rule_override_count,
            excluded_tag_count,
            modified_utc,
        ) = fields
        rows.append(
            {
                "scope": scope,
                "server_id": server_id,
                "game_type": game_type,
                "server_list_enabled": server_list_enabled,
                "namespace": namespace,
                "document_status": document_status,
                "schema_version": schema_version,
                "enabled": enabled,
                "plugin_vpn_enabled": plugin_vpn_enabled,
                "rule_count": rule_count,
                "rule_override_count": rule_override_count,
                "excluded_tag_count": excluded_tag_count,
                "modified_utc": modified_utc,
            }
        )
    global_rows = [row for row in rows if row["scope"] == "global"]
    global_namespaces = {row["namespace"] for row in global_rows}
    if len(global_rows) != 2 or global_namespaces != {"vpnProtection", "cod4xPlugin"}:
        raise RuntimeError("The sanitized query did not return both global settings.")
    server_namespaces: dict[str, set[str]] = {}
    server_namespace_counts: dict[tuple[str, str], int] = {}
    for row in rows:
        if row["scope"] == "server":
            server_namespaces.setdefault(row["server_id"], set()).add(row["namespace"])
            key = (row["server_id"], row["namespace"])
            server_namespace_counts[key] = server_namespace_counts.get(key, 0) + 1
    if any(namespaces != {"vpnProtection", "cod4xPlugin"} for namespaces in server_namespaces.values()):
        raise RuntimeError("The sanitized query returned an incomplete server settings set.")
    if any(count != 1 for count in server_namespace_counts.values()):
        raise RuntimeError("The sanitized query returned duplicate server settings.")
    return rows


def _resolve_flag(value: str, inherited: bool | None = None) -> bool | None:
    if value == "true":
        return True
    if value == "false":
        return False
    if value in ("null-or-missing", "missing-document"):
        return inherited if inherited is not None else False
    return None


def _effective_flags(rows: list[dict[str, str]]) -> tuple[dict[str, bool | None], list[dict[str, Any]]]:
    globals_by_namespace = {
        row["namespace"]: row for row in rows if row["scope"] == "global"
    }
    global_vpn = globals_by_namespace.get("vpnProtection")
    global_plugin = globals_by_namespace.get("cod4xPlugin")
    global_flags = {
        "vpnProtection": _resolve_flag(
            global_vpn["enabled"] if global_vpn else "missing-document"
        ),
        "pluginEnabled": _resolve_flag(
            global_plugin["enabled"] if global_plugin else "missing-document"
        ),
        "pluginVpnProtectionEnabled": _resolve_flag(
            global_plugin["plugin_vpn_enabled"] if global_plugin else "missing-document"
        ),
    }

    server_rows: dict[str, dict[str, dict[str, str]]] = {}
    for row in rows:
        if row["scope"] == "server":
            server_rows.setdefault(row["server_id"], {})[row["namespace"]] = row

    effective_servers = []
    for server_id, configurations in sorted(server_rows.items()):
        vpn = configurations.get("vpnProtection")
        plugin = configurations.get("cod4xPlugin")
        feature_enabled = _resolve_flag(
            vpn["enabled"] if vpn else "missing-document",
            global_flags["vpnProtection"],
        )
        plugin_enabled = _resolve_flag(
            plugin["enabled"] if plugin else "missing-document",
            global_flags["pluginEnabled"],
        )
        plugin_vpn_enabled = _resolve_flag(
            plugin["plugin_vpn_enabled"] if plugin else "missing-document",
            global_flags["pluginVpnProtectionEnabled"],
        )
        if any(value is None for value in (feature_enabled, plugin_enabled, plugin_vpn_enabled)):
            effective = "unknown"
        else:
            effective = (
                "true"
                if feature_enabled and plugin_enabled and plugin_vpn_enabled
                else "false"
            )
        effective_servers.append(
            {
                "server_id": server_id,
                "game_type": vpn["game_type"] if vpn else "",
                "server_list_enabled": vpn["server_list_enabled"] if vpn else "",
                "feature_enabled": feature_enabled,
                "plugin_enabled": plugin_enabled,
                "plugin_vpn_enabled": plugin_vpn_enabled,
                "effective": effective,
                "vpn_modified_utc": vpn["modified_utc"] if vpn else "",
                "plugin_modified_utc": plugin["modified_utc"] if plugin else "",
            }
        )
    return global_flags, effective_servers


def _format_flag(value: bool | None) -> str:
    return "unknown" if value is None else str(value).lower()


def _render_status(rows: list[dict[str, str]]) -> tuple[dict[str, bool | None], list[dict[str, Any]]]:
    global_flags, servers = _effective_flags(rows)
    for row in rows:
        if row["scope"] != "global":
            continue
        if row["namespace"] == "vpnProtection":
            print(
                "global vpnProtection: "
                f"document={row['document_status']} schemaVersion={row['schema_version']} "
                f"enabled={row['enabled']} rules={row['rule_count']} "
                f"ruleOverrides={row['rule_override_count']} "
                f"excludedPlayerTags={row['excluded_tag_count']} "
                f"lastModifiedUtc={row['modified_utc'] or 'unknown'}"
            )
        else:
            print(
                "global cod4xPlugin: "
                f"document={row['document_status']} enabled={row['enabled']} "
                f"vpnProtectionEnabled={row['plugin_vpn_enabled']} "
                f"lastModifiedUtc={row['modified_utc'] or 'unknown'}"
            )

    print(
        "global effective controls: "
        f"vpnProtection={_format_flag(global_flags['vpnProtection'])} "
        f"pluginVpnProtection={_format_flag(global_flags['pluginVpnProtectionEnabled'])} "
        f"pluginEnabled={_format_flag(global_flags['pluginEnabled'])}"
    )
    for server in servers:
        print(
            f"server={server['server_id']} gameType={server['game_type'] or 'unknown'} "
            f"serverListEnabled={server['server_list_enabled'] or 'unknown'} "
            f"vpnProtection={_format_flag(server['feature_enabled'])} "
            f"pluginEnabled={_format_flag(server['plugin_enabled'])} "
            f"pluginVpnProtection={_format_flag(server['plugin_vpn_enabled'])} "
            f"effective={server['effective']} "
            f"vpnLastModifiedUtc={server['vpn_modified_utc'] or 'unknown'} "
            f"pluginLastModifiedUtc={server['plugin_modified_utc'] or 'unknown'}"
        )

    if not servers:
        print("active-server-count=0 effectiveState=unknown")
    else:
        all_disabled = (
            global_flags["vpnProtection"] is False
            and global_flags["pluginVpnProtectionEnabled"] is False
            and all(
                server["feature_enabled"] is False
                and server["plugin_vpn_enabled"] is False
                for server in servers
            )
        )
        any_enabled = (
            global_flags["vpnProtection"] is True
            or global_flags["pluginVpnProtectionEnabled"] is True
            or any(
                server["feature_enabled"] is True
                or server["plugin_vpn_enabled"] is True
                for server in servers
            )
        )
        overall = "disabled" if all_disabled else "enabled" if any_enabled else "unknown"
        print(f"active-server-count={len(servers)} all-vpn-controls={overall}")
    return global_flags, servers


def _assert_cutover_preconditions(
    global_flags: dict[str, bool | None], servers: list[dict[str, Any]]
) -> None:
    if not servers:
        raise RuntimeError("Cutover refused: no active servers were returned for verification.")
    if (
        global_flags["vpnProtection"] is not False
        or global_flags["pluginVpnProtectionEnabled"] is not False
        or any(
            server["feature_enabled"] is not False
            or server["plugin_vpn_enabled"] is not False
            for server in servers
        )
    ):
        raise RuntimeError(
            "Cutover refused: every global and active-server VPN control must be verified disabled."
        )


def _backup_payload() -> tuple[str, str]:
    output = _run_sql(BACKUP_SQL)
    digest = None
    encoded_chunks = []
    for line in output:
        if line.startswith("HASH|") and re.fullmatch(r"[0-9A-Fa-f]{64}", line[5:]):
            digest = line[5:].lower()
        elif line.startswith("DATA|") and re.fullmatch(r"[0-9A-Fa-f]+", line[5:]):
            encoded_chunks.append(line[5:])
        else:
            raise RuntimeError("Unexpected backup response; no settings were changed.")
    if digest is None or not encoded_chunks:
        raise RuntimeError("The secure VPN configuration backup could not be prepared.")
    try:
        payload = bytes.fromhex("".join(encoded_chunks)).decode("utf-16le")
        records = json.loads(payload)
    except (ValueError, UnicodeDecodeError, json.JSONDecodeError) as error:
        raise RuntimeError("The VPN configuration backup could not be decoded.") from error
    if not isinstance(records, list):
        raise RuntimeError("The VPN configuration backup has an unexpected shape.")
    return json.dumps(records, ensure_ascii=False, separators=(",", ":")), digest


def _upload_backup(payload: str, digest: str) -> str:
    account = _environment("SQL_BACKUP_STORAGE_ACCOUNT")
    run_id = _environment("GITHUB_RUN_ID")
    attempt = _environment("GITHUB_RUN_ATTEMPT")
    blob_name = f"vpn-protection-cutover/{run_id}-{attempt}-{digest[:12]}.json"
    temp_dir = Path(_environment("RUNNER_TEMP"))
    temp_file = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w", encoding="utf-8", newline="\n", dir=temp_dir, prefix="vpn-settings-", suffix=".json", delete=False
        ) as backup:
            temp_file = Path(backup.name)
            backup.write(payload)
        upload = subprocess.run(
            [
                "az",
                "storage",
                "blob",
                "upload",
                "--account-name",
                account,
                "--container-name",
                "sql-backups",
                "--name",
                blob_name,
                "--file",
                str(temp_file),
                "--auth-mode",
                "login",
                "--only-show-errors",
                "--output",
                "none",
            ],
            capture_output=True,
            text=True,
            check=False,
        )
        if upload.returncode != 0:
            raise RuntimeError("Private backup upload failed; no settings were changed.")
        exists = subprocess.run(
            [
                "az",
                "storage",
                "blob",
                "exists",
                "--account-name",
                account,
                "--container-name",
                "sql-backups",
                "--name",
                blob_name,
                "--auth-mode",
                "login",
                "--query",
                "exists",
                "--output",
                "tsv",
            ],
            capture_output=True,
            text=True,
            check=False,
        )
        if exists.returncode != 0 or exists.stdout.strip().lower() != "true":
            raise RuntimeError("Private backup verification failed; no settings were changed.")
        return blob_name
    finally:
        if temp_file is not None:
            temp_file.unlink(missing_ok=True)


def _apply_cutover(digest: str) -> int:
    if not re.fullmatch(r"[0-9a-f]{64}", digest):
        raise RuntimeError("Invalid snapshot digest; no settings were changed.")
    output = _run_sql(CUTOVER_SQL.replace("__EXPECTED_HASH__", digest.upper()))
    for line in output:
        match = re.fullmatch(r"CUTOVER\|(\d+)", line)
        if match:
            return int(match.group(1))
    raise RuntimeError("The cutover transaction did not return its sanitized result.")


def main() -> None:
    operation = _environment("VPN_SETTINGS_OPERATION")
    if operation not in {"verify", "cutover"}:
        raise RuntimeError("Operation must be verify or cutover.")

    rows = _parse_status()
    global_flags, servers = _render_status(rows)
    if operation == "verify":
        print(
            "cache note: run at least 9 minutes after the last settings update; "
            "this query reads stored settings, not live in-process cache contents."
        )
        return

    _assert_cutover_preconditions(global_flags, servers)
    payload, digest = _backup_payload()
    blob_name = _upload_backup(payload, digest)
    deleted_count = _apply_cutover(digest)
    print(
        "cutover completed: "
        "global vpnProtection replaced with schemaVersion=2 enabled=false and empty rules; "
        f"deleted {deleted_count} vpnProtection server override(s)."
    )
    print(
        f"privateRecoveryBackup=sql-backups/{blob_name} "
        f"sha256={hashlib.sha256(payload.encode('utf-8')).hexdigest()}"
    )
    verification_rows = _parse_status()
    verification_flags, verification_servers = _effective_flags(verification_rows)
    global_vpn = next(
        (
            row
            for row in verification_rows
            if row["scope"] == "global" and row["namespace"] == "vpnProtection"
        ),
        None,
    )
    if (
        global_vpn is None
        or global_vpn["schema_version"] != "2"
        or global_vpn["enabled"] != "false"
        or global_vpn["rule_count"] != "0"
        or global_vpn["rule_override_count"] != "0"
        or global_vpn["excluded_tag_count"] != "0"
        or verification_flags["pluginVpnProtectionEnabled"] is not False
        or any(server["feature_enabled"] is not False for server in verification_servers)
        or any(server["plugin_vpn_enabled"] is not False for server in verification_servers)
        or verification_flags["vpnProtection"] is not False
    ):
        raise RuntimeError(
            "Post-cutover verification failed; the private VPN settings backup is available for recovery."
        )
    print("post-cutover verification: schemaVersion=2 enabled=false empty rules; server overrides absent.")
    print("cache note: direct SQL bypasses Repository API invalidation; wait at least 9 minutes before relying on consumer state.")


if __name__ == "__main__":
    try:
        main()
    except RuntimeError as error:
        print(f"::error::{error}", file=sys.stderr)
        raise SystemExit(1) from None
