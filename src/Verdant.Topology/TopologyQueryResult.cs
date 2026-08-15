namespace Verdant.Topology;

public enum TopologyQueryErrorCode
{
    InvalidNode,
    DeterministicGuardExceeded
}

public abstract record TopologyQueryResult<T>
{
    private TopologyQueryResult()
    {
    }

    public sealed record Success(T Value) : TopologyQueryResult<T>;

    public sealed record Failure(
        TopologyQueryErrorCode Code,
        TopologyNode? Node = null,
        int? Limit = null) : TopologyQueryResult<T>;
}