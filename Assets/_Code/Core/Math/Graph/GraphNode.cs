using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class GraphNode
{
    private List<int> nextNodes = new List<int>();
    private List<int> prevNodes = new List<int>();

    private RoadSection road;

    private int id;

    public int Id
    {
        get { return id; }
    }

    internal IReadOnlyList<int> NextNodes
    {
        get { return nextNodes; }
    }

    internal IReadOnlyList<int> PrevNodes
    {
        get { return prevNodes; }
    }

    internal RoadSection Road
    {
        get { return road; }
    }


    public GraphNode(RoadSection road, int id)
    {
        this.road = road;
        this.id = id;
    }

    public GraphNode(RoadSection road, List<int> nextNodes, List<int> prevNodes, int id)
    {
        this.nextNodes = new(nextNodes);
        this.prevNodes = new(prevNodes);
        this.road = road;
        this.id = id;
    }

    internal void AddConnect(GraphNode to)
    {
        if (to == null) return;
        if (nextNodes.Contains(to.id)) return; 
        nextNodes.Add(to.id);
        to.prevNodes.Add(id);
    }

    internal void RemoveConnect(GraphNode to)
    {
        if (to == null) return;
        nextNodes.Remove(to.id);
        to.prevNodes.Remove(id);
    }

    internal void RemoveNextById(int id)
    {
        nextNodes.Remove(id);
    }

    internal void RemovePrevById(int id)
    {
        prevNodes.Remove(id);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;

        GraphNode other = (GraphNode)obj;
        return this.id == other.id;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public override string ToString()
    {
        return id + "";
    }

}