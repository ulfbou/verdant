using System.Collections;
using System.Reflection;
using System.Text;
using Verdant.Compatibility;

namespace Verdant.Compatibility.Tests;

public sealed class CompatibilityValidatorTests
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

    [Fact]
    public void IdenticalCompositeIdentitiesAreCompatible()
    {
        var descriptor = CreateDescriptor();
        var result = CompatibilityValidator.Validate(descriptor, descriptor);
        Assert.IsType<CompatibilityValidationResult.Compatible>(result);
    }

    [Theory]
    [MemberData(nameof(Dimensions))]
    public void EveryDimensionIndependentlyCausesTypedIncompatibility(
        CompatibilityDimension dimension)
    {
        var result = ValidateWithAvailableChange(dimension, "different");
        var incompatible = Assert.IsType<CompatibilityValidationResult.Incompatible>(result);
        var mismatch = Assert.Single(incompatible.Mismatches);
        Assert.Equal(CompatibilityMismatchCode.IdentityMismatch, mismatch.Code);
        Assert.Equal(dimension, mismatch.Dimension);
        Assert.Equal("required", mismatch.RequiredIdentity.Value);
        Assert.Equal("different", mismatch.AvailableIdentity.Value);
    }

    [Fact]
    public void FormatMismatchDoesNotMasqueradeAsRulesetMismatch()
    {
        var mismatch = AssertSingleMismatch(
            ValidateWithAvailableChange(CompatibilityDimension.SaveFormatVersion, "save-v2"));
        Assert.Equal(CompatibilityDimension.SaveFormatVersion, mismatch.Dimension);
        Assert.NotEqual(CompatibilityDimension.RulesetVersion, mismatch.Dimension);
    }

    [Fact]
    public void FixtureMismatchDoesNotAlterGameplayIdentities()
    {
        var required = CreateDescriptor();
        var available = With(required, CompatibilityDimension.FixtureVersion, "fixture-v2");
        var mismatch = AssertSingleMismatch(CompatibilityValidator.Validate(required, available));
        Assert.Equal(CompatibilityDimension.FixtureVersion, mismatch.Dimension);
        Assert.Equal(required.EngineVersion, available.EngineVersion);
        Assert.Equal(required.RulesetVersion, available.RulesetVersion);
        Assert.Equal(required.RulePackVersion, available.RulePackVersion);
        Assert.Equal(required.CatalogVersion, available.CatalogVersion);
    }

    [Fact]
    public void RulePackRulesetAndCatalogMismatchesRemainDistinct()
    {
        var required = CreateDescriptor();
        var available = With(
            With(
                With(required, CompatibilityDimension.CatalogVersion, "catalog-v2"),
                CompatibilityDimension.RulePackVersion,
                "pack-v2"),
            CompatibilityDimension.RulesetVersion,
            "rules-v2");
        var incompatible = Assert.IsType<CompatibilityValidationResult.Incompatible>(
            CompatibilityValidator.Validate(required, available));
        Assert.Equal(
            [
                CompatibilityDimension.RulesetVersion,
                CompatibilityDimension.RulePackVersion,
                CompatibilityDimension.CatalogVersion
            ],
            incompatible.Mismatches.Select(item => item.Dimension));
    }

    [Fact]
    public void MultipleMismatchesUseFixedCanonicalDimensionOrder()
    {
        var required = CreateDescriptor();
        var available = required;
        foreach (var dimension in CanonicalDimensions.Reverse())
        {
            available = With(available, dimension, $"different-{(int)dimension}");
        }
        var incompatible = Assert.IsType<CompatibilityValidationResult.Incompatible>(
            CompatibilityValidator.Validate(required, available));
        Assert.Equal(CanonicalDimensions, incompatible.Mismatches.Select(item => item.Dimension));
    }

    [Fact]
    public void ComparisonIsExactAndCaseSensitive()
    {
        var mismatch = AssertSingleMismatch(
            ValidateWithAvailableChange(CompatibilityDimension.EngineVersion, "REQUIRED"));
        Assert.Equal("required", mismatch.RequiredIdentity.Value);
        Assert.Equal("REQUIRED", mismatch.AvailableIdentity.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" required")]
    [InlineData("required ")]
    [InlineData("required\tvalue")]
    [InlineData("required/value")]
    [InlineData(".required")]
    public void MissingBlankWhitespaceOrMalformedIdentitiesFailAtConstruction(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CompatibilityIdentity(value!));
    }

    [Fact]
    public void WhitespaceIsRejectedRatherThanSilentlyTrimmed()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new CompatibilityIdentity(" required "));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void NoFallbackToLatestCurrentWildcardOrOrderingOccurs()
    {
        foreach (var available in new[] { "latest", "current", "*", "2", "10" })
        {
            if (available == "*")
            {
                Assert.Throws<ArgumentException>(() => new CompatibilityIdentity(available));
                continue;
            }
            var mismatch = AssertSingleMismatch(
                ValidateWithAvailableChange(CompatibilityDimension.EngineVersion, available));
            Assert.Equal(available, mismatch.AvailableIdentity.Value);
        }
    }

    [Fact]
    public void RepeatedValidationIsStructurallyIdentical()
    {
        var required = CreateDescriptor();
        var available = With(required, CompatibilityDimension.ReplayFormatVersion, "replay-v2");
        var first = CompatibilityValidator.Validate(required, available);
        var second = CompatibilityValidator.Validate(required, available);
        Assert.Equal(first, second);
    }

    [Fact]
    public void DescriptorsAndIdentitiesExposeOnlyInitTimeImmutableState()
    {
        var descriptorProperties = typeof(CompatibilityDescriptor).GetProperties();
        Assert.Equal(7, descriptorProperties.Length);
        Assert.All(descriptorProperties, property => Assert.False(property.CanWrite));
        Assert.False(typeof(CompatibilityDescriptor).IsAssignableTo(typeof(IDictionary)));
        Assert.False(typeof(CompatibilityIdentity).GetProperty(nameof(CompatibilityIdentity.Value))!.CanWrite);
    }

    [Fact]
    public void MismatchCollectionsAreReadOnlyAndDefensivelyCopied()
    {
        var source = new List<CompatibilityMismatch>
        {
            new(
                CompatibilityMismatchCode.IdentityMismatch,
                CompatibilityDimension.EngineVersion,
                Id("required"),
                Id("available"))
        };
        var result = new CompatibilityValidationResult.Incompatible(source);
        source.Clear();
        Assert.Single(result.Mismatches);
        Assert.False(result.Mismatches is CompatibilityMismatch[]);
        var collection = Assert.IsAssignableFrom<ICollection<CompatibilityMismatch>>(result.Mismatches);
        Assert.True(collection.IsReadOnly);
    }

    [Fact]
    public void ValidationHasNoHostExecutionOrAuthorityDependencies()
    {
        var assembly = typeof(CompatibilityValidator).Assembly;
        var references = assembly.GetReferencedAssemblies()
            .Select(item => item.Name ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(references, name => name.StartsWith("Verdant.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("JSInterop", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("Http", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("Sql", StringComparison.Ordinal));

        var validatorFields = typeof(CompatibilityValidator)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.Empty(validatorFields);
        var validateParameters = typeof(CompatibilityValidator)
            .GetMethod(nameof(CompatibilityValidator.Validate))!
            .GetParameters()
            .Select(item => item.ParameterType)
            .ToArray();
        Assert.Equal([typeof(CompatibilityDescriptor), typeof(CompatibilityDescriptor)], validateParameters);
    }

    [Fact]
    public void ProductionAssemblyContainsNoConsumerSpecificTerminology()
    {
        var bytes = File.ReadAllBytes(typeof(CompatibilityValidator).Assembly.Location);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain("FirstBloom", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mold", text, StringComparison.OrdinalIgnoreCase);
    }

    public static TheoryData<CompatibilityDimension> Dimensions()
    {
        var data = new TheoryData<CompatibilityDimension>();
        foreach (var dimension in CanonicalDimensions)
        {
            data.Add(dimension);
        }
        return data;
    }

    private static CompatibilityValidationResult ValidateWithAvailableChange(
        CompatibilityDimension dimension,
        string value)
    {
        var required = CreateDescriptor();
        return CompatibilityValidator.Validate(required, With(required, dimension, value));
    }

    private static CompatibilityMismatch AssertSingleMismatch(CompatibilityValidationResult result)
    {
        var incompatible = Assert.IsType<CompatibilityValidationResult.Incompatible>(result);
        return Assert.Single(incompatible.Mismatches);
    }

    private static CompatibilityDescriptor CreateDescriptor() =>
        new(
            Id("required"),
            Id("required"),
            Id("required"),
            Id("required"),
            Id("required"),
            Id("required"),
            Id("required"));

    private static CompatibilityDescriptor With(
        CompatibilityDescriptor descriptor,
        CompatibilityDimension dimension,
        string value)
    {
        var identity = Id(value);
        return new CompatibilityDescriptor(
            dimension == CompatibilityDimension.EngineVersion ? identity : descriptor.EngineVersion,
            dimension == CompatibilityDimension.RulesetVersion ? identity : descriptor.RulesetVersion,
            dimension == CompatibilityDimension.RulePackVersion ? identity : descriptor.RulePackVersion,
            dimension == CompatibilityDimension.CatalogVersion ? identity : descriptor.CatalogVersion,
            dimension == CompatibilityDimension.SaveFormatVersion ? identity : descriptor.SaveFormatVersion,
            dimension == CompatibilityDimension.ReplayFormatVersion ? identity : descriptor.ReplayFormatVersion,
            dimension == CompatibilityDimension.FixtureVersion ? identity : descriptor.FixtureVersion);
    }

    private static CompatibilityIdentity Id(string value) => new(value);
}