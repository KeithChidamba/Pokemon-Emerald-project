
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum PartyUsage
{
    SwapOut,ItemUsage,General
}
public class Pokemon_party : MonoBehaviour,IInjectable
{
    public Pokemon[] party;
    public int numMembers;
    public int selectedMemberNumber;
    public int memberToMove;
    public readonly int maxNumMembers = 6;
    private int _currentStepCount;
    
    public bool moving;
    public Pokemon_party_member[] memberCards;
    public GameObject partyUI;
    public GameObject memberSelector;
    public GameObject optionSelector;
    public Image cancelButton;

    public GameObject[] partyOptions;
    public GameObject partyOptionsParent;
    public Text partyUsageText;
    public event Action<int> OnMemberSelected;
    public PartyUsage currentUsage;
    
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private pokemon_storage _pokemonStorageHandler;
    private Item_handler _itemHandler;
    private BattleIntro _battleIntroHandler;
    private Player_movement _playerMovementHandler;
    private PokemonPartyInputService _partyInputService;
    private PokemonOperations _pokemonOperationsHandler;
    private Game_Load _gameLoadingHandler;
    
    public void Inject(ServiceContainer container)
    {
        _playerMovementHandler = container.Resolve<Player_movement>();
        _partyInputService = container.Resolve<PokemonPartyInputService>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _itemHandler = container.Resolve<Item_handler>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _playerMovementHandler.OnNewTile += CheckPoisonTickEffect;
    }
    private void CheckPoisonTickEffect()
    {
        _currentStepCount++;
        if (_currentStepCount < 4) return;
        _currentStepCount = 0;
        
        for (int i = 0; i < numMembers; i++)
        {
            if (party[i] != null)
            {
                if (party[i].hp == 0) continue;
                
                if (party[i].statusEffect == StatusEffect.Poison)
                {
                    //no need for hp loss animation since this only happens outside ui
                    party[i].hp--;
                }
            }
        }
    }
    
    public void UpdatePartyUsageMessage(string message)
    {
        partyUsageText.text = message;
    }
    public void ValidatePartyExit()
    {
        if (currentUsage==PartyUsage.SwapOut) return;
        _inputStateHandler.ResetRelevantUi(InputStateName.PokemonPartyNavigation,true);
    }

    public void CheckStateUpdate(InputState currentState)
    {
        if (currentState.stateName != InputStateName.PokemonPartyNavigation 
            && currentState.stateName != InputStateName.PokemonPartyItemUsage)
            return;
        _inputStateHandler.OnSelectionIndexChanged += UpdateCancelButton;
    }
    private void UpdateCancelButton(int currentIndex)
    {
        cancelButton.sprite = currentIndex < numMembers? memberCards[0].pokeballClosedImage.sprite
                :memberCards[0].pokeballOpenImage.sprite;
    }
    public List<Pokemon> GetLivingPokemon()
    {
        List<Pokemon> alivePokemon = new(GetValidPokemon());
        alivePokemon.RemoveAll(p => p.hp <= 0);
        return alivePokemon;
    }
    public  List<Pokemon> GetValidPokemon()
    {
        List<Pokemon> validPokemon = new();
        for (int i = 0; i < maxNumMembers; i++)
            if (party[i] != null)
                validPokemon.Add(party[i]);
        return validPokemon;
    }
    private bool IsValidSwap(int memberPosition,bool swappingIn)
    {
        if (_turnBasedCombatHandler.ContainsSwitch(memberPosition-1))
        {
            _dialogueHandler.DisplayDetails(party[memberPosition-1].pokemonDisplayName +
                                                     " is already going to be sent out");
            return false;
        }
        if ( (memberPosition < 3 & _battleHandler.isDoubleBattle) || memberPosition == 1)
        {
            var swapIn = _battleHandler.battleParticipants[memberPosition - 1];
            
            _dialogueHandler.DisplayDetails(swapIn.pokemon.pokemonDisplayName +
                                                     " is already in battle");
            return false;
        }
        var participantIndex = (_battleHandler.isDoubleBattle && swappingIn)
            ?_turnBasedCombatHandler.currentTurnIndex :0;
        
        var currentParticipant = _battleHandler.battleParticipants[participantIndex];
        if (!currentParticipant.canEscape && swappingIn)
        {
            _dialogueHandler.DisplayDetails(currentParticipant.pokemon.pokemonDisplayName +
                                                     " is trapped");
            return false;
        }
        return true;
    }
    public void BeginMemberSwap(int memberPosition)
    {
        if (_battleHandler.battleInProgress)
        {//cant swap in a member who is already fighting
            var currentParticipant = _battleHandler.GetCurrentParticipant();
            if (!IsValidSwap(memberPosition,true))
            {
               return;
            }
            _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.SwitchPokemonIn);

            var switchData = new SwitchOutData(_turnBasedCombatHandler.currentTurnIndex
                ,memberPosition - 1,currentParticipant);
            _turnBasedCombatHandler.SaveSwitchTurn(switchData);
            
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
            selectedMemberNumber = 0;
        }
        else
        {
            if (numMembers > 1)
            {
                UpdatePartyUsageMessage("Select Pokemon to swap with");
                moving = true;
                memberToMove = memberPosition;
                partyOptionsParent.SetActive(false);
                _inputStateHandler.RemoveTopInputLayer(false);
            }
            else
                _dialogueHandler.DisplayDetails("There must be at least 2 Pokemon to swap");
        }
    }
    
    public void SelectMember(int memberPosition)
    {
        var selectedMember = memberCards[memberPosition - 1];
        if (selectedMember.isEmpty) return;
        
        if (currentUsage == PartyUsage.SwapOut && selectedMember.pokemon.hp <= 0)
        {
            return;
        }
        switch (currentUsage)
        {
            case PartyUsage.SwapOut:
                if (!IsValidSwap(memberPosition,false)) return;
                OnMemberSelected?.Invoke(memberPosition);
                break;
            case PartyUsage.General:
                GeneralPartyUsage();
                break;
            case PartyUsage.ItemUsage:
                OnMemberSelected?.Invoke(memberPosition);
                break;
        }

        void GeneralPartyUsage()
        {
            if (selectedMember.isEmpty)
                ClearSelectionUI();
            else
            {
                selectedMemberNumber = memberPosition;
                if (moving)
                {
                    memberToMove--;
                    if(party[selectedMemberNumber-1] != party[memberToMove])
                        SwapMembers(memberToMove);
                }
                else
                {
                    ClearSelectionUI();
                    partyOptionsParent.SetActive(true);
                    _partyInputService.PokemonPartyOptions();
                }
            }
        }
    }
    public void ClearSelectionUI()
    {
        moving = false;
        partyOptionsParent.SetActive(false);
    }

    public void ResetPartyState()
    {
        currentUsage = PartyUsage.General;
        _inputStateHandler.OnStateChanged -= CheckStateUpdate;
        _inputStateHandler.OnSelectionIndexChanged -= UpdateCancelButton;
        cancelButton.sprite = memberCards[0].pokeballClosedImage.sprite;
    }
    public IEnumerator SwapMemberWithoutTurnUsage(int partyPosition)
    {
        partyPosition--;
        (party[selectedMemberNumber-1], party[partyPosition]) = 
            (party[partyPosition], party[selectedMemberNumber-1]);

        var participant = _battleHandler.battleParticipants[selectedMemberNumber - 1];
        var alivePokemon= GetLivingPokemon();
        
        UpdateUIAfterSwap();
        
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
        
        //this is called when ui doesnt shift so no need for changes in boolean
        yield return _battleIntroHandler.SwitchInPokemon(participant,alivePokemon[selectedMemberNumber - 1],false);
        
        selectedMemberNumber = 0;
        _turnBasedCombatHandler.faintEventDelay = false;
    }
    public void UpdateUIAfterSwap()
    {
        RefreshMemberCards();
        memberCards[0].ChangeVisibility(true);
        ClearSelectionUI();
        _partyInputService.UpdateHealthBarColors();
    }
    private void SwapMembers(int partyIndex)
    {
        var swapStore = party[selectedMemberNumber-1];
        var message = $"You swapped {party[partyIndex].pokemonDisplayName} with {swapStore.pokemonDisplayName}";
        party[selectedMemberNumber-1] = party[partyIndex];
        party[partyIndex] = swapStore;
        moving = false;
        if (_battleHandler.battleInProgress)
        {
            var participant = _battleHandler.battleParticipants[selectedMemberNumber - 1];
            var alivePokemon= GetLivingPokemon();
            StartCoroutine(_battleIntroHandler.SwitchInPokemon(participant,alivePokemon[selectedMemberNumber - 1]));
        }
        else
            _dialogueHandler.DisplayDetails(message);
        UpdatePartyUsageMessage("Choose a pokemon");
        memberToMove = 0;
        selectedMemberNumber = 0;
        UpdateUIAfterSwap();
    }

    public void AddMember(Pokemon pokemon, string pokeballType)
    {
        var newPokemon = InstanceFactory.CreatePokemon(pokemon); 
        newPokemon.pokeballName = pokeballType; 
        newPokemon.hasTrainer = true;
        CompletePokemonAddition(newPokemon);
    }
    public void AddGiftMember(PokemonGiftInteractoinInfo giftData)
    {
        _pokemonOperationsHandler.CreateSpecificPokemon(GetNewMember,giftData.giftPokemon,giftData.pokemonLevel
            ,giftData.evolutionStageNumber);
        void GetNewMember(Pokemon newPokemon)
        {
            newPokemon.hasTrainer = true;
            newPokemon.pokeballName = "Pokeball"; 
            newPokemon.ChangeFriendshipLevel(120);
            newPokemon.captureInformation.levelCaptured = newPokemon.currentLevel;
            newPokemon.captureInformation.areaName = Utility.GetAreaName(_gameLoadingHandler.playerData.location);
            _pokemonOperationsHandler.SetupPokemonNaming(newPokemon, (result)=>CompletePokemonAddition(newPokemon));
        }
    }
    private void CompletePokemonAddition(Pokemon newPokemon)
    {
        if (newPokemon.nickName == string.Empty)
        {
            newPokemon.nickName = newPokemon.pokemonName;
        }
        if (numMembers<maxNumMembers)
        {
            party[numMembers] = newPokemon;
            numMembers++;
        }
        else
            _pokemonStorageHandler.AddPokemonToStorage(newPokemon);
        _dialogueHandler.DisplayDetails("You got a " + newPokemon.pokemonDisplayName);
    }
  
    public void SortByFainted()
    {
        for (int i = 0; i < numMembers - 1; i++)
        {
            bool swapped = false;

            for (int j = 0; j < numMembers - i - 1; j++)
            {
                var current = party[j];
                var next = party[j + 1];

                // Swap only if current is fainted and next is not fainted
                if (current.hp <= 0 && next.hp > 0)
                {
                    (party[j], party[j + 1]) = (next, current);
                    swapped = true;
                }
            }

            // Stop early if no swaps occurred
            if (!swapped)
                break;
        }
    }
    public void RefreshMemberCards()
    {
        numMembers = 0;
        for (int i=0;i<maxNumMembers;i++)
        {
            if (party[i] != null)
            {
                memberCards[numMembers].pokemon = party[i];
                memberCards[numMembers].ActivateUI();
                numMembers++;
            }
            else
            {
                memberCards[i].ResetUI();
            }
        }
    }
    public void RemoveMember(int partyPosition)
    {
        partyPosition--;
        party[partyPosition] = null;
        numMembers--;
        //sort
        if(partyPosition < party.Length-1)
        {
            for (int i = partyPosition; i < party.Length - 1; i++)
                party[i] = party[i + 1];
            party[^1] = null;
        }
    }
    public void HealPartyPokemon()
    {
        for (int i = 0; i < numMembers; i++)
        {
            var pokemon = party[i];
            pokemon.hp = pokemon.maxHp;
            foreach (var move in pokemon.moveSet)
                move.powerpoints = move.maxPowerpoints;
            pokemon.statusEffect = StatusEffect.None;
        }
    }
}
