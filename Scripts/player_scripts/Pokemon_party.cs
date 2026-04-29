
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_party : MonoBehaviour,IInjectable
{
    public Pokemon[] party;
    public int numMembers;
    public int selectedMemberNumber;
    public int memberToMove;
    private bool _swappingIn;
    public bool swapOutNext;
    public bool moving;
    public Pokemon_party_member[] memberCards;
    public GameObject partyUI;
    public GameObject memberSelector;
    public GameObject optionSelector;
    public Image cancelButton;
    private Item _itemToUse;
    public GameObject[] partyOptions;
    public GameObject partyOptionsParent;
    public Text partyUsageText;
    public event Action<int> OnMemberSelected;
    
    private Dialogue_handler _dialogueHandler;
    private PokemonOperations _pokemonOperations;
    private InputStateHandler _inputStateHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private pokemon_storage _pokemonStorageHandler;
    private Item_handler _itemHandler;
    private BattleIntro _battleIntroHandler;
    private PokemonPartyInputService _partyInputService;
    public void Inject(ServiceContainer container)
    {
        _partyInputService = container.Resolve<PokemonPartyInputService>();
        _dialogueOptionsHandler = container.Resolve<DialogueOptionsEventHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _pokemonOperations = container.Resolve<PokemonOperations>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _itemHandler = container.Resolve<Item_handler>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        gameObject.SetActive(true);
    }       

    public void UpdatePartyUsageMessage(string message)
    {
        partyUsageText.text = message;
    }
    public void ExitParty()
    {
        if (swapOutNext) return;
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
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
        for (int i = 0; i < 6; i++)
            if (party[i] != null)
                validPokemon.Add(party[i]);
        return validPokemon;
    }
    private bool IsValidSwap(int memberPosition)
    {
        if (_turnBasedCombatHandler.ContainsSwitch(memberPosition-1))
        {
            _dialogueHandler.DisplayDetails(party[memberPosition-1].pokemonName +
                                                     " is already going to be sent out");
            return false;
        }
        if ( (memberPosition < 3 & _battleHandler.isDoubleBattle) || memberPosition == 1)
        {
            var swapIn = _battleHandler.battleParticipants[memberPosition - 1];
            
            _dialogueHandler.DisplayDetails(swapIn.pokemon.pokemonName +
                                                     " is already in battle");
            return false;
        }
        var participantIndex = (_battleHandler.isDoubleBattle & _swappingIn)
            ?_turnBasedCombatHandler.currentTurnIndex :0;
        
        var currentParticipant = _battleHandler.battleParticipants[participantIndex];
        if (!currentParticipant.canEscape & _swappingIn)
        {
            _dialogueHandler.DisplayDetails(currentParticipant.pokemon.pokemonName +
                                                     " is trapped");
            return false;
        }
        return true;
    }
    public void SelectMemberToBeSwapped(int memberPosition)
    {
        if (_dialogueOptionsHandler.playerInBattle)
        {//cant swap in a member who is already fighting
            var currentParticipant = _battleHandler.GetCurrentParticipant();
            _swappingIn = true;
            if (!IsValidSwap(memberPosition))
            {
                _swappingIn = false; return;
            }
            _battleHandler.usedTurnForSwap = true;

            var switchData = new SwitchOutData(_turnBasedCombatHandler.currentTurnIndex
                ,memberPosition - 1,currentParticipant);
            _turnBasedCombatHandler.SaveSwitchTurn(switchData);
            
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
            selectedMemberNumber = 0;
            _swappingIn = false;
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

    public void ReceiveItem(Item item)
    {
        _itemToUse = item;
    }
    public void SelectMember(int memberPosition)
    {
        var selectedMember = memberCards[memberPosition - 1];
        if (selectedMember.isEmpty) return;
        
        if (_dialogueOptionsHandler.playerInBattle && selectedMember.pokemon.hp <= 0)
            if (!_itemHandler.usingItem || swapOutNext)
                return;
        
        if (swapOutNext)
        {//selecting a swap in
            if (!IsValidSwap(memberPosition)) return;
            OnMemberSelected?.Invoke(memberPosition);
        }
        else if (_itemHandler.usingItem)
        {//use item on pokemon
            _itemHandler.UseItem(_itemToUse,selectedMember.pokemon);
        }
        else
        {//move around members in party
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
        swapOutNext = false;
        _inputStateHandler.OnStateChanged -= CheckStateUpdate;
        _inputStateHandler.OnSelectionIndexChanged -= UpdateCancelButton;
        cancelButton.sprite = memberCards[0].pokeballClosedImage.sprite;
        _itemHandler.usingItem = false;//in case player closes before using item
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
        var message = $"You swapped {party[partyIndex].pokemonName} with {swapStore.pokemonName}";
        party[selectedMemberNumber-1] = party[partyIndex];
        party[partyIndex] = swapStore;
        moving = false;
        if (_dialogueOptionsHandler.playerInBattle)
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

    public void AddMemberForTesting(Pokemon pokemon)
    {
        AddMember(pokemon,isGiftPokemon:true);
    }
    public void AddMember(Pokemon pokemon, string pokeballType="Pokeball", bool isGiftPokemon=false)
    {
        var newPokemon = InstanceFactory.CreatePokemon(pokemon); 
        newPokemon.pokeballName = pokeballType; 
        newPokemon.hasTrainer = true; 
        if (isGiftPokemon)
        {
            _pokemonOperations.SetPokemonTraits(newPokemon);
            newPokemon.ChangeFriendshipLevel(120);
            if (newPokemon.currentLevel == 0)
                newPokemon.LevelUp();
            newPokemon.hp = newPokemon.maxHp;
        }
        if (numMembers<6)
        {
            party[numMembers] = newPokemon;
            numMembers++;
            _pokemonStorageHandler.numPartyMembers++;
        }
        else
            _pokemonStorageHandler.AddPokemonToStorage(newPokemon);
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
        for (int i=0;i<6;i++)
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
}
