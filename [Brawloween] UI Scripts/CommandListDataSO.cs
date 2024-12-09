using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Command List Data", menuName = "CommandListDataSO")]
public class CommandListDataSO : ScriptableObject
{
    public string displayName; // Move name
    public string inputString; // Inputs to perform the move
    public string costOrRequirementString; // If the move spends or requires a unique resource

    public Sprite previewSprite; // Sprite from the move's animation, this will eventually be a video
    public Vector2 previewPos;
    public float previewScale;

    public string description; // Functionality or uses for the move

    public List<CommandListDataSO> followUps = new List<CommandListDataSO>();
}