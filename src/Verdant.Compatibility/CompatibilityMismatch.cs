namespace Verdant.Compatibility;

public enum CompatibilityMismatchCode
{
    IdentityMismatch
}

public sealed record CompatibilityMismatch(
    CompatibilityMismatchCode Code,
    CompatibilityDimension Dimension,
    CompatibilityIdentity RequiredIdentity,
    CompatibilityIdentity AvailableIdentity);