
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
    public bool viewingDetails;
    public bool viewingOptions;
    public bool viewingParty;
    public bool givingItem;
    public Pokemon_party_member[] memberCards;
    public GameObject partyUI;
    public GameObject memberIndicator;
    private Item _itemToUse;
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
    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += CloseParty;
    }

    public List<Pokemon> GetLivingPokemon()
    {
        List<Pokemon> alivePokemon = new();
        for (int i = 0; i < 6; i++)
            if (party[i] != null)
                if(party[i].HP > 0)
                    alivePokemon.Add(party[i]);
        return alivePokemon;
    }
    public void ViewParty()
    {
         viewingParty = true;
         RefreshMemberCards();
    }
    public void ViewPokemonDetails()
    {//view pokemon details from button click
        Pokemon_Details.instance.Load_Details(party[selectedMemberIndex-1]);
        viewingDetails = true;
        ClearSelectionUI();
    }

    private bool IsValidSwap(int memberPosition)
    { 
        if (memberPosition >= 3) return false;
        var swapIn = Battle_handler.Instance.battleParticipants[memberPosition - 1];
        if (swapIn.pokemon == null) return false;
        if (swapIn.pokemon == party[memberPosition - 1])
        {
            Dialogue_handler.instance.Write_Info(swapIn.pokemon.Pokemon_name +
                                                 " is already in battle", "Details", 1f);
            return false;
        }
        return true;
    }
    public void SelectMemberToBeSwapped(int memberPosition)
    {
        if (Options_manager.Instance.playerInBattle)
        {//cant swap in a member who is already fighting
            if (!IsValidSwap(memberPosition)) return;
            swappingIn = true;
            swapOutNext = false;
            selectedMemberIndex = Turn_Based_Combat.Instance.currentTurnIndex+1;
            Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].ResetParticipantState();
            MoveMember(memberPosition);
        }
        else
        {
            if (numMembers > 1)
            {
                moving = true;
                memberToMove = memberPosition;
                viewingOptions = false;
                memberCards[memberPosition - 1].GetComponent<Pokemon_party_member>().options.SetActive(false);
            }
            else
                Dialogue_handler.instance.Write_Info("There must be at least 2 Pokemon to swap","Details",1f);
        }
    }

    public void ReceiveItem(Item item)
    {
        _itemToUse = item;
    }
    public void SelectMember(int memberPosition)
    {
        var selectedMember = memberCards[memberPosition - 1];
        if (Options_manager.Instance.playerInBattle && selectedMember.pokemon.HP <= 0) return;
        if (swapOutNext)
        {//selecting a swap in
            if (!IsValidSwap(memberPosition)) return;
            MoveMember(memberPosition);
        }
        else if (Item_handler.Instance.usingItem)
        {//use item on pokemon
            Item_handler.Instance.selectedPartyPokemon = selectedMember.pokemon;
            Item_handler.Instance.UseItem(_itemToUse);
            memberIndicator.transform.position = selectedMember.transform.position;
            memberIndicator.SetActive(true);
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
                    viewingOptions = true;
                    memberIndicator.transform.position = selectedMember.transform.position;
                    memberIndicator.SetActive(true);
                    selectedMember.options.SetActive(true);
                }
            }
        }
    }
    public void ClearSelectionUI()
    {
        viewingOptions = false;
        moving = false;
        memberIndicator.SetActive(false);
        for (int i = 0; i < numMembers; i++)
            memberCards[i].GetComponent<Pokemon_party_member>().options.SetActive(false);
    }

    private void GiveItemToMember(int memberPosition)
    {
        var selectedMember = memberCards[memberPosition - 1];
        if (selectedMember.pokemon.HasItem)
        {
            Dialogue_handler.instance.Write_Info(selectedMember.pokemon.Pokemon_name
                                                 +" is already holding something","Details",1f);
            givingItem = false;
            _itemToUse = null;
            Game_ui_manager.Instance.CloseParty();
            Game_ui_manager.Instance.Invoke(nameof(Game_ui_manager.Instance.ViewBag),1.1f);
            return;
        }
        Dialogue_handler.instance.Write_Info(selectedMember.pokemon.Pokemon_name
                                             +" recieved a "+_itemToUse.itemName,"Details",1.3f);
        selectedMember.pokemon.GiveItem(Obj_Instance.CreateItem(_itemToUse));
        _itemToUse.quantity--;
        Bag.Instance.CheckItemQuantity(_itemToUse);
        memberIndicator.transform.position = selectedMember.transform.position;
        memberIndicator.SetActive(true);
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
private void CloseParty()
{
    Game_ui_manager.Instance.CloseParty();
    swapOutNext = false;
    swappingIn = false;
}
    void SwitchIn()
    {
        if (swapOutNext)
            Turn_Based_Combat.Instance.faintEventDelay = false;
        if(swappingIn)
            Turn_Based_Combat.Instance.NextTurn();
        CloseParty();
    }
    private void SwapMembers(int partyPosition)
    {
        var swapStore = party[selectedMemberIndex-1];
        party[selectedMemberIndex-1] = party[partyPosition];
        party[partyPosition] = swapStore;
        moving = false;
        if (Options_manager.Instance.playerInBattle)
            Battle_handler.Instance.SetParticipant(Battle_handler.Instance.battleParticipants[selectedMemberIndex-1]);
        memberToMove = 0;
        selectedMemberIndex = 0;
        RefreshMemberCards();
        memberIndicator.SetActive(false);
        ClearSelectionUI();
        if(!swappingIn && !swapOutNext)
            Dialogue_handler.instance.Write_Info("You swapped " + swapStore.Pokemon_name+ " with "+ party[partyPosition].Pokemon_name,"Details",1f);
    }
    public void AddMember(Pokemon pokemon)
    { //add new pokemon after catch or event
        if (numMembers<6)
        {
            party[numMembers] = pokemon_storage.instance.Add_pokemon(pokemon);
            numMembers++;
        }
        else
        {
            if (pokemon_storage.instance.num_pokemon < pokemon_storage.instance.max_num_pokemon)
            {
                pokemon_storage.instance.storage_operetation = false;
                pokemon_storage.instance.non_party_pokemon.Add(pokemon_storage.instance.Add_pokemon(pokemon));
                pokemon_storage.instance.num_non_party_pokemon++;
            }
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
        pokemon_storage.instance.non_party_pokemon.Add(member);
        SortMembers(Party_position);
    }
}
