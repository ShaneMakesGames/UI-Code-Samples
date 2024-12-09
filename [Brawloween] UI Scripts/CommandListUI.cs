using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CommandListUI : MonoBehaviour
{
    #region Singleton

    public static CommandListUI singleton;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else Destroy(this.gameObject);
    }

    #endregion

    public static bool hasFocus;
    public static bool menuAnimating;
    private bool isPopulated;

    public GameObject canvasOBJ;

    [Header("ScriptableObjects")]
    public List<CommandListDataSO> commandNormals = new List<CommandListDataSO>();
    public List<CommandListDataSO> movementAndUniques = new List<CommandListDataSO>();
    public List<CommandListDataSO> specialMoves = new List<CommandListDataSO>();

    private List<CommandListItem> allCommandListItems = new List<CommandListItem>();
    private List<CommandListItem> commandListItemsNoHeaders = new List<CommandListItem>();
    private List<CommandListItem> headerCommandListItems = new List<CommandListItem>();
    private int cursorIndex;
    private int headerCursorIndex;

    [Header("UI Elements")]
    public TextMeshProUGUI descriptionText;
    public Transform commandListItemParent;
    public GameObject scrollbarOBJ;

    [Header("Prefabs")]
    public GameObject headerPrefab;
    public GameObject listItemPrefab;
    public GameObject followUpPrefab;

    public const float MAX_Y_HEIGHT_FOR_COMMAND_LIST_ITEM = 215;
    public const float Y_PADDING_AFTER_HEADER = -100;
    public const float Y_PADDING_AFTER_LIST_ITEM = -130;

    public const float SCROLLBAR_Y_START_POS = 95;
    public const float MAX_SCROLLBAR_DISTANCE = -400;

    public void OnOpen()
    {
        if (!isPopulated) PopulateCommandListData();
        hasFocus = true;
        SetVisuals(true);
    }

    public void OnClose()
    {
        StartCoroutine(CloseMenuCoroutine());
    }

    IEnumerator CloseMenuCoroutine()
    {
        PauseUI.singleton.SetPauseVisuals(true);
        SetVisuals(false);
        hasFocus = false;
        yield return new WaitForEndOfFrame();
        PauseUI.hasFocus = true;
    }

    public void SetVisuals(bool activeState)
    {
        canvasOBJ.SetActive(activeState);
    }

    public void PopulateCommandListData()
    {
        isPopulated = true;

        TryAddCommandListItemsOfType("COMMAND NORMALS", commandNormals);
        TryAddCommandListItemsOfType("MOVEMENT AND UNIQUE MECHANICS", movementAndUniques);
        TryAddCommandListItemsOfType("SPECIAL MOVES", specialMoves);
    }

    public CommandListItem CreateListItemAtLocation(GameObject prefab, float yOffset)
    {
        GameObject obj = Instantiate(prefab, commandListItemParent);
        obj.transform.localPosition = new Vector3(0, yOffset, 0);
        CommandListItem listItem = obj.GetComponent<CommandListItem>();
        return listItem;
    }

    public CommandListItem GetMostRecentCommandListItem()
    {
        return allCommandListItems[allCommandListItems.Count - 1];
    }

    public void TryAddCommandListItemsOfType(string headerName, List<CommandListDataSO> dataSOList)
    {
        if (dataSOList.Count == 0) return;

        float yTarget;
        if (allCommandListItems.Count == 0) yTarget = MAX_Y_HEIGHT_FOR_COMMAND_LIST_ITEM;
        else yTarget = GetMostRecentCommandListItem().transform.localPosition.y + Y_PADDING_AFTER_HEADER;

        CommandListItem listItem = CreateListItemAtLocation(headerPrefab, yTarget);
        listItem.displayName.text = headerName;
        if (headerCommandListItems.Count == 0) listItem.OnSelect();
        allCommandListItems.Add(listItem);
        headerCommandListItems.Add(listItem);

        for (int i = 0; i < dataSOList.Count; i++)
        {
            CommandListDataSO dataSO = dataSOList[i];

            listItem = CreateListItemAtLocation(listItemPrefab, GetMostRecentCommandListItem().transform.localPosition.y);
            listItem.UpdateDataFromScriptableObject(dataSO);
            Vector3 targetPos = listItem.transform.localPosition;
            if (allCommandListItems.Count == 1) descriptionText.text = listItem.OnSelect(); // If it's the very first in the list, select it by default

            if (i == 0) targetPos.y += Y_PADDING_AFTER_HEADER;
            else targetPos.y += Y_PADDING_AFTER_LIST_ITEM;
            listItem.transform.localPosition = targetPos;
            allCommandListItems.Add(listItem);
            commandListItemsNoHeaders.Add(listItem);

            if (dataSO.followUps.Count == 0) continue;

            // Follow-ups require a different set-up bc they need to be visually differentiated
            for (int j = 0; j < dataSO.followUps.Count; j++)
            {
                CommandListItem followUplistItem = CreateListItemAtLocation(followUpPrefab, GetMostRecentCommandListItem().transform.localPosition.y);
                targetPos = followUplistItem.transform.localPosition;
                targetPos.x = 25;
                targetPos.y += Y_PADDING_AFTER_LIST_ITEM;
                followUplistItem.transform.localPosition = targetPos;

                followUplistItem.UpdateDataFromScriptableObject(dataSO.followUps[j]);
                followUplistItem.OnDeselect();
                followUplistItem.rootListItem = listItem;
                allCommandListItems.Add(followUplistItem);
                commandListItemsNoHeaders.Add(followUplistItem);
            }
        }
    }

    public void Update()
    {
        if (!hasFocus) return;

        CheckForCommandListInput();
    }

    public void CheckForCommandListInput()
    {
        if (GameManager.activePlayer.GetPauseInput())
        {
            OnClose();
            return;
        }
        if (GameManager.activePlayer.GetCancelInput())
        {
            OnClose();
            return;
        }
        if (GameManager.activePlayer.GetUpOrDownInput(InputManager.downDirectionalInputs))
        {
            MoveCursor(1);
            return;
        }
        if (GameManager.activePlayer.GetUpOrDownInput(InputManager.upDirectionalInputs))
        {
            MoveCursor(-1);
            return;
        }
    }

    public void MoveCursor(int amount)
    {
        CommandListItem listItem = commandListItemsNoHeaders[cursorIndex];
        CommandListItem rootItem = listItem.rootListItem;
        listItem.OnDeselect();

        cursorIndex += amount;
        if (cursorIndex < 0) cursorIndex = commandListItemsNoHeaders.Count - 1;
        if (cursorIndex > commandListItemsNoHeaders.Count - 1) cursorIndex = 0;

        AdjustParentOffset();

        listItem = commandListItemsNoHeaders[cursorIndex];
        listItem.OnSelect();
        descriptionText.text = listItem.description;
        
        if (rootItem != null && listItem.rootListItem != rootItem) // Root list item should no longer be highlighted
        {
            rootItem.OnDeselect();
        }
        
        TryChangeHeaderHighlight();
    }

    private void AdjustParentOffset()
    {
        // If going offscreen, moves the parent object so the selected list item will be visible
        Vector3 targetPos;
        int distanceFromNeutral = cursorIndex - 3;
        if (distanceFromNeutral <= 0) targetPos = new Vector3(-250, 0, 0);
        else targetPos = new(-250, distanceFromNeutral * -Y_PADDING_AFTER_LIST_ITEM, 0);
        commandListItemParent.transform.localPosition = targetPos;

        // Determines scrollbar positioning based on the parent offset 
        float maxYPos = -Y_PADDING_AFTER_LIST_ITEM * (commandListItemsNoHeaders.Count - 4);
        float percent = targetPos.y / maxYPos;
        int scrollPos = (int)SCROLLBAR_Y_START_POS + (int)(MAX_SCROLLBAR_DISTANCE * percent);
        scrollbarOBJ.transform.localPosition = new Vector3(330, scrollPos, 0);
    }

    /// <summary>
    /// Highlights the associated header based on what type of list item is selected
    /// </summary>
    private void TryChangeHeaderHighlight()
    {
        if (cursorIndex < commandNormals.Count)
        {
            if (headerCursorIndex == 0) return;
            UpdateHeaderHighlights(0);
        }
        if (cursorIndex >= commandNormals.Count && cursorIndex < commandNormals.Count + movementAndUniques.Count)
        {
            if (headerCursorIndex == 1) return;
            UpdateHeaderHighlights(1);
        }
        else if (cursorIndex >= commandNormals.Count + movementAndUniques.Count)
        {
            if (headerCursorIndex == 2) return;
            UpdateHeaderHighlights(2);
        }
    }

    public void UpdateHeaderHighlights(int newCursorIndex)
    {
        CommandListItem listItem = headerCommandListItems[headerCursorIndex];
        listItem.OnDeselect();

        headerCursorIndex = newCursorIndex;
        listItem = headerCommandListItems[headerCursorIndex];
        listItem.OnSelect();
    }
}