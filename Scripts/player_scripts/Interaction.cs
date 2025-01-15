using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Interable_obj", menuName = "interaction")]
public class Interaction : ScriptableObject
{
    public string InteractionMsg = "";
    public string InteractionType = "";
    public List<string> InteractionOptions = new();
    public string ResultMessage = "";
    public List<string> OptionsUiText= new();
    public List<string> AdditionalInfo = new();
}
