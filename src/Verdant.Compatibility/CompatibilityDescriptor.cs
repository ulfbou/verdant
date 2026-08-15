namespace Verdant.Compatibility;

public sealed record CompatibilityDescriptor
{
    public CompatibilityDescriptor(
        CompatibilityIdentity engineVersion,
        CompatibilityIdentity rulesetVersion,
        CompatibilityIdentity rulePackVersion,
        CompatibilityIdentity catalogVersion,
        CompatibilityIdentity saveFormatVersion,
        CompatibilityIdentity replayFormatVersion,
        CompatibilityIdentity fixtureVersion)
    {
        ArgumentNullException.ThrowIfNull(engineVersion);
        ArgumentNullException.ThrowIfNull(rulesetVersion);
        ArgumentNullException.ThrowIfNull(rulePackVersion);
        ArgumentNullException.ThrowIfNull(catalogVersion);
        ArgumentNullException.ThrowIfNull(saveFormatVersion);
        ArgumentNullException.ThrowIfNull(replayFormatVersion);
        ArgumentNullException.ThrowIfNull(fixtureVersion);

        EngineVersion = engineVersion;
        RulesetVersion = rulesetVersion;
        RulePackVersion = rulePackVersion;
        CatalogVersion = catalogVersion;
        SaveFormatVersion = saveFormatVersion;
        ReplayFormatVersion = replayFormatVersion;
        FixtureVersion = fixtureVersion;
    }

    public CompatibilityIdentity EngineVersion { get; }
    public CompatibilityIdentity RulesetVersion { get; }
    public CompatibilityIdentity RulePackVersion { get; }
    public CompatibilityIdentity CatalogVersion { get; }
    public CompatibilityIdentity SaveFormatVersion { get; }
    public CompatibilityIdentity ReplayFormatVersion { get; }
    public CompatibilityIdentity FixtureVersion { get; }

    public CompatibilityIdentity GetIdentity(CompatibilityDimension dimension) =>
        dimension switch
        {
            CompatibilityDimension.EngineVersion => EngineVersion,
            CompatibilityDimension.RulesetVersion => RulesetVersion,
            CompatibilityDimension.RulePackVersion => RulePackVersion,
            CompatibilityDimension.CatalogVersion => CatalogVersion,
            CompatibilityDimension.SaveFormatVersion => SaveFormatVersion,
            CompatibilityDimension.ReplayFormatVersion => ReplayFormatVersion,
            CompatibilityDimension.FixtureVersion => FixtureVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(dimension))
        };
}