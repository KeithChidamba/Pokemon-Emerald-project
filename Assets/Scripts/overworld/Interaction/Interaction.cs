using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Interaction", menuName = "interaction")]
public class Interaction : InfoModuleHolder
{
    private string runtimeID = "";
    
    [FormerlySerializedAs("InteractionMsg")] public string interactionMessage = "";
    public StringBuilder interactionMessageBuilder = new();
    public DialogType dialogueType;
    public bool isEventTrigger;
    public List<InteractionOptions> interactionOptions = new();
    [FormerlySerializedAs("OptionsUiText")] public List<string> optionsUiText= new();
    
    public OverworldInteractionType overworldInteraction;
    
    public List<AdditionalInfoModule> additionalInfoModules = new();
    public T GetModule<T>() where T : AdditionalInfoModule 
        => additionalInfoModules.FirstOrDefault(m => m is T) as T;
    
/// <summary>
/// give interactions a random unique ID
/// </summary>
    public void SetRuntimeID()
    {
        runtimeID = $"{Utility.Random16Bit() * 256 - interactionMessage.Length}";
    }
    public string GetID => runtimeID;
}

public enum OverworldInteractionType
{
    None,
    PokemartClerk,
    PlantBerry,
    PickBerry,
    WaterBerryTree
}