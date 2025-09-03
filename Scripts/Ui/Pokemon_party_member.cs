using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private Action _healthPhaseUpdateEvent;
    public bool isEmpty = false;
    private bool _isViewingCard;
    public void Levelup()//testing purposes
    {
        if(pokemon==null)return;
        var exp = 
           PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel, pokemon.expGroup)-pokemon.currentExpAmount;
        pokemon.ReceiveExperience(exp);
        pokemon.hp=pokemon.maxHp;
    }

    private void Start()
    {
        InputStateHandler.Instance.OnStateChanged += CheckIfViewing;
    }

    public void ActivateUI()
    {
        _isViewingCard = true;
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

    void CheckIfViewing(InputState currentState)
    {
        if (isEmpty) return;
        if (currentState.stateGroups.Contains(InputStateHandler.StateGroup.PokemonParty))
        {
            _healthPhaseUpdateEvent = () => PokemonOperations.UpdateHealthPhase(pokemon, hpSliderImage);
            pokemon.OnHealthChanged += _healthPhaseUpdateEvent;
            _isViewingCard = true;
        }
        else
        {
            pokemon.OnHealthChanged -= _healthPhaseUpdateEvent;
            _isViewingCard = false;
        }
    }
    private void Update()
    {
        if (isEmpty || !_isViewingCard) return;
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
