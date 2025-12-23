
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Pokemon_party : MonoBehaviour
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
    public static Pokemon_party Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UpdatePartyUsageMessage(string message)
    {
        partyUsageText.text = message;
    }
    public void ExitParty()
    {
        if (swapOutNext) return;
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonParty);
    }

    public void CheckStateUpdate(InputState currentState)
    {
        if (currentState.stateName != InputStateHandler.StateName.PokemonPartyNavigation 
            && currentState.stateName != InputStateHandler.StateName.PokemonPartyItemUsage)
            return;
        InputStateHandler.Instance.OnSelectionIndexChanged += UpdateCancelButton;
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
        if (Turn_Based_Combat.Instance.ContainsSwitch(memberPosition-1))
        {
            Dialogue_handler.Instance.DisplayDetails(party[memberPosition-1].pokemonName +
                                                     " is already going to be sent out");
            return false;
        }
        if ( (memberPosition < 3 & Battle_handler.Instance.isDoubleBattle) || memberPosition == 1)
        {
            var swapIn = Battle_handler.Instance.battleParticipants[memberPosition - 1];
            
            Dialogue_handler.Instance.DisplayDetails(swapIn.pokemon.pokemonName +
                                                     " is already in battle");
            return false;
        }
        var participantIndex = (Battle_handler.Instance.isDoubleBattle & _swappingIn)
            ?Turn_Based_Combat.Instance.currentTurnIndex :0;
        
        var currentParticipant = Battle_handler.Instance.battleParticipants[participantIndex];
        if (!currentParticipant.canEscape & _swappingIn)
        {
            Dialogue_handler.Instance.DisplayDetails(currentParticipant.pokemon.pokemonName +
                                                     " is trapped");
            return false;
        }
        return true;
    }
    public void SelectMemberToBeSwapped(int memberPosition)
    {
        if (Options_manager.Instance.playerInBattle)
        {//cant swap in a member who is already fighting
            var currentParticipant = Battle_handler.Instance.GetCurrentParticipant();
            _swappingIn = true;
            if (!IsValidSwap(memberPosition))
            {
                _swappingIn = false; return;
            }
            Battle_handler.Instance.usedTurnForSwap = true;

            var switchData = new SwitchOutData(Turn_Based_Combat.Instance.currentTurnIndex
                ,memberPosition - 1,currentParticipant);
            Turn_Based_Combat.Instance.SaveSwitchTurn(switchData);
            
            InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonParty);
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
                InputStateHandler.Instance.RemoveTopInputLayer(false);
            }
            else
                Dialogue_handler.Instance.DisplayDetails("There must be at least 2 Pokemon to swap");
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
        
        if (Options_manager.Instance.playerInBattle && selectedMember.pokemon.hp <= 0)
            if (!Item_handler.Instance.usingItem || swapOutNext)
                return;
        
        if (swapOutNext)
        {//selecting a swap in
            if (!IsValidSwap(memberPosition)) return;
            OnMemberSelected?.Invoke(memberPosition);
        }
        else if (Item_handler.Instance.usingItem)
        {//use item on pokemon
            Item_handler.Instance.UseItem(_itemToUse,selectedMember.pokemon);
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
                    InputStateHandler.Instance.PokemonPartyOptions();
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
        InputStateHandler.Instance.OnSelectionIndexChanged -= UpdateCancelButton;
        cancelButton.sprite = memberCards[0].pokeballClosedImage.sprite;
        Item_handler.Instance.usingItem = false;//in case player closes before using item
    }
    public IEnumerator SwapMemberInBattle(int partyPosition)
    {
        partyPosition--;
        (party[selectedMemberNumber-1], party[partyPosition]) = 
            (party[partyPosition], party[selectedMemberNumber-1]);

        var participant = Battle_handler.Instance.battleParticipants[selectedMemberNumber - 1];
        var alivePokemon= GetLivingPokemon();
        
        
        UpdateUIAfterSwap();
        
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonParty);
        yield return BattleIntro.Instance.SwitchInPokemon(participant,alivePokemon[selectedMemberNumber - 1],true);
        selectedMemberNumber = 0;
        Turn_Based_Combat.Instance.faintEventDelay = false;
    }
    public void UpdateUIAfterSwap()
    {
        RefreshMemberCards();
        ClearSelectionUI();
        InputStateHandler.Instance.UpdateHealthBarColors();
    }
    private void SwapMembers(int partyIndex)
    {
        var swapStore = party[selectedMemberNumber-1];
        var message = $"You swapped {party[partyIndex].pokemonName} with {swapStore.pokemonName}";
        party[selectedMemberNumber-1] = party[partyIndex];
        party[partyIndex] = swapStore;
        moving = false;
        if (Options_manager.Instance.playerInBattle)
        {
            var participant = Battle_handler.Instance.battleParticipants[selectedMemberNumber - 1];
            var alivePokemon= GetLivingPokemon();
            StartCoroutine(BattleIntro.Instance.SwitchInPokemon(participant,alivePokemon[selectedMemberNumber - 1]));
        }
        else
            Dialogue_handler.Instance.DisplayDetails(message);
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
        var newPokemon = Obj_Instance.CreatePokemon(pokemon); 
        newPokemon.pokeballName = pokeballType; 
        newPokemon.hasTrainer = true; 
        if (isGiftPokemon)
        {
            PokemonOperations.SetPokemonTraits(newPokemon);
            newPokemon.ChangeFriendshipLevel(120);
            if (newPokemon.currentLevel == 0)
                newPokemon.LevelUp();
            newPokemon.hp = newPokemon.maxHp;
        }
        if (numMembers<6)
        {
            party[numMembers] = newPokemon;
            numMembers++;
            pokemon_storage.Instance.numPartyMembers++;
        }
        else
            pokemon_storage.Instance.AddPokemonToStorage(pokemon);
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
                memberCards[i].ResetUI();
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
