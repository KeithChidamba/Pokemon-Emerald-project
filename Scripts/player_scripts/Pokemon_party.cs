
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Pokemon_party : MonoBehaviour
{
    public Pokemon[] party;
    public int num_members=0;
    public int Selected_member=0;
    public int Member_to_Move=0;
    public bool moving = false;
    public bool Swapping_in=false;
    public bool SwapOutNext = false;
    public bool viewing_details = false;
    public bool viewing_options = false;
    public bool viewing_party = false;
    public bool Giving_item = false;
    [FormerlySerializedAs("Memeber_cards")] public Pokemon_party_member[] Member_cards;
    public GameObject party_ui;
    public GameObject member_indicator;
    private Item item_to_use;
    public static Pokemon_party instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Start()
    {
        Battle_handler.Instance.OnBattleEnd += close_party;
    }
    public List<Pokemon> GetLivingPokemon()
    {
        List<Pokemon> alivePokemon=new();
        foreach (Pokemon p in Pokemon_party.instance.party)
            if (p != null && p.HP > 0)
                alivePokemon.Add(p);
        return alivePokemon;
    }
    public void View_party()
    {
         viewing_party = true;
         Refresh_Member_Cards();
    }
    public void Pkm_Details()
    {//view pokemon details from button click
        Pokemon_Details.instance.Load_Details(party[Selected_member-1]);
        viewing_details = true;
        Cancel();
    }

    private bool isvalidSwap(int Member_position)
    {
        if(Member_position<3)
        {
            Battle_Participant Swapin = Battle_handler.Instance.battleParticipants[Member_position - 1];
            if (Swapin.pokemon != null)
                if (Swapin.pokemon == party[Member_position - 1])
                {
                    Dialogue_handler.instance.Write_Info(Swapin.pokemon.Pokemon_name +
                                                         " is already in battle", "Details", 1f);
                    return false;
                }
        }
        return true;
    }
    public void Moving(int Member_position)
    {
        if (Options_manager.instance.playerInBattle)
        {//cant swap in a member who is already fighting
            if (!isvalidSwap(Member_position)) return;
            Swapping_in = true;
            SwapOutNext = false;
            Selected_member = Turn_Based_Combat.Instance.currentTurnIndex+1;
            Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].ResetParticipantState();
            Move_Member(Member_position);
        }
        else
        {
            if (num_members > 1)
            {
                moving = true;
                Member_to_Move = Member_position;
                viewing_options = false;
                Member_cards[Member_position - 1].GetComponent<Pokemon_party_member>().Options.SetActive(false);
            }
            else
                Dialogue_handler.instance.Write_Info("There must be at least 2 Pokemon to swap","Details",1f);
        }
    }

    public void Recieve_item(Item item)
    {
        item_to_use = item;
    }
    public void Member_Selected(int Member_position)
    {
        Pokemon_party_member selectedMember = Member_cards[Member_position - 1];
        if (Options_manager.instance.playerInBattle && selectedMember.pkm.HP <= 0) return;
        if (SwapOutNext)
        {//selecting a swap in
            if (!isvalidSwap(Member_position)) return;
            Move_Member(Member_position);
        }
        else if (Item_handler.instance.Using_item)
        {//use item on pokemon
            Item_handler.instance.selected_party_pkm = selectedMember.pkm;
            Item_handler.instance.Use_Item(item_to_use);
            member_indicator.transform.position = selectedMember.transform.position;
            member_indicator.SetActive(true);
        }
        else if (Giving_item)
            GiveItem(Member_position);
        else
        {//move around members in party
            if (selectedMember.GetComponent<Pokemon_party_member>().isEmpty)
                Cancel();
            else
            {
                if (moving)
                {
                    Selected_member = Member_position;
                    Move_Member(Member_to_Move);
                }
                else
                {
                    Cancel();
                    Selected_member = Member_position;
                    viewing_options = true;
                    member_indicator.transform.position = selectedMember.transform.position;
                    member_indicator.SetActive(true);
                    selectedMember.GetComponent<Pokemon_party_member>().Options.SetActive(true);
                }
            }
        }
    }
    public void Cancel()
    {
        viewing_options = false;
        moving = false;
        member_indicator.SetActive(false);
        for (int i = 0; i < num_members; i++)
            Member_cards[i].GetComponent<Pokemon_party_member>().Options.SetActive(false);
    }

    private void GiveItem(int Member_position)
    {
        Pokemon_party_member selectedMember = Member_cards[Member_position - 1];
        if (selectedMember.pkm.HasItem)
        {
            Dialogue_handler.instance.Write_Info(selectedMember.pkm.Pokemon_name
                                                 +" is already holding something","Details",1f);
            Giving_item = false;
            item_to_use = null;
            Game_ui_manager.instance.Close_party();
            Game_ui_manager.instance.Invoke(nameof(Game_ui_manager.instance.View_Bag),1.1f);
            return;
        }
        Dialogue_handler.instance.Write_Info(selectedMember.pkm.Pokemon_name
                                             +" recieved a "+item_to_use.Item_name,"Details",1.3f);
        selectedMember.pkm.GiveItem(Obj_Instance.set_Item(item_to_use));
        item_to_use.quantity--;
        Bag.instance.check_Quantity(item_to_use);
        member_indicator.transform.position = selectedMember.transform.position;
        member_indicator.SetActive(true);
        Giving_item = false;
        item_to_use = null;
        Refresh_Member_Cards();
    }
    private void Move_Member(int Party_position)
    {
        Party_position--;
        if (SwapOutNext || Swapping_in)
        {
            swap(Party_position);
            Invoke(nameof(switchIn),1f);//
        }
        else
            if(party[Selected_member-1] != party[Party_position])
                swap(Party_position);
    }
private void close_party()
{
    Game_ui_manager.instance.Close_party();
    SwapOutNext = false;
    Swapping_in = false;
}
    void switchIn()
    {
        if (SwapOutNext)
            Turn_Based_Combat.Instance.faintEventDelay = false;
        if(Swapping_in)
            Turn_Based_Combat.Instance.NextTurn();
        close_party();
    }
    void swap(int Party_position)
    {
        Pokemon Swap_store = party[Selected_member-1];
        party[Selected_member-1] = party[Party_position];
        party[Party_position] = Swap_store;
        moving = false;
        if (Options_manager.instance.playerInBattle)
            Battle_handler.Instance.SetParticipant(Battle_handler.Instance.battleParticipants[Selected_member-1]);
        Member_to_Move = 0;
        Selected_member = 0;
        Refresh_Member_Cards();
        member_indicator.SetActive(false);
        Cancel();
        if(!Swapping_in && !SwapOutNext)
            Dialogue_handler.instance.Write_Info("You swapped " + Swap_store.Pokemon_name+ " with "+ party[Party_position].Pokemon_name,"Details",1f);
    }
    public void Add_Member(Pokemon pokemon)
    { //add new pokemon after catch or event
        if (num_members<6)
        {
            party[num_members] = pokemon_storage.instance.Add_pokemon(pokemon);
            num_members++;
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
    void sort_Members(int empty_position)
    {
        if(empty_position < party.Length-1)
        {
            for (int i = empty_position; i < party.Length - 1; i++)
                party[i] = party[i + 1];
            party[party.Length - 1] = null;
        }
    }
    public void Refresh_Member_Cards()
    {
        num_members = 0;
        foreach (Pokemon_party_member mon in Member_cards)
        {
                Member_cards[num_members].pkm = null;
                Member_cards[num_members].Reset_ui();
        }
        for (int i=0;i<6;i++)
        {
            if (party[i] != null)
            {
                Member_cards[num_members].pkm = party[i];
                Member_cards[num_members].Party_pos = num_members + 1;
                Member_cards[num_members].Set_Ui();
                num_members++;
            }
            else
                Member_cards[i].Reset_ui();
        }
    }
    public void Remove_Member(int Party_position)
    {
        //pc operation remove from party
        Party_position--;
        Pokemon member = party[Party_position];
        party[Party_position] = null;
        num_members--;
        pokemon_storage.instance.non_party_pokemon.Add(member);
        sort_Members(Party_position);
    }
}
