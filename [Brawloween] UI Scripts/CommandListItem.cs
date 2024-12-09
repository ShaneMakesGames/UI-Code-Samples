using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// This class is the visual for the individual attacks/moves in the command list
/// </summary>
public class CommandListItem : MonoBehaviour
{
    public TextMeshProUGUI displayName;
    public TextMeshProUGUI inputString;
    public TextMeshProUGUI costOrRequirementString;

    public string description;
    public Image previewImage;

    public Image backgroundImage;
    public Image deselectFilter; // Unselected list items will be slightly grayed out

    public Color selectedColor;
    public Color deselectedColor;

    public CommandListItem rootListItem; // If this move is a follow-up, it needs a reference to the root list item

    public const float ANIM_IN_TIME = 0.25f;
    public const float FAST_ANIM_OUT_TIME = 0.075f;
    public const float ANIM_OUT_TIME = 0.125f;

    public void UpdateDataFromScriptableObject(CommandListDataSO data)
    {
        displayName.text = data.displayName;
        inputString.text = data.inputString;
        costOrRequirementString.text = data.costOrRequirementString;
        if (data.previewSprite != null)
        {
            previewImage.color = Color.white;
            previewImage.sprite = data.previewSprite;

            previewImage.transform.localPosition = data.previewPos;
            if (data.previewScale == 0) previewImage.transform.localScale = Vector3.one;
            else previewImage.transform.localScale = new Vector3(data.previewScale, data.previewScale, data.previewScale);
        }
        description = data.description;
    }

    public void OnDeselect()
    {
        LeanTween.moveLocalX(backgroundImage.gameObject, -1100, ANIM_OUT_TIME);
        deselectFilter.enabled = true;
        LeanTween.scale(gameObject, Vector3.one, FAST_ANIM_OUT_TIME);
    }

    public string OnSelect()
    {
        LeanTween.moveLocalX(backgroundImage.gameObject, 0, ANIM_IN_TIME);
        deselectFilter.enabled = false;
        LeanTween.scale(gameObject, new Vector3(1.03f, 1.03f, 1.03f), ANIM_IN_TIME);

        if (rootListItem != null) rootListItem.OnSelect();

        return description;
    }
}