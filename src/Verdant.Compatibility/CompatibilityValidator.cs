namespace Verdant.Compatibility;

public sealed class CompatibilityValidator
{
    private static readonly CompatibilityDimension[] CanonicalDimensions =
    [
        CompatibilityDimension.EngineVersion,
        CompatibilityDimension.RulesetVersion,
        CompatibilityDimension.RulePackVersion,
        CompatibilityDimension.CatalogVersion,
        CompatibilityDimension.SaveFormatVersion,
        CompatibilityDimension.ReplayFormatVersion,
        CompatibilityDimension.FixtureVersion
    ];

    public static CompatibilityValidationResult Validate(
        CompatibilityDescriptor required,
        CompatibilityDescriptor available)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(available);

        var mismatches = new List<CompatibilityMismatch>();
        foreach (var dimension in CanonicalDimensions)
        {
            var requiredIdentity = required.GetIdentity(dimension);
            var availableIdentity = available.GetIdentity(dimension);
            if (!StringComparer.Ordinal.Equals(
                    requiredIdentity.Value,
                    availableIdentity.Value))
            {
                mismatches.Add(new CompatibilityMismatch(
                    CompatibilityMismatchCode.IdentityMismatch,
                    dimension,
                    requiredIdentity,
                    availableIdentity));
            }
        }

        return mismatches.Count == 0
            ? new CompatibilityValidationResult.Compatible()
            : new CompatibilityValidationResult.Incompatible(mismatches);
    }
}