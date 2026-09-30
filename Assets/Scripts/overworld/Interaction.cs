using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Interaction", menuName = "interaction")]
public class Interaction : ScriptableObject
{
    private string runtimeID = "";
    
    [FormerlySerializedAs("InteractionMsg")] public string interactionMessage = "";
    public DialogType dialogueType;
    public bool isEventTrigger;
    public List<InteractionOptions> interactionOptions = new();
    [FormerlySerializedAs("ResultMessage")] public string resultMessage = "";
    [FormerlySerializedAs("OptionsUiText")] public List<string> optionsUiText= new();
    public AdditionalInfoModule additionalInfo;
    public OverworldInteractionType overworldInteraction;
    public AreaName location;
    public T GetModule<T>() where T : AdditionalInfoModule
    {
        return additionalInfo as T;
    }

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
    WaterBerryTree,
    PokemonCenter,
    Battle,
    ReceiveGiftPokemon
}