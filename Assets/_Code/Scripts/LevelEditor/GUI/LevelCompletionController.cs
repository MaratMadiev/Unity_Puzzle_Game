using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompletionController : MonoBehaviour
{
    [SerializeField]
    TMP_Text header;
    [SerializeField]
    TMP_Text cars;
    [SerializeField]
    TMP_Text length;
    [SerializeField]
    TMP_Text total;

    public void OnSimulationEnd(LevelResult res)
    {
        foreach (Transform obj in header.transform.parent.parent) obj.gameObject.SetActive(false); 
        // выключаем все меню
        header.transform.parent.gameObject.SetActive(true);

        var savedResult = PlayerPrefs.GetInt($"result_{res.currentNumber}");

        header.text = res.IsPassed ? "Level Passed" : "Level Failed";
        cars.text = res.cars.ToString();
        length.text = res.length.ToString();
        total.text = savedResult < res.total ? $"{res.total} - New best score!": $"{res.total} (Best score is {savedResult})";
    }

    public void GoBackToMenu()
    {
        SceneManager.LoadScene("menu");
    }
}


