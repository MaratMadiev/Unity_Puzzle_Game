using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuiController : MonoBehaviour
{
    [SerializeField]
    Button straight;
    [SerializeField]
    Button curve;
    [SerializeField]
    Button delete;
    [SerializeField]
    Button slopeUp;
    [SerializeField]
    Button slopeFlat;
    [SerializeField]
    Button slopeDown;
    [SerializeField]
    Button increment;
    [SerializeField]
    Button decrement;
    [SerializeField]
    TMP_Text levelText;

    [SerializeField]
    GameManager gm;

    public void OnDrawingStateButtonsUpdate()
    {
        straight.image.color = Color.gray;
        curve.image.color = Color.gray;
        delete.image.color = Color.gray;

        var curType = gm.GetComponent<LevelEditor>().Type;

        if (curType == EditorMode.Straight) straight.image.color = Color.white;
        else if (curType == EditorMode.Curve) curve.image.color = Color.white;
        else if (curType == EditorMode.Delete) delete.image.color = Color.white;
    }

    public void OnSlopeTypeUpdate()
    {
        slopeUp.image.color = Color.gray;
        slopeDown.image.color = Color.gray;
        slopeFlat.image.color = Color.gray;

        var curType = gm.GetComponent<LevelEditor>().SlopeType;

        if (curType == RoadSection.RoadType.Upward) slopeUp.image.color = Color.white;
        else if (curType == RoadSection.RoadType.Downward) slopeDown.image.color = Color.white;
        else if (curType == RoadSection.RoadType.Flat) slopeFlat.image.color = Color.white;
    }

    public void OnLevelUpdate()
    {
        var level = gm.GetComponent<LevelEditor>().CurrentLevel;
        if (level == 0)
        {
            slopeUp.interactable = true;
            slopeDown.interactable = false;
            increment.interactable = true;
            decrement.interactable = false;
        }
        else if (level == GameRules.MaxLevel)
        {
            slopeUp.interactable = false;
            slopeDown.interactable = true;
            increment.interactable = false;
            decrement.interactable = true;
        } else
        {
            slopeUp.interactable = true;
            slopeDown.interactable = true;
            increment.interactable = true;
            decrement.interactable = true;
        }

        levelText.text = (level + 1).ToString();
    }

    private void Awake()
    {
        OnDrawingStateButtonsUpdate();
        OnLevelUpdate();
        OnSlopeTypeUpdate();
    }
}
