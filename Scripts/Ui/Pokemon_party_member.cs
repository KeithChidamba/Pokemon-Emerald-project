using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_party_member : MonoBehaviour
{
    [FormerlySerializedAs("Pkm_name")] public Text pokemonNameText;
    [FormerlySerializedAs("Pkm_Lv")] public Text pokemonLevelText;
    [FormerlySerializedAs("Pkm_front_img")] public Image pokemonFrontImage;
    [FormerlySerializedAs("Status_img")] public Image statusEffectImage;
    [FormerlySerializedAs("pkm_hp")] public Slider pokemonHealthBarUI;
    [FormerlySerializedAs("pkm")] public Pokemon pokemon;
    public int partyPosition;
    [FormerlySerializedAs("Options")] public GameObject options;
    [FormerlySerializedAs("main_ui")] public GameObject[] mainUI;
    [FormerlySerializedAs("empty_ui")] public GameObject emptySlotUI;
    [FormerlySerializedAs("HeldItem_img")] public GameObject heldItemImage;
    [FormerlySerializedAs("TakeHeldItem_btn")] public Button takeHeldItemButton;
    public bool isEmpty = false;
    private void Start()
    {
        options.SetActive(false);
    }
    public void Levelup()//debugging purposes
    {
        if(pokemon==null)return;
        var exp = PokemonOperations.CalculateExpForNextLevel(pokemon.Current_level, pokemon.EXPGroup)+1;
        pokemon.ReceiveExperience(exp);
        pokemon.HP=pokemon.max_HP;
    }
    public void ActivateUI()
    {
        pokemonFrontImage.sprite = pokemon.front_picture;
        foreach (var ui in mainUI)
            ui.SetActive(true);
        isEmpty = false;
        emptySlotUI.SetActive(false);
        heldItemImage.SetActive(pokemon.HasItem);
        takeHeldItemButton.interactable = pokemon.HasItem;
        if (pokemon.Status_effect == "None")
            statusEffectImage.gameObject.SetActive(false);
        else
        {
            statusEffectImage.gameObject.SetActive(true);
            statusEffectImage.sprite = Resources.Load<Sprite>("Pokemon_project_assets/Pokemon_obj/Status/"
                                                       + pokemon.Status_effect.Replace(" ","").ToLower());
        }
    }
    public void ResetUI()
    {
        foreach (var ui in mainUI)
            ui.SetActive(false);
        isEmpty = true;
        pokemon = null;
        heldItemImage.gameObject.SetActive(false);
        statusEffectImage.gameObject.SetActive(false);
        emptySlotUI.SetActive(true);
    }
    private void Update()
    {
        if (isEmpty) return;
        pokemonHealthBarUI.value = pokemon.HP;
        pokemonHealthBarUI.maxValue = pokemon.max_HP;
        pokemonHealthBarUI.minValue = 0;
        pokemonLevelText.text = "Lv: " + pokemon.Current_level;
        pokemonNameText.text = pokemon.Pokemon_name;
        pokemonFrontImage.color = ((pokemon.HP <= 0))? 
             Color.HSVToRGB(17, 96, 54)
            :Color.HSVToRGB(0,0,100);
    }
}
