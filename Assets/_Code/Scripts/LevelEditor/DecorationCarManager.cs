using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(GameManager))]
[RequireComponent(typeof(SimulationCarManager))]
public class DecorationCarManager : MonoBehaviour
{
    int maxCar = 40;

    List<(Car, int, int, List<GraphNode>)> currentCars = new();
    Dictionary<int, float> gwTimers = new();
    Dictionary<(int, int), List<List<GraphNode>>> allPathes = new();

    bool isGoing = true;

    [SerializeField]
    public bool IsGoing
    {
        get
        {
            return isGoing;
        }
        set
        {
            if (!value)
            {
                foreach (var carToDel in currentCars)
                {
                    Destroy(carToDel.Item1.gameObject);
                }
                currentCars.Clear();
            }
            isGoing = value;
        }
    }



    public void RecalculateAllPaths()
    {
        allPathes.Clear();
        GameManager gm = gameObject.GetComponent<GameManager>();
        var paths = gm.CurrentPaths;

        foreach (var path in paths)
        {
            foreach (var subPath in path.Value)
            {
                var kShortest = gm.FindKShortestPaths(subPath.Item1, subPath.Item2, 10);
                if (!allPathes.ContainsKey(path.Key)) allPathes[path.Key] = new();
                allPathes[path.Key].AddRange(kShortest);
            }
        }
    }

    void Update()
    {
        if (!isGoing) return;

        Collider[] coll = new Collider[1];

        GameManager gm = gameObject.GetComponent<GameManager>();
        SimulationCarManager sim = gameObject.GetComponent<SimulationCarManager>();
        currentCars.RemoveAll(car => car.Item1 == null);
        var toDelete = currentCars.Where(car => !DoesPathStillExist(car));
        foreach (var carToDel in toDelete)
        {
            Destroy(carToDel.Item1.gameObject);
        }

        currentCars.RemoveAll(car => car.Item1 == null);

        foreach (var path in allPathes)
        {
            var startGw = gm.StartGateways.Where(gw => gw.Id == path.Key.Item1).First();
            var endGw = gm.EndGateways.Where(gw => gw.Id == path.Key.Item2).First();
            int layerMask = LayerMask.GetMask("car");

            if (!gwTimers.ContainsKey(path.Key.Item1)) gwTimers[path.Key.Item1] = Time.time + 60f / startGw.Intensity;

            if (Time.time > gwTimers[path.Key.Item1])
            {
                if (currentCars.Count == maxCar) break;

                var isOccupied = Physics.OverlapBoxNonAlloc(
                    startGw.Curve.PointA.ToVector3XZ(), new Vector3(1.5f, 1.5f, 1.5f), coll, Quaternion.identity, layerMask) > 0;
                if (isOccupied) continue;

                gwTimers[path.Key.Item1] = Time.time + (60f * UnityEngine.Random.Range(0.85f, 1.15f) / startGw.Intensity);

                var randomPath = path.Value[UnityEngine.Random.Range(0, path.Value.Count)];
                var carGo = Instantiate(sim.CarPrefab);

                var carPathData = CarPathData.GetFromRoadSections(randomPath, startGw, endGw);
                carGo.GetComponent<Car>().Initialize(carPathData, sim.RandomCarModel);
                currentCars.Add((carGo.GetComponent<Car>(), path.Key.Item1, path.Key.Item2, randomPath));
            }
        }

        foreach (var car in currentCars)
        {
            car.Item1.Emulate(Time.deltaTime * 1.0f);
        }
    }

    private bool DoesPathStillExist((Car, int, int, List<GraphNode>) car)
    {
        if (!allPathes.ContainsKey((car.Item2, car.Item3))) return false;
        if (allPathes[(car.Item2, car.Item3)].Contains(car.Item4)) return true;
        return allPathes[(car.Item2, car.Item3)].Any(path => path.SequenceEqual(car.Item4));
    }
}

