using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Verdant.Serialization;
using Xunit;

namespace Verdant.Serialization.Tests;

public sealed class CanonicalSerializationTests
{
    [Theory]
    [MemberData(nameof(ExactScalarCases))]
    public void ScalarsProduceExactCanonicalBytes(CanonicalValue value, string expected)
    {
        AssertBytes(value, expected);
    }

    public static TheoryData<CanonicalValue, string> ExactScalarCases => new()
    {
        { CanonicalValue.Null.Value, "null" },
        { new CanonicalValue.BooleanValue(true), "true" },
        { new CanonicalValue.BooleanValue(false), "false" },
        { new CanonicalValue.StringValue(string.Empty), "\"\"" },
        { new CanonicalValue.IntegerValue(0), "0" },
        { new CanonicalValue.IntegerValue(42), "42" },
        { new CanonicalValue.IntegerValue(-42), "-42" },
        { new CanonicalValue.IntegerValue(long.MinValue), "-9223372036854775808" },
        { new CanonicalValue.IntegerValue(long.MaxValue), "9223372036854775807" }
    };

    [Fact]
    public void ObjectsAreOrderedCanonicallyIndependentOfInsertionOrder()
    {
        var first = Object(("z", Integer(1)), ("a", Integer(2)));
        var second = Object(("a", Integer(2)), ("z", Integer(1)));

        AssertBytes(first, "{\"a\":2,\"z\":1}");
        Assert.Equal(CanonicalJson.SerializeToUtf8(first), CanonicalJson.SerializeToUtf8(second));
        Assert.Equal(CanonicalSha256.Compute(first).Hexadecimal, CanonicalSha256.Compute(second).Hexadecimal);
    }

    [Fact]
    public void NestedObjectsOrderMembersAtEveryLevel()
    {
        var value = Object(
            ("z", Object(("b", Integer(2)), ("a", Integer(1)))),
            ("a", Integer(0)));
        AssertBytes(value, "{\"a\":0,\"z\":{\"a\":1,\"b\":2}}");
    }

    [Fact]
    public void DuplicateMemberNamesFailExplicitly()
    {
        Assert.Throws<ArgumentException>(() => Object(("a", Integer(1)), ("a", Integer(2))));
    }

    [Fact]
    public void MemberNamesAndStringsUseExactJsonEscapingAndUtf8()
    {
        var value = Object(("a\"", new CanonicalValue.StringValue("line\n\t\u0001\\\"é")));
        AssertBytes(value, "{\"a\\\"\":\"line\\n\\t\\u0001\\\\\\\"é\"}");
        Assert.DoesNotContain((byte)0xEF, CanonicalJson.SerializeToUtf8(new CanonicalValue.StringValue("é")).Take(1));
    }

    [Fact]
    public void NonAsciiKeysUseOrdinalUtf16JcsCompatibleOrder()
    {
        var value = Object(("é", Integer(1)), ("a", Integer(2)), ("😀", Integer(3)));
        AssertBytes(value, "{\"a\":2,\"é\":1,\"😀\":3}");
    }

    [Fact]
    public void ArraysPreserveSuppliedOrderAndAreNeverSorted()
    {
        var first = Array(Integer(2), Integer(1), Integer(3));
        var second = Array(Integer(1), Integer(2), Integer(3));
        AssertBytes(first, "[2,1,3]");
        Assert.NotEqual(CanonicalJson.SerializeToUtf8(first), CanonicalJson.SerializeToUtf8(second));
        Assert.NotEqual(CanonicalSha256.Compute(first).Hexadecimal, CanonicalSha256.Compute(second).Hexadecimal);
    }

    [Fact]
    public void NestedArraysAndObjectsPreserveTheirOwnOrderingContracts()
    {
        var value = Array(Object(("z", Integer(1)), ("a", Integer(2))), Array(Integer(2), Integer(1)));
        AssertBytes(value, "[{\"a\":2,\"z\":1},[2,1]]");
    }

    [Fact]
    public void AbsenceAndExplicitNullProduceDifferentBytes()
    {
        AssertBytes(Object(), "{}");
        AssertBytes(Object(("value", CanonicalValue.Null.Value)), "{\"value\":null}");
    }

    [Fact]
    public void SerializationIsCultureIndependentAndHasNoBomOrNewline()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            var bytes = CanonicalJson.SerializeToUtf8(Object(("n", Integer(-1234))));
            Assert.Equal("{\"n\":-1234}", Encoding.UTF8.GetString(bytes));
            Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
            Assert.NotEqual((byte)'\n', bytes[^1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void InvalidUnicodeTextFailsExplicitly()
    {
        Assert.Throws<ArgumentException>(() => new CanonicalValue.StringValue("\uD800"));
        Assert.Throws<ArgumentException>(() => Object(("\uDC00", Integer(1))));
    }

    [Fact]
    public void FloatingPointProjectionIsNotAvailable()
    {
        var nestedTypes = typeof(CanonicalValue).GetNestedTypes(BindingFlags.Public);
        Assert.DoesNotContain(nestedTypes, type => type.Name.Contains("Double", StringComparison.Ordinal));
        Assert.DoesNotContain(nestedTypes, type => type.Name.Contains("Decimal", StringComparison.Ordinal));
        Assert.DoesNotContain(nestedTypes, type => type.Name.Contains("Float", StringComparison.Ordinal));
    }

    [Fact]
    public void Sha256UsesExactlyInspectedCanonicalBytes()
    {
        var value = Object(("a", Integer(1)));
        var result = CanonicalSha256.Compute(value);
        Assert.Equal("{\"a\":1}", Encoding.UTF8.GetString(result.Utf8Bytes.ToArray()));
        Assert.Equal("015abd7f5cc57a2dd94b7590f04ad8084273905ee33ec5cebeae62276a97f862", result.Hexadecimal);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(result.Utf8Bytes.ToArray())), result.Hexadecimal);
        Assert.Matches("^[0-9a-f]{64}$", result.Hexadecimal);
    }

    [Fact]
    public void IndependentlyKnownEmptyObjectDigestMatches()
    {
        Assert.Equal("44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a", CanonicalSha256.Compute(Object()).Hexadecimal);
    }

    [Fact]
    public void AuthoritativeMutationChangesDigestAndRepeatedHashingIsStable()
    {
        var one = Object(("value", Integer(1)));
        var two = Object(("value", Integer(2)));
        Assert.Equal(CanonicalSha256.Compute(one).Hexadecimal, CanonicalSha256.Compute(one).Hexadecimal);
        Assert.NotEqual(CanonicalSha256.Compute(one).Hexadecimal, CanonicalSha256.Compute(two).Hexadecimal);
    }

    [Fact]
    public void ObjectMembersCannotExposeOrMutateBackingStorage()
    {
        var projection = Object(("value", Integer(1)));
        var beforeBytes = CanonicalJson.SerializeToUtf8(projection);
        var beforeDigest = CanonicalSha256.Compute(projection).Hexadecimal;

        Assert.False(projection.Members is KeyValuePair<string, CanonicalValue>[]);
        var list = Assert.IsAssignableFrom<IList<KeyValuePair<string, CanonicalValue>>>(
            projection.Members);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            list[0] = new KeyValuePair<string, CanonicalValue>(
                "value",
                Integer(999)));

        Assert.Equal(beforeBytes, CanonicalJson.SerializeToUtf8(projection));
        Assert.Equal(beforeDigest, CanonicalSha256.Compute(projection).Hexadecimal);
    }

    [Fact]
    public void ArrayItemsCannotExposeOrMutateBackingStorage()
    {
        var projection = Array(Integer(1), Integer(2));
        var beforeBytes = CanonicalJson.SerializeToUtf8(projection);
        var beforeDigest = CanonicalSha256.Compute(projection).Hexadecimal;

        Assert.False(projection.Items is CanonicalValue[]);
        var list = Assert.IsAssignableFrom<IList<CanonicalValue>>(projection.Items);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = Integer(999));

        Assert.Equal(beforeBytes, CanonicalJson.SerializeToUtf8(projection));
        Assert.Equal(beforeDigest, CanonicalSha256.Compute(projection).Hexadecimal);
    }

    [Fact]
    public void DigestBytesCannotExposeOrMutateBackingStorage()
    {
        var result = CanonicalSha256.Compute(Object(("value", Integer(1))));
        var beforeBytes = result.Utf8Bytes.ToArray();
        var beforeDigest = result.Hexadecimal;

        Assert.False(result.Utf8Bytes is byte[]);
        var list = Assert.IsAssignableFrom<IList<byte>>(result.Utf8Bytes);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list[0] = 0);

        Assert.Equal(beforeBytes, result.Utf8Bytes);
        Assert.Equal(beforeDigest, result.Hexadecimal);
        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(result.Utf8Bytes.ToArray())),
            result.Hexadecimal);
    }

    [Fact]
    public void CanonicalDigestCannotBeConstructedByConsumers()
    {
        Assert.Empty(typeof(CanonicalDigest).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void ProductionAssemblyHasNoReflectionHostOrGameDependencies()
    {
        var assembly = typeof(CanonicalJson).Assembly;
        Assert.DoesNotContain(assembly.GetTypes().SelectMany(t => t.GetMethods()), method => method.Name.Contains("SerializeObject", StringComparison.Ordinal));
        var references = assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain(references, x => x.Contains("Mold", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, x => x.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    private static CanonicalValue.IntegerValue Integer(long value) => new(value);
    private static CanonicalValue.Array Array(params CanonicalValue[] values) => new(values);
    private static CanonicalValue.ObjectValue Object(params (string Name, CanonicalValue Value)[] members) =>
        new(members.Select(member => new KeyValuePair<string, CanonicalValue>(member.Name, member.Value)));

    private static void AssertBytes(CanonicalValue value, string expected)
    {
        var bytes = CanonicalJson.SerializeToUtf8(value);
        Assert.Equal(expected, Encoding.UTF8.GetString(bytes));
        Assert.Equal(Encoding.UTF8.GetBytes(expected), bytes);
    }
}