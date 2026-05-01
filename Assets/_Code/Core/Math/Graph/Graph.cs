using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Graph
{
    Dictionary<int, GraphNode> nodes;

    int idCounter = 0;

    public Dictionary<int, GraphNode> Nodes
    {
        get { return new(nodes); }
    }

    public Graph()
    {
        nodes = new Dictionary<int, GraphNode>(256);
    }

    public Graph(Dictionary<int, GraphNode> nodes)
    {
        this.nodes = new Dictionary<int, GraphNode>(nodes);
    }

    public GraphNode Add(RoadSection roadComponent)
    {
        if (roadComponent == null) return null;
        GraphNode node = new(roadComponent, ++idCounter);
        nodes[node.Id] = node;

        return node;
    }

    public void Remove(GraphNode node)
    {
        if (node == null) return;
        var nexts = node.NextNodes;
        var prevs = node.PrevNodes;

        foreach (var next in nexts)
        {
            if (nodes.ContainsKey(next))
            {
                nodes[next].RemovePrevById(node.Id);
            }
        }
        foreach (var pre in prevs)
        {
            if (nodes.ContainsKey(pre))
            {
                nodes[pre].RemoveNextById(node.Id);
            }
        }
        nodes.Remove(node.Id);
    }

    public void RemoveById(int id)
    {
        if (nodes.TryGetValue(id, out var node))
        {
            Remove(node);
        }
    }

    public void AddConnection(GraphNode from, GraphNode to)
    {
        if (from == null || to == null) return;
        if (!nodes.ContainsKey(from.Id) || !nodes.ContainsKey(to.Id)) return;

        from.AddConnect(to);
    }

    public List<GraphNode> FindPath(int idFrom, int idTo)
    {
        if (!nodes.ContainsKey(idFrom) || !nodes.ContainsKey(idTo))
            return null;

        Dictionary<int, float> distances = new Dictionary<int, float>();
        Dictionary<int, int> previous = new Dictionary<int, int>();

        var priorityQueue = new SortedSet<(float distance, int nodeId)>();

        foreach (var node in nodes.Values)
        {
            distances[node.Id] = float.MaxValue;
            previous[node.Id] = -1;
        }

        distances[idFrom] = 0;
        priorityQueue.Add((0, idFrom));

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Min;
            priorityQueue.Remove(current);

            float currentDist = current.distance;
            int currentId = current.nodeId;

            if (currentId == idTo)
                break;

            if (currentDist > distances[currentId])
                continue;

            GraphNode currentNode = nodes[currentId];

            foreach (int nextId in currentNode.NextNodes)
            {
                if (!nodes.ContainsKey(nextId))
                {
                    Debug.LogError("Graph is incorrect");
                    continue;
                }

                GraphNode nextNode = nodes[nextId];

                float edgeLength = nextNode.Road?.Curve?.Length ?? 1f;
                float newDist = currentDist + edgeLength;

                if (newDist < distances[nextId])
                {
                    distances[nextId] = newDist;
                    previous[nextId] = currentId;
                    priorityQueue.Add((newDist, nextId));
                }
            }
        }

        if (distances[idTo] == float.MaxValue)
            return null; 

        List<GraphNode> path = new List<GraphNode>();
        int currentPathId = idTo;

        while (currentPathId != -1)
        {
            path.Add(nodes[currentPathId]);
            currentPathId = previous[currentPathId];
        }

        path.Reverse();
        return path;
    }

    private List<GraphNode> FindPathWithBlocklists(int idFrom, int idTo,
        HashSet<(int from, int to)> blockedEdges = null,
        HashSet<int> blockedVertices = null)
    {
        if (!nodes.ContainsKey(idFrom) || !nodes.ContainsKey(idTo))
            return null;

        blockedEdges ??= new HashSet<(int, int)>();
        blockedVertices ??= new HashSet<int>();

        // Если старт или финиш заблокированы — пути нет
        if (blockedVertices.Contains(idFrom) || blockedVertices.Contains(idTo))
            return null;

        var distances = new Dictionary<int, float>();
        var previous = new Dictionary<int, int>();
        var priorityQueue = new SortedSet<(float dist, int nodeId)>();

        foreach (var node in nodes.Values)
        {
            distances[node.Id] = float.MaxValue;
            previous[node.Id] = -1;
        }

        distances[idFrom] = 0;
        priorityQueue.Add((0, idFrom));

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Min;
            priorityQueue.Remove(current);

            float currentDist = current.dist;
            int currentId = current.nodeId;

            if (currentId == idTo)
                break;

            if (currentDist > distances[currentId])
                continue;

            GraphNode currentNode = nodes[currentId];

            foreach (int nextId in currentNode.NextNodes)
            {
                // Проверяем блокировку ребра
                if (blockedEdges.Contains((currentId, nextId)))
                    continue;

                // Проверяем блокировку вершины
                if (blockedVertices.Contains(nextId))
                    continue;

                if (!nodes.ContainsKey(nextId))
                    continue;

                GraphNode nextNode = nodes[nextId];
                float edgeLength = nextNode.Road?.Curve?.Length ?? 1f;
                float newDist = currentDist + edgeLength;

                if (newDist < distances[nextId])
                {
                    distances[nextId] = newDist;
                    previous[nextId] = currentId;
                    priorityQueue.Add((newDist, nextId));
                }
            }
        }

        if (distances[idTo] == float.MaxValue)
            return null;

        // Восстановление пути
        var path = new List<GraphNode>();
        int cur = idTo;
        while (cur != -1)
        {
            path.Add(nodes[cur]);
            cur = previous[cur];
        }
        path.Reverse();
        return path;
    }

    public List<List<GraphNode>> FindKShortestPaths(int startId, int endId, int k)
    {
        if (k <= 0) return new List<List<GraphNode>>();

        var shortestPaths = new List<List<GraphNode>>();

        var firstPath = FindPathWithBlocklists(startId, endId);
        if (firstPath == null) return shortestPaths;

        shortestPaths.Add(firstPath);

        var candidates = new SortedSet<(float cost, List<GraphNode> path, int pathIndex)>(
        Comparer<(float cost, List<GraphNode> path, int pathIndex)>.Create((a, b) =>
        {
            int cmp = a.cost.CompareTo(b.cost);
            if (cmp != 0) return cmp;
            return a.pathIndex.CompareTo(b.pathIndex);
        }));

        int globalIndex = 0; // для уникальности в SortedSet

        // Для i от 0 до K-1 генерируем следующие пути
        for (int i = 0; i < k - 1; i++)
        {
            List<GraphNode> currentPath = shortestPaths[i];
            int pathLength = currentPath.Count;

            // Для каждой вершины-кандидата на отклонение (кроме последней)
            for (int spurIndex = 0; spurIndex < pathLength - 1; spurIndex++)
            {
                // Корневая часть пути от старта до spur-вершины
                var rootPath = currentPath.Take(spurIndex + 1).ToList();
                int spurNodeId = rootPath.Last().Id;

                // !!!Формируем блокировки
                var blockedEdges = new HashSet<(int from, int to)>();
                var blockedVertices = new HashSet<int>();

                // Запрещаем все вершины из rootPath, кроме spur-вершины (чтобы не возвращаться)
                for (int j = 0; j < rootPath.Count - 1; j++)
                {
                    blockedVertices.Add(rootPath[j].Id);
                }

                // Запрещаем рёбра, которые уже использовались в предыдущих найденных путях,
                // имеющих такой же rootPath
                foreach (var otherPath in shortestPaths)
                {
                    if (otherPath.Count > spurIndex &&
                        otherPath.Take(spurIndex + 1).SequenceEqual(rootPath, new NodeIdComparer()))
                    {
                        // Совпадает rootPath, запрещаем следующее ребро в этом пути
                        int nextIdInOther = otherPath[spurIndex + 1].Id;
                        blockedEdges.Add((spurNodeId, nextIdInOther));
                    }
                }

                // Ищем spur-путь от spur-вершины до endId с учётом блокировок
                var spurPath = FindPathWithBlocklists(spurNodeId, endId, blockedEdges, blockedVertices);
                if (spurPath == null) continue;

                // Склеиваем rootPath (без последней вершины, т.к. она дублируется в spurPath)
                var totalPath = new List<GraphNode>(rootPath);
                totalPath.AddRange(spurPath.Skip(1));

                // Вычисляем стоимость (можно пересчитать или взять из расстояний, но проще пробежать)
                float totalCost = 0;
                for (int idx = 0; idx < totalPath.Count - 1; idx++)
                {
                    var fromNode = totalPath[idx];
                    var toNode = totalPath[idx + 1];
                    totalCost += toNode.Road?.Curve?.Length ?? 1f;
                }

                // Добавляем в кандидаты, если такого пути ещё нет
                if (!candidates.Any(c => c.path.SequenceEqual(totalPath, new NodeIdComparer())))
                {
                    candidates.Add((totalCost, totalPath, globalIndex++));
                }
            }

            // Если кандидатов нет — дальнейшие пути невозможны
            if (candidates.Count == 0) break;

            // Берём наилучший кандидат и делаем его следующим кратчайшим путём
            var best = candidates.Min;
            candidates.Remove(best);
            shortestPaths.Add(best.path);
        }

        return shortestPaths;
    }

    public override string ToString()
    {
        StringBuilder res = new StringBuilder();
        res.AppendLine($"В графе {nodes.Count} ущлов:");
        foreach (var node in nodes)
        {
            res.Append($"[{node.Value.Id} | {string.Join(',', node.Value.NextNodes)} | {string.Join(',', node.Value.PrevNodes)}] " );
        }

        return res.ToString();
    }

    // Вспомогательный компаратор для сравнения путей по ID узлов
    private class NodeIdComparer : IEqualityComparer<GraphNode>
    {
        public bool Equals(GraphNode x, GraphNode y) => x?.Id == y?.Id;
        public int GetHashCode(GraphNode obj) => obj.Id.GetHashCode();
    }

}

