using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_party_member : MonoBehaviour,IInjectable
{
    public Text pokemonNameText;
    public Text pokemonLevelText;
    public Image pokemonFrontImage;
    public Image pokeballClosedImage;
    public Image pokeballOpenImage;
    public Image statusEffectImage;
    public Image genderImage;
    public Slider pokemonHealthBarUI;
    public RawImage hpSliderImage;
    public Pokemon pokemon;
    public List<GameObject> mainUI;
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
    
    private Pokemon_party _pokemonPartyHandler;
    private InputStateHandler _inputStateHandler;
    
    public void Inject(Container container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        OnInject();
    }

    private void OnInject()
    {
        _inputStateHandler.OnStateChanged += CheckIfViewing;
        _inputStateHandler.OnStateRemoved += ResetSelectionVisual;
        
        _startPos = pokemonFrontImage.rectTransform.anchoredPosition;
        _targetPos = _startPos +Vector2.up * 10f;
    }
    
    public void LevelupForTesting()//testing purposes
    {
        if(pokemon==null)return;
        var exp = 
           PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel, pokemon.expGroup)-pokemon.currentExpAmount;
        pokemon.ReceiveExperience(exp+1);
        pokemon.hp=pokemon.maxHp;
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
        
        genderImage.sprite = Resources.Load<Sprite>(
            Save_manager.GetDirectory(AssetDirectory.UI) 
            + pokemon.gender.ToString().ToLower());
        
        if (pokemon.statusEffect == StatusEffect.None)
            statusEffectImage.gameObject.SetActive(false);
        else
        {
            statusEffectImage.gameObject.SetActive(true);
            statusEffectImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(AssetDirectory.Status)
                + pokemon.statusEffect.ToString().ToLower());
        }
        _inputStateHandler.OnSelectionIndexChanged += UpdateUi;
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

    private void SetPokemonHealthUpdate()
    {
        _healthPhaseUpdateEvent = 
            (attacker) => PokemonOperations.UpdateHealthPhase(pokemon, hpSliderImage);
        if (pokemon == null) return;
        pokemon.OnHealthChanged += _healthPhaseUpdateEvent;
            
        if(pokemon.hp<=0)
        {
            statusEffectImage.gameObject.SetActive(true);
            statusEffectImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(AssetDirectory.Status)
                + "fainted");
        }
    }
    void CheckIfViewing(InputState currentState)
    {
        if (isEmpty) return;
        
        if (currentState.stateGroups.Contains(InputStateGroup.PokemonParty))
        {
            SetPokemonHealthUpdate();
        }
        else
        {
            _viewingParty = false;
            if(pokemon!=null) pokemon.OnHealthChanged -= _healthPhaseUpdateEvent;
            ChangeVisibility(false);
        }
        _isViewingCard = currentState.stateName == InputStateName.PokemonPartyNavigation
                         || currentState.stateName == InputStateName.PokemonPartyItemUsage;
        if (_isViewingCard)
        {
            _inputStateHandler.OnSelectionIndexChanged += UpdateUi;
            UpdateUi(_inputStateHandler.CurrentState.currentSelectionIndex);
        }
    }
    private void ResetSelectionVisual(InputState previousState)
    {
        if (!_viewingParty) return;
        if (isEmpty) return;
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
        if (currentIndex == _pokemonPartyHandler.numMembers)
        {
            ChangeVisibility(false);
            return;
        }
        ChangeVisibility(_pokemonPartyHandler.party[currentIndex] == pokemon);
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
