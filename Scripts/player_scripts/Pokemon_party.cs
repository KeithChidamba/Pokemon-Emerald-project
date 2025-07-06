
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Pokemon_party : MonoBehaviour
{
    public Pokemon[] party;
    public int numMembers;
    public int selectedMemberIndex;
    public int memberToMove;
    public bool moving;
    public bool swappingIn;
    public bool swapOutNext;
    public bool givingItem;
    public Pokemon_party_member[] memberCards;
    public GameObject partyUI;
    public GameObject memberSelector;
    public GameObject optionSelector;
    private Item _itemToUse;
    public GameObject[] partyOptions;
    public GameObject partyOptionsParent;
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
    public List<Pokemon> GetLivingPokemon()
    {
        List<Pokemon> alivePokemon = new();
        for (int i = 0; i < 6; i++)
            if (party[i] != null)
                if(party[i].hp > 0)
                    alivePokemon.Add(party[i]);
        return alivePokemon;
    }
    private bool IsValidSwap(int memberPosition)
    {
        if ( (memberPosition < 3 & Battle_handler.Instance.isDoubleBattle) || memberPosition == 1)
        {
            var swapIn = Battle_handler.Instance.battleParticipants[memberPosition - 1];
            
            Dialogue_handler.Instance.DisplayDetails(swapIn.pokemon.pokemonName +
                                                  " is already in battle", 1f);
            return false;
        }
        var participantIndex = (Battle_handler.Instance.isDoubleBattle & swappingIn)
            ?Turn_Based_Combat.Instance.currentTurnIndex :0;
        
        var currentParticipant = Battle_handler.Instance.battleParticipants[participantIndex];
        if (!currentParticipant.canEscape & swappingIn)
        {
            Dialogue_handler.Instance.DisplayDetails(currentParticipant.pokemon.pokemonName +
                                                  " is trapped", 1f);
            return false;
        }
        return true;
    }
    public void SelectMemberToBeSwapped(int memberPosition)
    {
        if (Options_manager.Instance.playerInBattle)
        {//cant swap in a member who is already fighting
            var currentParticipant = Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex];
           
            swappingIn = true;
            if (!IsValidSwap(memberPosition)) { swappingIn = false; return;}
            swapOutNext = false;
            selectedMemberIndex = Turn_Based_Combat.Instance.currentTurnIndex+1;
            currentParticipant.ResetParticipantState();
            MoveMember(memberPosition);
        }
        else
        {
            if (numMembers > 1)
            {
                Dialogue_handler.Instance.DisplayDetails("Select Pokemon to swap with");
                moving = true;
                memberToMove = memberPosition;
                partyOptionsParent.SetActive(false);
                InputStateHandler.Instance.RemoveTopInputLayer(false);
            }
            else
                Dialogue_handler.Instance.DisplayDetails("There must be at least 2 Pokemon to swap",1f);
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
            MoveMember(memberPosition);
        }
        else if (Item_handler.Instance.usingItem)
        {//use item on pokemon
            Item_handler.Instance.UseItem(_itemToUse,selectedMember.pokemon);
            memberSelector.transform.position = selectedMember.transform.position;
            memberSelector.SetActive(true);
        }
        else if (givingItem)
            GiveItemToMember(memberPosition);
        else
        {//move around members in party
            if (selectedMember.isEmpty)
                ClearSelectionUI();
            else
            {
                selectedMemberIndex = memberPosition;
                if (moving)
                    MoveMember(memberToMove);
                else
                {
                    ClearSelectionUI();
                    memberSelector.transform.position = selectedMember.transform.position;
                    memberSelector.SetActive(true);
                    partyOptionsParent.SetActive(true);
                    InputStateHandler.Instance.PokemonPartyOptions();
                }
            }
        }
    }
    public void ClearSelectionUI()
    {
        moving = false;
        memberSelector.SetActive(false);
        partyOptionsParent.SetActive(false);
    }

    public void ResetPartyState()
    {
        swapOutNext = false;
        swappingIn = false;

        Item_handler.Instance.usingItem = false;//in case player closes before using item
        
        givingItem = false;
    }
    private void GiveItemToMember(int memberPosition)
    {
        var selectedMember = memberCards[memberPosition - 1];
        if (selectedMember.pokemon.hasItem)
        {
            Dialogue_handler.Instance.DisplayDetails(selectedMember.pokemon.pokemonName
                                                 +" is already holding something",1f);
            givingItem = false;
            _itemToUse = null;
            return;
        }
        Dialogue_handler.Instance.DisplayDetails(selectedMember.pokemon.pokemonName
                                             +" received a "+_itemToUse.itemName,1.3f);
        selectedMember.pokemon.GiveItem(Obj_Instance.CreateItem(_itemToUse));
        _itemToUse.quantity--;
        Bag.Instance.CheckItemQuantity(_itemToUse);
        memberSelector.transform.position = selectedMember.transform.position;
        memberSelector.SetActive(true);
        givingItem = false;
        _itemToUse = null;
        RefreshMemberCards();
    }
    private void MoveMember(int partyPosition)
    {
        partyPosition--;
        if (swapOutNext || swappingIn)
        {
            SwapMembers(partyPosition);
            Invoke(nameof(SwitchIn),1f);
        }
        else
            if(party[selectedMemberIndex-1] != party[partyPosition])
                SwapMembers(partyPosition);
    }

    void SwitchIn()
    {
        if (swapOutNext)
            Turn_Based_Combat.Instance.faintEventDelay = false;
        if(swappingIn)
            Turn_Based_Combat.Instance.NextTurn();
        InputStateHandler.Instance.RemoveTopInputLayer(true);
    }
    private void SwapMembers(int partyPosition)
    {
        var swapStore = party[selectedMemberIndex-1];
        var message = $"You swapped {party[partyPosition].pokemonName} with {swapStore.pokemonName}";
        party[selectedMemberIndex-1] = party[partyPosition];
        party[partyPosition] = swapStore;
        moving = false;
        if (Options_manager.Instance.playerInBattle)
            Battle_handler.Instance.SetParticipant(Battle_handler.Instance.battleParticipants[selectedMemberIndex-1]);
        memberToMove = 0;
        selectedMemberIndex = 0;
        RefreshMemberCards();
        memberSelector.SetActive(false);
        ClearSelectionUI();
        if(!swappingIn && !swapOutNext)
            Dialogue_handler.Instance.DisplayDetails(message,1f);
    }
    public void AddMember(Pokemon pokemon)
    { //add new pokemon after catch or event
        if (numMembers<6)
        {
            party[numMembers] = pokemon_storage.Instance.CreateAndSetupPokemon(pokemon);
            numMembers++;
        }
        else
        {
            pokemon_storage.Instance.doingStorageOperation = false;
            pokemon_storage.Instance.nonPartyPokemon.Add(pokemon_storage.Instance.CreateAndSetupPokemon(pokemon));
            pokemon_storage.Instance.numNonPartyPokemon++;
        }
    }
    void SortMembers(int emptyPosition)
    {
        if(emptyPosition < party.Length-1)
        {
            for (int i = emptyPosition; i < party.Length - 1; i++)
                party[i] = party[i + 1];
            party[party.Length - 1] = null;
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
                memberCards[numMembers].partyPosition = numMembers + 1;
                memberCards[numMembers].ActivateUI();
                numMembers++;
            }
            else
                memberCards[i].ResetUI();
        }
    }
    public void RemoveMember(int Party_position)
    {
        //pc operation remove from party
        Party_position--;
        var member = party[Party_position];
        party[Party_position] = null;
        numMembers--;
        pokemon_storage.Instance.nonPartyPokemon.Add(member);
        SortMembers(Party_position);
    }
}
