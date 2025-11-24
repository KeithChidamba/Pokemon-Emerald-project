using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_party_member : MonoBehaviour
{
    public Text pokemonNameText;
    public Text pokemonLevelText;
    public Image pokemonFrontImage;
    public Image pokeballClosedImage;
    public Image pokeballOpenImage;
    public Image statusEffectImage;
    public Slider pokemonHealthBarUI;
    public RawImage hpSliderImage;
    public Pokemon pokemon;
    public GameObject[] mainUI;
    public GameObject memberSelectedImage;
    public GameObject memberNotSelectedImage;
    public GameObject emptySlotUI;
    public GameObject heldItemImage;
    private Action<Battle_Participant> _healthPhaseUpdateEvent;
    public bool isEmpty;
    private bool _isViewingCard;
    private bool _viewingParty;
    private bool _exitedPartyState;
    private Vector2 _startPos;
    private Vector2 _targetPos;
    private bool _movingToTarget = true;
    public void LevelupForTesting()//testing purposes
    {
        if(pokemon==null)return;
        var exp = 
           PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel, pokemon.expGroup)-pokemon.currentExpAmount;
        pokemon.ReceiveExperience(exp+1);
        pokemon.hp=pokemon.maxHp;
    }

    private void Start()
    {
        InputStateHandler.Instance.OnStateChanged += CheckIfViewing;
        InputStateHandler.Instance.OnStateRemoved += ResetSelectionVisual;
        
        _startPos = pokemonFrontImage.rectTransform.anchoredPosition;
        _targetPos = _startPos +Vector2.up * 10f;
    }
    private void MoveInLoop()
    {
        Vector2 target;
        if (_movingToTarget)
        {
            pokemonFrontImage.sprite = pokemon.partyFrame1;
            target = _targetPos;
        }
        else
        {
            pokemonFrontImage.sprite = pokemon.partyFrame2;
            target = _startPos;
        }
        pokemonFrontImage.rectTransform.anchoredPosition = Vector2.MoveTowards(
            pokemonFrontImage.rectTransform.anchoredPosition,
            target,
            40f * Time.deltaTime
        );

        if (Vector2.Distance(pokemonFrontImage.rectTransform.anchoredPosition, target) < 0.01f)
        {
            _movingToTarget = !_movingToTarget;
        }
        
    }
    public void ActivateUI()
    {
        _isViewingCard = true;
        _viewingParty = true;
        
        pokemonFrontImage.sprite = pokemon.partyFrame1;
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
        InputStateHandler.Instance.OnSelectionIndexChanged += UpdateUi;
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
            _viewingParty = true;
            _healthPhaseUpdateEvent = 
                (attacker) => PokemonOperations.UpdateHealthPhase(pokemon, hpSliderImage);
            pokemon.OnHealthChanged += _healthPhaseUpdateEvent;
            if(pokemon.hp<=0)
            {
                statusEffectImage.gameObject.SetActive(true);
                statusEffectImage.sprite = Resources.Load<Sprite>(
                    Save_manager.GetDirectory(Save_manager.AssetDirectory.Status)
                    + "fainted");
            }
        }
        else
        {
            _viewingParty = false;
            pokemon.OnHealthChanged -= _healthPhaseUpdateEvent;
            ChangeVisibility(false);
        }
        _isViewingCard = currentState.stateName == InputStateHandler.StateName.PokemonPartyNavigation
                         || currentState.stateName == InputStateHandler.StateName.PokemonPartyItemUsage;
        if (_isViewingCard)
        {
            InputStateHandler.Instance.OnSelectionIndexChanged += UpdateUi;
            UpdateUi(InputStateHandler.Instance.currentState.currentSelectionIndex);
        }
    }
    private void ResetSelectionVisual(InputState previousState)
    {
        ChangeVisibility(false);
    }
    public void ChangeVisibility(bool isSelected)
    {
        pokeballClosedImage.gameObject.SetActive(!isSelected);
        pokeballOpenImage.gameObject.SetActive(isSelected);
        memberSelectedImage.SetActive(isSelected);
        memberNotSelectedImage.SetActive(!isSelected);
    }
    private void UpdateUi(int currentIndex)
    {
        if (isEmpty || !_isViewingCard) return;
        if (currentIndex == Pokemon_party.Instance.numMembers)
        {
            ChangeVisibility(false);
            return;
        }
        ChangeVisibility(Pokemon_party.Instance.party[currentIndex] == pokemon);
    }
    private void Update()
    {
        if (isEmpty) return;
        if (_viewingParty) MoveInLoop();
        
        if (!_isViewingCard) return;
        pokemonHealthBarUI.value = pokemon.hp;
        pokemonHealthBarUI.maxValue = pokemon.maxHp;
        pokemonHealthBarUI.minValue = 0;
        pokemonLevelText.text = "Lv" + pokemon.currentLevel;
        pokemonNameText.text = pokemon.pokemonName;
        pokemonFrontImage.color = ((pokemon.hp <= 0))? 
            Color.HSVToRGB(17, 96, 54)
            :Color.HSVToRGB(0,0,100);
    }
}
