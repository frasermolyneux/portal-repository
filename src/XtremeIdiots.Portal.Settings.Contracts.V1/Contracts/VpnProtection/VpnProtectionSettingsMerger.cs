namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

public sealed class VpnProtectionSettingsMerger
{
    private readonly VpnProtectionSettingsValidator validator = new();

    public EffectiveVpnProtectionSettings Merge(
        VpnProtectionSettingsDocument? globalDocument,
        VpnProtectionSettingsDocument? serverDocument)
    {
        if (!validator.Validate(globalDocument).IsValid || !validator.Validate(serverDocument).IsValid)
        {
            return EffectiveVpnProtectionSettings.Disabled(validationFailed: true);
        }

        var mergedRules = new List<VpnProtectionRule>();
        if (serverDocument?.InheritGlobalRules is not false)
        {
            mergedRules.AddRange((globalDocument?.Rules ?? []).Select(CloneRule));
            ApplyOverrides(mergedRules, serverDocument?.RuleOverrides ?? []);
        }

        mergedRules.AddRange((serverDocument?.Rules ?? []).Select(CloneRule));

        var mergedValidation = validator.Validate(new VpnProtectionSettingsDocument
        {
            Enabled = serverDocument?.Enabled ?? globalDocument?.Enabled,
            Rules = mergedRules,
            ExcludedPlayerTags = MergeExcludedPlayerTags(globalDocument, serverDocument)
        });

        if (!mergedValidation.IsValid)
        {
            return EffectiveVpnProtectionSettings.Disabled(validationFailed: true);
        }

        return new EffectiveVpnProtectionSettings
        {
            Enabled = serverDocument?.Enabled ?? globalDocument?.Enabled ?? false,
            Rules = mergedRules.Select((rule, index) => ToEffectiveRule(rule, index)).ToArray(),
            ExcludedPlayerTags = new HashSet<string>(
                MergeExcludedPlayerTags(globalDocument, serverDocument),
                StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void ApplyOverrides(
        List<VpnProtectionRule> rules,
        IReadOnlyList<VpnProtectionRuleOverride> ruleOverrides)
    {
        foreach (var ruleOverride in ruleOverrides)
        {
            var ruleIndex = rules.FindIndex(rule =>
                string.Equals(rule.Id, ruleOverride.Id, StringComparison.OrdinalIgnoreCase));

            if (ruleIndex < 0)
            {
                continue;
            }

            var rule = rules[ruleIndex];
            rules[ruleIndex] = new VpnProtectionRule
            {
                Id = rule.Id,
                Enabled = ruleOverride.Enabled ?? rule.Enabled,
                Signal = ruleOverride.Signal ?? rule.Signal,
                Operator = ruleOverride.Operator ?? rule.Operator,
                ExpectedValue = ruleOverride.ExpectedValue ?? rule.ExpectedValue,
                Action = ruleOverride.Action ?? rule.Action,
                ReasonTemplate = ruleOverride.ReasonTemplate ?? rule.ReasonTemplate,
                ExtensionData = rule.ExtensionData
            };
        }
    }

    private static string[] MergeExcludedPlayerTags(
        VpnProtectionSettingsDocument? globalDocument,
        VpnProtectionSettingsDocument? serverDocument)
    {
        return (globalDocument?.ExcludedPlayerTags ?? [])
            .Concat(serverDocument?.ExcludedPlayerTags ?? [])
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static VpnProtectionRule CloneRule(VpnProtectionRule rule) => new()
    {
        Id = rule.Id,
        Enabled = rule.Enabled,
        Signal = rule.Signal,
        Operator = rule.Operator,
        ExpectedValue = rule.ExpectedValue,
        Action = rule.Action,
        ReasonTemplate = rule.ReasonTemplate,
        ExtensionData = rule.ExtensionData
    };

    private static EffectiveVpnProtectionRule ToEffectiveRule(VpnProtectionRule rule, int orderIndex) => new()
    {
        Id = rule.Id.Trim(),
        Enabled = rule.Enabled ?? true,
        Signal = rule.Signal,
        Operator = rule.Operator,
        ExpectedValue = rule.ExpectedValue.Trim(),
        Action = rule.Action,
        ReasonTemplate = string.IsNullOrWhiteSpace(rule.ReasonTemplate)
            ? VpnProtectionSettingsConstants.DefaultReasonTemplate
            : rule.ReasonTemplate.Trim(),
        OrderIndex = orderIndex
    };
}