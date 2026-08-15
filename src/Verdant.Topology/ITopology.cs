namespace Verdant.Topology;

public interface ITopology
{
    TopologyQueryResult<IReadOnlyList<TopologyNode>> GetNeighbors(
        TopologyNode node);

    TopologyQueryResult<int> Distance(
        TopologyNode first,
        TopologyNode second);
}