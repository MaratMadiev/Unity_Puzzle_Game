using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    AllLevelsSO allLevelsSO;

    [SerializeField]
    GameObject mainMenu;
    [SerializeField]
    GameObject settings;
    [SerializeField]
    GameObject levels;

    [SerializeField]
    Transform levelsContainer;

    public GameObject levelButtonPrefab;

    void Start()
    {
        ShowMainMenu();
        GenerateLevels();
    }

    public void Quit()
    {
        Application.Quit();
    }

    void LoadLevel(int levelId)
    {
        LevelContext.CurrentLevel = allLevelsSO.levels.First(x => x.levelNumber == levelId);
        SceneManager.LoadScene("level");
    }

    public void ShowLevels()
    {
        mainMenu.SetActive(false);
        settings.SetActive(false);
        levels.SetActive(true);
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        settings.SetActive(false);
        levels.SetActive(false);
    }

    public void ShowSettings()
    {
        mainMenu.SetActive(false);
        settings.SetActive(true);
        levels.SetActive(false);
    }

    public void GenerateLevels()
    {
        foreach (Transform child in levelsContainer)
        {
            Destroy(child.gameObject);
        }

        var lvlsSorted = allLevelsSO.levels.OrderBy(x => x.levelNumber);

        foreach (var level in lvlsSorted)
        {
            GameObject obj = Instantiate(levelButtonPrefab, levelsContainer);
            Button button = obj.GetComponent<Button>();

            bool locked = level.levelNumber != 1 && PlayerPrefs.GetInt($"unlocked_{level.levelNumber}") == 0; 

            var text = obj.GetComponentInChildren<TMP_Text>();

            Debug.Log(level.levelNumber);
            text.text = locked ? "Locked..." : $"Level {level.levelNumber.ToString()}";
            button.interactable = !locked;
            int id = level.levelNumber;

            button.onClick.AddListener(() =>
            {
                LoadLevel(id);
            });
        }
    }

}
