using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimulationCarManager : MonoBehaviour
{
    public int CarsPassedLast { get; private set; }
    public bool IsCurrentlySimulating { get; private set; }
    public float SimualtionCoef { get; private set; }

    [SerializeField]
    GameObject carPrefab;
    [SerializeField]
    List<GameObject> carModelPrefabs;



    public GameObject CarPrefab { get => carPrefab; }
    public GameObject RandomCarModel { get => carModelPrefabs[Random.Range(0, carModelPrefabs.Count)]; }

    public void StartSimulating(Action callback)
    {
        if (!GetComponent<GameManager>().IsLevelFinished) return;
        if (IsCurrentlySimulating) return;
        StartCoroutine(SimulateCoroutine(callback));
    }

    IEnumerator SimulateCoroutine(Action callback)
    {
        GetComponent<LevelEditor>().SetNone();
        GetComponent<DecorationCarManager>().IsGoing = false;
        IsCurrentlySimulating = true;

        GameManager gm = GetComponent<GameManager>();

        Dictionary<(int, int), List<(List<GraphNode>, float)>> allPathes = new();
        int seed = 147;
        var paths = gm.CurrentPaths;
        int carsResult = 0;

        Random.InitState(seed);

        foreach (var indexPath in paths)
        {

            foreach (var subPath in indexPath.Value)
            {
                var kShortest = gm.FindKShortestPaths(subPath.Item1, subPath.Item2, 15);
                if (!allPathes.ContainsKey(indexPath.Key)) allPathes[indexPath.Key] = new();
                var pathWithLengths = CalcLengths(kShortest);
                allPathes[indexPath.Key].AddRange(pathWithLengths);
            }
        }

        List<(Car, int, int, List<GraphNode>)> currentCars = new();
        Dictionary<(int, int), float> gwTimers = new();

        Collider[] coll = new Collider[5];

        float duration = 20;
        float elapsed = 0;
        float timeCoef = 10;
        float timeScaled = 0;

        var allPathList = allPathes.ToList();

        while (elapsed < duration)
        {
            currentCars.RemoveAll(car => car.Item1 == null);

            float dt = Time.deltaTime * timeCoef;
            timeScaled += dt;

            var path = allPathList[Random.Range(0, allPathList.Count)];

            var startGw = gm.StartGateways.First(gw => gw.Id == path.Key.Item1);
            var endGw = gm.EndGateways.First(gw => gw.Id == path.Key.Item2);
            int layerMask = LayerMask.GetMask("car");

            if (!gwTimers.ContainsKey(path.Key)) gwTimers[path.Key] = timeScaled + 60f / startGw.Intensity;

            if (timeScaled > gwTimers[path.Key])
            {
                var isOccupied = Physics.OverlapBoxNonAlloc(
                    startGw.Curve.PointA.ToVector3XZ(), new Vector3(4.5f, 4.5f, 4.5f), coll, Quaternion.identity, layerMask) > 0;
                if (!isOccupied)
                {
                    gwTimers[path.Key] = timeScaled + (60f * Random.Range(0.85f, 1.15f) / startGw.Intensity);

                    var randomPath = GetRandomPath(path.Value);
                    var carGo = Instantiate(carPrefab);

                    var carPathData = CarPathData.GetFromRoadSections(randomPath, startGw, endGw);
                    carGo.GetComponent<Car>().Initialize(carPathData, RandomCarModel);
                    carGo.GetComponent<Car>().onPathEndCallback.AddListener(() => carsResult++);
                    currentCars.Add((carGo.GetComponent<Car>(), path.Key.Item1, path.Key.Item2, randomPath));
                }
            }

            foreach (var car in currentCars)
            {
                car.Item1.Emulate(dt);
            }

            elapsed += Time.deltaTime;
            SimualtionCoef = elapsed / duration;
            yield return null;
        }


        CarsPassedLast = carsResult;
        currentCars.ForEach(car => { if (car.Item1 != null) Destroy(car.Item1.gameObject); });
        currentCars.Clear();


        IsCurrentlySimulating = false;
        GetComponent<DecorationCarManager>().IsGoing = true;

        GetComponent<LevelEditor>().SetStraight();
        callback.Invoke();
    }

    private static List<(List<GraphNode>, float)> CalcLengths(List<List<GraphNode>> kShortest)
    {
        List<(List<GraphNode>, float)> lengthAndPath = new(kShortest.Count);
        foreach (var path in kShortest)
        {
            float len = 0f;
            foreach (var node in path)
            {
                len += node.Road.Curve.Length;
            }
            lengthAndPath.Add((path, len));
        }

        return lengthAndPath;
    }

    private static List<GraphNode> GetRandomPath(List<(List<GraphNode> path, float length)> lengthAndPath)
    {

        double totalWeight = lengthAndPath.Sum(x => 1d / x.length);
        double randomValue = Random.value * totalWeight;

        double currentWeightSum = 0;
        foreach (var item in lengthAndPath)
        {
            currentWeightSum += 1.0 / item.length;
            if (currentWeightSum >= randomValue)
            {
                return item.path;
            }
        }

        return lengthAndPath[0].path;
    }
}
