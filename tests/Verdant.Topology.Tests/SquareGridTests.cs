using System.Reflection;
using Verdant.Topology;
using Xunit;

namespace Verdant.Topology.Tests;

public sealed class SquareGridTests
{
    [Fact]
    public void ConstructionPreservesExplicitDimensions()
    {
        var grid = new SquareGrid(3, 5);

        Assert.Equal(3, grid.Width);
        Assert.Equal(5, grid.Height);
        Assert.True(grid.Contains(new TopologyNode(0, 0)));
        Assert.True(grid.Contains(new TopologyNode(2, 4)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void InvalidDimensionsFailExplicitly(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SquareGrid(width, height));
    }

    [Fact]
    public void MinimumGridHasCanonicalIdentityAndNoNeighbors()
    {
        var grid = new SquareGrid(1, 1);
        var node = new TopologyNode(0, 0);

        Assert.Equal(new TopologyNode(0, 0), node);
        Assert.Empty(AssertSuccess(grid.GetNeighbors(node)));
        Assert.Equal(0, AssertSuccess(grid.Distance(node, node)));
    }

    [Fact]
    public void CornerNeighborsUseCanonicalNorthEastSouthWestFiltering()
    {
        var grid = new SquareGrid(4, 4);

        Assert.Equal(
            [new TopologyNode(1, 0), new TopologyNode(0, 1)],
            AssertSuccess(grid.GetNeighbors(new TopologyNode(0, 0))));
    }

    [Fact]
    public void EdgeNeighborsPreserveCanonicalOrder()
    {
        var grid = new SquareGrid(4, 4);

        Assert.Equal(
            [
                new TopologyNode(2, 0),
                new TopologyNode(1, 1),
                new TopologyNode(0, 0)
            ],
            AssertSuccess(grid.GetNeighbors(new TopologyNode(1, 0))));
    }

    [Fact]
    public void InteriorNeighborsAreNorthEastSouthWest()
    {
        var grid = new SquareGrid(4, 4);
        var node = new TopologyNode(1, 1);
        TopologyNode[] expected =
        [
            new(1, 0),
            new(2, 1),
            new(1, 2),
            new(0, 1)
        ];

        Assert.Equal(expected, AssertSuccess(grid.GetNeighbors(node)));
        Assert.Equal(expected, AssertSuccess(grid.GetNeighbors(node)));
        Assert.Equal(expected, AssertSuccess(grid.GetNeighbors(node)));
    }

    [Fact]
    public void DistanceIsOrthogonalSymmetricAndZeroForIdentity()
    {
        var grid = new SquareGrid(8, 9);
        var first = new TopologyNode(0, 0);
        var second = new TopologyNode(6, 7);

        Assert.Equal(13, AssertSuccess(grid.Distance(first, second)));
        Assert.Equal(13, AssertSuccess(grid.Distance(second, first)));
        Assert.Equal(0, AssertSuccess(grid.Distance(first, first)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 4)]
    public void InvalidNeighborQueriesReturnTypedFailure(int x, int y)
    {
        var node = new TopologyNode(x, y);
        var failure = AssertFailure(new SquareGrid(4, 4).GetNeighbors(node));

        Assert.Equal(TopologyQueryErrorCode.InvalidNode, failure.Code);
        Assert.Equal(node, failure.Node);
        Assert.Null(failure.Limit);
    }

    [Fact]
    public void InvalidDistanceReportsTheExactInvalidNodeWithoutClamping()
    {
        var grid = new SquareGrid(4, 4);
        var invalid = new TopologyNode(4, 3);

        var failure = AssertFailure(
            grid.Distance(new TopologyNode(0, 0), invalid));

        Assert.Equal(TopologyQueryErrorCode.InvalidNode, failure.Code);
        Assert.Equal(invalid, failure.Node);
        Assert.Null(failure.Limit);
    }

    [Fact]
    public void LargerGridBoundariesRemainUnambiguous()
    {
        var grid = new SquareGrid(1000, 2000);
        var last = new TopologyNode(999, 1999);

        Assert.True(grid.Contains(last));
        Assert.False(grid.Contains(new TopologyNode(1000, 1999)));
        Assert.False(grid.Contains(new TopologyNode(999, 2000)));
        Assert.Equal(
            [new TopologyNode(999, 1998), new TopologyNode(998, 1999)],
            AssertSuccess(grid.GetNeighbors(last)));
        Assert.Equal(
            2998,
            AssertSuccess(grid.Distance(new TopologyNode(0, 0), last)));
    }

    [Fact]
    public void TraversalAtExactAllowanceReturnsCompleteCanonicalResult()
    {
        var grid = new SquareGrid(2, 2);

        Assert.Equal(
            [
                new TopologyNode(0, 0),
                new TopologyNode(1, 0),
                new TopologyNode(0, 1),
                new TopologyNode(1, 1)
            ],
            AssertSuccess(grid.Traverse(new TopologyNode(0, 0), 4)));
    }

    [Fact]
    public void TraversalBeyondAllowanceFailsTypedWithoutTruncatedSuccess()
    {
        var grid = new SquareGrid(2, 2);

        var failure = AssertFailure(
            grid.Traverse(new TopologyNode(0, 0), 3));

        Assert.Equal(
            TopologyQueryErrorCode.DeterministicGuardExceeded,
            failure.Code);
        Assert.Null(failure.Node);
        Assert.Equal(3, failure.Limit);
    }

    [Fact]
    public void InvalidTraversalStartFailsBeforeGuardEvaluation()
    {
        var grid = new SquareGrid(2, 2);
        var invalid = new TopologyNode(2, 0);

        var failure = AssertFailure(grid.Traverse(invalid, 1));

        Assert.Equal(TopologyQueryErrorCode.InvalidNode, failure.Code);
        Assert.Equal(invalid, failure.Node);
        Assert.Null(failure.Limit);
    }

    [Fact]
    public void QueriesDoNotMutateTopologyState()
    {
        var grid = new SquareGrid(3, 3);
        var before = (grid.Width, grid.Height);

        _ = grid.GetNeighbors(new TopologyNode(1, 1));
        _ = grid.Distance(new TopologyNode(0, 0), new TopologyNode(2, 2));
        _ = grid.Traverse(new TopologyNode(0, 0), 9);

        Assert.Equal(before, (grid.Width, grid.Height));
        Assert.Equal(
            [new TopologyNode(1, 0), new TopologyNode(0, 1)],
            AssertSuccess(grid.GetNeighbors(new TopologyNode(0, 0))));
    }

    [Fact]
    public void ProductionTopologyHasNoHostPresentationStorageClockOrRandomDependencies()
    {
        var referencedAssemblies = typeof(SquareGrid)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToArray();

        Assert.DoesNotContain(referencedAssemblies, name =>
            name!.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedAssemblies, name =>
            name!.Contains("JSInterop", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedAssemblies, name =>
            name!.Contains("Mold", StringComparison.OrdinalIgnoreCase));

        var publicMembers = typeof(SquareGrid).Assembly
            .GetExportedTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Select(member => member.ToString() ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(publicMembers, member =>
            member.Contains("Random", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMembers, member =>
            member.Contains("Clock", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMembers, member =>
            member.Contains("Storage", StringComparison.OrdinalIgnoreCase));
    }

    private static T AssertSuccess<T>(TopologyQueryResult<T> result) =>
        Assert.IsType<TopologyQueryResult<T>.Success>(result).Value;

    private static TopologyQueryResult<T>.Failure AssertFailure<T>(
        TopologyQueryResult<T> result) =>
        Assert.IsType<TopologyQueryResult<T>.Failure>(result);
}