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
    public RawImage hpSliderImage;
    [FormerlySerializedAs("pkm")] public Pokemon pokemon;
    [FormerlySerializedAs("main_ui")] public GameObject[] mainUI;
    [FormerlySerializedAs("empty_ui")] public GameObject emptySlotUI;
    [FormerlySerializedAs("HeldItem_img")] public GameObject heldItemImage;
    public bool isEmpty = false;

    public void Levelup()//debugging purposes
    {
        if(pokemon==null)return;
        var exp = PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel, pokemon.expGroup)+1;
        pokemon.ReceiveExperience(exp);
        pokemon.hp=pokemon.maxHp;
    }
    public void ActivateUI()
    {
        pokemonFrontImage.sprite = pokemon.frontPicture;
        foreach (var ui in mainUI)
            ui.SetActive(true);
        isEmpty = false;
        emptySlotUI.SetActive(false);
        heldItemImage.SetActive(pokemon.hasItem);
        if (pokemon.statusEffect == PokemonOperations.StatusEffect.None)
            statusEffectImage.gameObject.SetActive(false);
        else
        {
            statusEffectImage.gameObject.SetActive(true);
            statusEffectImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(Save_manager.AssetDirectory.Status)
                + pokemon.statusEffect.ToString().ToLower());
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
        pokemonHealthBarUI.value = pokemon.hp;
        pokemonHealthBarUI.maxValue = pokemon.maxHp;
        pokemonHealthBarUI.minValue = 0;
        pokemonLevelText.text = "Lv: " + pokemon.currentLevel;
        pokemonNameText.text = pokemon.pokemonName;
        pokemonFrontImage.color = ((pokemon.hp <= 0))? 
             Color.HSVToRGB(17, 96, 54)
            :Color.HSVToRGB(0,0,100);
    }
}
