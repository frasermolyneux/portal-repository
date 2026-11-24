#!/usr/bin/env python3
"""Temporary, manually dispatched VPN settings verification operation."""

from __future__ import annotations

import os
import subprocess
import sys
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


def _resolve_flag(
    value: str, inherited: bool | None = None, *, has_inherited: bool = False
) -> bool | None:
    if value == "true":
        return True
    if value == "false":
        return False
    if value in ("null-or-missing", "missing-document"):
        return inherited if has_inherited else False
    return None


def _resolve_document_flag(
    row: dict[str, str] | None,
    property_name: str,
    inherited: bool | None = None,
    *,
    has_inherited: bool = False,
) -> bool | None:
    if row is None:
        return inherited if has_inherited else False
    if row["document_status"] == "invalid-json":
        return None
    if row["document_status"] == "valid-json" and row["schema_version"] not in {"0", "1"}:
        return None
    if row["document_status"] not in {"missing-document", "valid-json"}:
        return None
    return _resolve_flag(
        row[property_name],
        inherited,
        has_inherited=has_inherited,
    )


def _effective_flags(rows: list[dict[str, str]]) -> tuple[dict[str, bool | None], list[dict[str, Any]]]:
    globals_by_namespace = {
        row["namespace"]: row for row in rows if row["scope"] == "global"
    }
    global_vpn = globals_by_namespace.get("vpnProtection")
    global_plugin = globals_by_namespace.get("cod4xPlugin")
    global_flags = {
        "vpnProtection": _resolve_document_flag(
            global_vpn,
            "enabled",
        ),
        "pluginEnabled": _resolve_document_flag(
            global_plugin,
            "enabled",
        ),
        "pluginVpnProtectionEnabled": _resolve_document_flag(
            global_plugin,
            "plugin_vpn_enabled",
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
        feature_enabled = _resolve_document_flag(
            vpn,
            "enabled",
            global_flags["vpnProtection"],
            has_inherited=True,
        )
        plugin_enabled = _resolve_document_flag(
            plugin,
            "enabled",
            global_flags["pluginEnabled"],
            has_inherited=True,
        )
        plugin_vpn_enabled = _resolve_document_flag(
            plugin,
            "plugin_vpn_enabled",
            global_flags["pluginVpnProtectionEnabled"],
            has_inherited=True,
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
                "global stored vpnProtection: "
                f"document={row['document_status']} schemaVersion={row['schema_version']} "
                f"storedEnabled={row['enabled']} rules={row['rule_count']} "
                f"ruleOverrides={row['rule_override_count']} "
                f"excludedPlayerTags={row['excluded_tag_count']} "
                f"lastModifiedUtc={row['modified_utc'] or 'unknown'}"
            )
        else:
            print(
                "global stored cod4xPlugin: "
                f"document={row['document_status']} storedEnabled={row['enabled']} "
                f"storedVpnProtectionEnabled={row['plugin_vpn_enabled']} "
                f"lastModifiedUtc={row['modified_utc'] or 'unknown'}"
            )

    print(
        "global stored/inherited controls: "
        f"vpnProtection={_format_flag(global_flags['vpnProtection'])} "
        f"pluginVpnProtection={_format_flag(global_flags['pluginVpnProtectionEnabled'])} "
        f"pluginEnabled={_format_flag(global_flags['pluginEnabled'])}"
    )
    for server in servers:
        print(
            f"server={server['server_id']} gameType={server['game_type'] or 'unknown'} "
            f"serverListEnabled={server['server_list_enabled'] or 'unknown'} "
            f"vpnProtectionStoredOrInherited={_format_flag(server['feature_enabled'])} "
            f"pluginEnabledStoredOrInherited={_format_flag(server['plugin_enabled'])} "
            f"pluginVpnProtectionStoredOrInherited={_format_flag(server['plugin_vpn_enabled'])} "
            f"derivedFromStoredFlags={server['effective']} "
            f"vpnLastModifiedUtc={server['vpn_modified_utc'] or 'unknown'} "
            f"pluginLastModifiedUtc={server['plugin_modified_utc'] or 'unknown'}"
        )

    if not servers:
        print("active-server-count=0 derivedState=unknown")
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
        print(f"active-server-count={len(servers)} all-stored-controls={overall}")
    return global_flags, servers


def main() -> None:
    operation = _environment("VPN_SETTINGS_OPERATION")
    if operation != "verify":
        raise RuntimeError("Operation must be verify.")

    rows = _parse_status()
    _render_status(rows)
    print(
        "cache note: run at least 9 minutes after the last settings update; "
        "this query reads stored settings, not live in-process cache contents."
    )
    print(
        "validation note: inheritance is derived from stored control flags; "
        "typed contract validation and live plugin process state are not queried."
    )


if __name__ == "__main__":
    try:
        main()
    except RuntimeError as error:
        print(f"::error::{error}", file=sys.stderr)
        raise SystemExit(1) from None
