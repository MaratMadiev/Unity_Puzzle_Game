using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level Data")]
public class LevelSO : ScriptableObject
{
    [Header("Основное")]
    public int levelNumber;
    public string levelName;

    [Header("Въезды/выезды уровня (префабы)")]
    public GatewayStartStruct[] startGateways;      // 
    public GatewayFinishStruct[] finishGateways;    // 

    //[Header("Препятствия")]
    // public GameObject[] startGateways;
}
