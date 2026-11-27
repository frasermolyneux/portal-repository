using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

/// <summary>Creates the stable identity of one configured action for one captured occurrence.</summary>
public static class RuleOccurrenceEffectIdentity
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string Create(Guid gameServerId, Guid occurrenceId, string featureId, string ruleId, string actionId)
    {
        if (!TryCreate(gameServerId, occurrenceId, featureId, ruleId, actionId, out var identity))
        {
            throw new ArgumentException("A server, occurrence and valid feature, rule and action identifiers are required.");
        }

        return identity;
    }

    public static bool TryCreate(
        Guid gameServerId, Guid occurrenceId, string? featureId, string? ruleId, string? actionId, out string identity)
    {
        identity = string.Empty;
        if (gameServerId == Guid.Empty || occurrenceId == Guid.Empty ||
            !ValidIdentifier(featureId, 64) || !ValidIdentifier(ruleId, 128) || !ValidIdentifier(actionId, 128))
        {
            return false;
        }

        try
        {
            using var input = new MemoryStream();
            var prefix = $"portal-rule-occurrence-effect-v1\n{gameServerId:N}\n{occurrenceId:N}\n";
            input.Write(Encoding.ASCII.GetBytes(prefix));
            foreach (var value in new[] { featureId!, ruleId!, actionId! })
            {
                var bytes = StrictUtf8.GetBytes(value);
                var length = new byte[4];
                BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
                input.Write(length);
                input.Write(bytes);
            }

            identity = Convert.ToHexStringLower(SHA256.HashData(input.ToArray()));
            return true;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    private static bool ValidIdentifier(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength && value.Trim() == value;
}
