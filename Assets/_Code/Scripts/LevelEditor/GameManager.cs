using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LevelEditor), typeof(DecorationCarManager))]
public class GameManager : MonoBehaviour
{
    [SerializeField]
    int from = 0;
    [SerializeField]
    int to = 0;
    [SerializeField]
    LevelSO defaultLevel;
    [SerializeField]
    GameObject gatewayPrefab;

    Graph graph = null;

    List<Gateway> startGateways = null;
    List<Gateway> finishGateways = null;

    Dictionary<(int, int), List<(int, int)>> currentPaths;
    bool isLevelFinished = false;

    public IReadOnlyDictionary<(int, int), List<(int, int)>> CurrentPaths => currentPaths;
    public IReadOnlyList<Gateway> StartGateways => startGateways;
    public IReadOnlyList<Gateway> EndGateways => finishGateways;

    private Dictionary<int, int> roadIdToStartGateway;
    private Dictionary<int, int> roadIdToFinishGateway;

    private Dictionary<int, List<int>> startGwToFinishGw;

    public bool IsLevelFinished { get => isLevelFinished; private set => isLevelFinished = value; }

    public IReadOnlyDictionary<int, GraphNode> Nodes
    {
        get
        {
            if (graph == null) return null;
            return graph.Nodes;
        }
    }

    [ContextMenu("Дебаг граф")]
    void LogGraph()
    {
        Debug.Log(graph);
    }

    [ContextMenu("Путь.")]
    void FindPath()
    {
        var path = graph.FindPath(from, to);
        if (path == null)
        {
            Debug.Log("No way");
            return;
        }

        Debug.Log(string.Join("->", path));

        var pathes = graph.FindKShortestPaths(from, to, 10);

        foreach (var p in pathes) Debug.Log(string.Join("->", p));
    }

    public List<List<GraphNode>> FindKShortestPaths(int from, int to, int k)
    {
        return graph.FindKShortestPaths(from, to, k);
    }

    public List<GraphNode> FindPath(int from, int to)
    {
        return graph.FindPath(from, to);
    }


    void Awake()
    {
        LoadLevel();
        currentPaths = new();
    }

    private void LoadLevel()
    {
        // TODO: сделать сохранение всего уровня
        graph = new Graph();
        startGateways = new List<Gateway>();
        finishGateways = new List<Gateway>();

        LevelSO curLvl = LevelContext.CurrentLevel;

        if (curLvl == null)
        {
            Debug.Log("No level");
            curLvl = defaultLevel;
        }


        roadIdToStartGateway = new();
        roadIdToFinishGateway = new();
        startGwToFinishGw = new(); 

        foreach (var start in curLvl.startGateways)
        {
            var go = Instantiate(gatewayPrefab);
            go.GetComponent<Gateway>().Initialize(start.gatewayId, GatewayType.Start, new(start.roadStart, start.roadfinish), start.intensity);
            startGateways.Add(go.GetComponent<Gateway>());

            foreach (var finish in start.targetGatewayIds)
            {
                if (!startGwToFinishGw.ContainsKey(start.gatewayId)) startGwToFinishGw.Add(start.gatewayId, new());
                startGwToFinishGw[start.gatewayId].Add(finish);
            }
        }

        foreach (var finish in curLvl.finishGateways)
        {
            var go = Instantiate(gatewayPrefab);
            go.GetComponent<Gateway>().Initialize(finish.gatewayId, GatewayType.Finish, new(finish.roadStart, finish.roadfinish), -1);
            finishGateways.Add(go.GetComponent<Gateway>());
        }

    }

    public GraphNode Add(RoadSection roadComponent)
    {
        var graphNode = graph.Add(roadComponent);


        return graphNode;
    }

    public void RemoveById(int id)
    {
        if (graph.Nodes.TryGetValue(id, out var node))
        {
            Destroy(node.Road.gameObject);
            graph.Remove(node);
        }
    }

    public void AddConnection(int from, int to)
    {
        if (!Nodes.ContainsKey(from) || !Nodes.ContainsKey(to)) return;

        graph.AddConnection(Nodes[from], Nodes[to]);
    }

    void FindPaths()
    {
        currentPaths.Clear();

        if (roadIdToStartGateway.Keys.Count == 0 || roadIdToFinishGateway.Keys.Count == 0)
        {
            isLevelFinished = false;
            return;
        }

        foreach (var keyValPair in roadIdToStartGateway)
        {
            var roadStartId = keyValPair.Key;
            var startGwId = keyValPair.Value;
            var roadFinishIds = roadIdToFinishGateway.Where(roadToFinish => startGwToFinishGw[startGwId].Contains(roadToFinish.Value)).Select(p => p.Key).ToList();
            foreach (var roadFinishId in roadFinishIds)
            {
                var path = graph.FindPath(roadStartId, roadFinishId);
                if (path != null)
                {
                    if (!currentPaths.ContainsKey((roadIdToStartGateway[roadStartId], roadIdToFinishGateway[roadFinishId])))
                        currentPaths[(roadIdToStartGateway[roadStartId], roadIdToFinishGateway[roadFinishId])] = new();
                    currentPaths[(roadIdToStartGateway[roadStartId], roadIdToFinishGateway[roadFinishId])].Add((roadStartId, roadFinishId));
                }
            }
        }
        
    }

    public void UpdateGatewayRoads(SnapPoints snapPoints)
    {
        foreach (var startGw in startGateways)
        {
            var key = SnapPoints.SnapPointKey.GetKeyFromGateway(startGw);
            var outcomings = snapPoints.Dict[key].OutcomingRoads;

            foreach (var road in outcomings)
            {
                roadIdToStartGateway[road] = startGw.Id;
                
            }
        }
        foreach (var finishGw in finishGateways)
        {
            var key = SnapPoints.SnapPointKey.GetKeyFromGateway(finishGw);
            var incomings = snapPoints.Dict[key].IncomingRoads;

            foreach (var road in incomings)
            {
                roadIdToFinishGateway[road] = finishGw.Id;
            }
        }

    }

    public void OnChange()
    {
        FindPaths();
        CheckIfLevelPassed();
        GetComponent<DecorationCarManager>().RecalculateAllPaths();
    }

    private void CheckIfLevelPassed()
    {
        foreach (var gwPair in startGwToFinishGw)
        {
            var startGw = gwPair.Key;
            foreach (var endGw in gwPair.Value)
            {
                if (!currentPaths.ContainsKey((startGw, endGw)))
                {
                    IsLevelFinished = false;
                    return;
                } 
            }
        }

        IsLevelFinished = true;
        Debug.Log("Level completed...");
        return;
    }
}



