namespace Verdant.Compatibility;

public abstract class CompatibilityValidationResult : IEquatable<CompatibilityValidationResult>
{
    protected CompatibilityValidationResult()
    {
    }

    public abstract bool Equals(CompatibilityValidationResult? other);

    public override bool Equals(object? obj) => obj is CompatibilityValidationResult other && Equals(other);

    public abstract override int GetHashCode();

    public sealed class Compatible : CompatibilityValidationResult
    {
        public override bool Equals(CompatibilityValidationResult? other) => other is Compatible;

        public override int GetHashCode() => typeof(Compatible).GetHashCode();
    }

    public sealed class Incompatible : CompatibilityValidationResult
    {
        public Incompatible(IEnumerable<CompatibilityMismatch> mismatches)
        {
            ArgumentNullException.ThrowIfNull(mismatches);
            var materialized = mismatches.ToArray();
            if (materialized.Length == 0)
            {
                throw new ArgumentException(
                    "An incompatible result requires at least one mismatch.",
                    nameof(mismatches));
            }

            if (materialized.Any(item => item is null))
            {
                throw new ArgumentException(
                    "Mismatch collections cannot contain null entries.",
                    nameof(mismatches));
            }

            Mismatches = Array.AsReadOnly(materialized);
        }

        public IReadOnlyList<CompatibilityMismatch> Mismatches { get; }

        public override bool Equals(CompatibilityValidationResult? other) =>
            other is Incompatible incompatible && Mismatches.SequenceEqual(incompatible.Mismatches);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var mismatch in Mismatches)
            {
                hash.Add(mismatch);
            }

            return hash.ToHashCode();
        }
    }
}
