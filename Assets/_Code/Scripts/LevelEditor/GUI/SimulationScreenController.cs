using TMPro;
using UnityEngine;

public class SimulationScreenController : MonoBehaviour
{
    [SerializeField]
    SimulationCarManager simulationCarManager;
    [SerializeField]
    TMP_Text text;

    private void FixedUpdate()
    {
        text.text = $"({(int)(simulationCarManager.SimualtionCoef * 100)}% ready...)";  
    }
}
