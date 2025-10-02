using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Interable_obj", menuName = "interaction")]
public class Interaction : ScriptableObject
{
    [FormerlySerializedAs("InteractionMsg")] public string interactionMessage = "";
    public Dialogue_handler.DialogType interactionType;
    [FormerlySerializedAs("InteractionOptions")] public List<string> interactionOptions = new();
    [FormerlySerializedAs("ResultMessage")] public string resultMessage = "";
    [FormerlySerializedAs("OptionsUiText")] public List<string> optionsUiText= new();
    [FormerlySerializedAs("AdditionalInfo")] public List<string> additionalInfo = new();
    public bool hasSeparateLogicHandler;
}
