using UnityEngine;

[CreateAssetMenu(fileName = "AllLevels", menuName = "Game/AllLevels")]
public class AllLevelsSO : ScriptableObject
{
    [Header("Список уровней")]
    public LevelSO[] levels;
}

