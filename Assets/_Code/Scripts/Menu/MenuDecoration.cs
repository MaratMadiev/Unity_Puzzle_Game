using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuDecoration : MonoBehaviour
{
    List<CarPathData> pathMenu = new List<CarPathData>
    {
        new CarPathData(new(new(22, -7), new(8, 2)), 0, RoadSection.RoadType.Flat),
        new CarPathData(new(new(8, 2), new(3.593f, 4.833f), new(0, 1)), 0, RoadSection.RoadType.Flat),
        new CarPathData(new(new(0, 1), new(-15, -15)), 0, RoadSection.RoadType.Flat),
    };

    [SerializeField]
    Car carPrefab;
    [SerializeField]
    RoadSection roadSectionPrefab;
    [SerializeField]
    GameObject[] models;

    Car currentCar;


    private void Start()
    {
        foreach (var path in pathMenu)
        {
            var obj = Instantiate(roadSectionPrefab);
            obj.Initialize(path.curve, path.level, path.type, obj.Id);
        }
        CarInit();
    }

    private void CarInit()
    {
        currentCar = Instantiate(carPrefab);
        currentCar.Initialize(pathMenu, models[UnityEngine.Random.Range(0, models.Length)]);
        currentCar.onPathEndCallback.AddListener(CarInit);
    }

    private void Update()
    {
        if (currentCar != null) currentCar.Emulate(Time.deltaTime);
    }
}
