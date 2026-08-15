namespace Verdant.Topology;

public sealed class SquareGrid : ITopology
{
    public SquareGrid(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _ = checked(width * height);
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public bool Contains(TopologyNode node) =>
        node.X >= 0 && node.X < Width &&
        node.Y >= 0 && node.Y < Height;

    public TopologyQueryResult<IReadOnlyList<TopologyNode>> GetNeighbors(
        TopologyNode node)
    {
        if (!Contains(node))
        {
            return new TopologyQueryResult<IReadOnlyList<TopologyNode>>.Failure(
                TopologyQueryErrorCode.InvalidNode,
                node);
        }

        var neighbors = new List<TopologyNode>(4);
        AddIfContained(neighbors, new TopologyNode(node.X, node.Y - 1));
        AddIfContained(neighbors, new TopologyNode(node.X + 1, node.Y));
        AddIfContained(neighbors, new TopologyNode(node.X, node.Y + 1));
        AddIfContained(neighbors, new TopologyNode(node.X - 1, node.Y));

        return new TopologyQueryResult<IReadOnlyList<TopologyNode>>.Success(
            neighbors.ToArray());
    }

    public TopologyQueryResult<int> Distance(
        TopologyNode first,
        TopologyNode second)
    {
        if (!Contains(first))
        {
            return new TopologyQueryResult<int>.Failure(
                TopologyQueryErrorCode.InvalidNode,
                first);
        }

        if (!Contains(second))
        {
            return new TopologyQueryResult<int>.Failure(
                TopologyQueryErrorCode.InvalidNode,
                second);
        }

        var xDistance = Math.Abs((long)first.X - second.X);
        var yDistance = Math.Abs((long)first.Y - second.Y);
        var distance = checked(xDistance + yDistance);

        return new TopologyQueryResult<int>.Success(checked((int)distance));
    }

    public TopologyQueryResult<IReadOnlyList<TopologyNode>> Traverse(
        TopologyNode start,
        int maxNodeVisits)
    {
        if (!Contains(start))
        {
            return new TopologyQueryResult<IReadOnlyList<TopologyNode>>.Failure(
                TopologyQueryErrorCode.InvalidNode,
                start);
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxNodeVisits);

        var visited = new HashSet<TopologyNode>();
        var queue = new Queue<TopologyNode>();
        var ordered = new List<TopologyNode>();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            if (ordered.Count == maxNodeVisits)
            {
                return new TopologyQueryResult<IReadOnlyList<TopologyNode>>.Failure(
                    TopologyQueryErrorCode.DeterministicGuardExceeded,
                    Limit: maxNodeVisits);
            }

            ordered.Add(current);

            var neighbors = GetNeighbors(current);
            var success = (TopologyQueryResult<IReadOnlyList<TopologyNode>>.Success)neighbors;
            foreach (var neighbor in success.Value)
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return new TopologyQueryResult<IReadOnlyList<TopologyNode>>.Success(
            ordered.ToArray());
    }

    private void AddIfContained(
        List<TopologyNode> target,
        TopologyNode node)
    {
        if (Contains(node))
        {
            target.Add(node);
        }
    }
}