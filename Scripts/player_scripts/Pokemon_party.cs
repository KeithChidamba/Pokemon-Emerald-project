using UnityEngine;

public class Pokemon_party : MonoBehaviour
{
    public Pokemon[] party;
    public pokemon_storage storage;
    public int num_members=0;
    public int Selected_member=0;
    public int Member_to_Move=0;
    public bool moving = false;
    public bool viewing_details = false;
    public bool viewing_options = false;
    public bool viewing_party = false;
    public Pokemon_party_member[] Memeber_cards;
    public GameObject party_ui;
    public GameObject member_indicator;
    public Pokemon_Details details;
    private Item item_to_use;
    public void View_party()
    {
         viewing_party = true;
         Refresh_Member_Cards();
    }
    public void Pkm_Details()
    {
        details.gameObject.SetActive(true);
        details.Load_Details(party[Selected_member-1]);
        viewing_details = true;
        Cancel();
    }
    public void Moving(int Member_position)
    {
        moving = true;
        Member_to_Move = Member_position;
        viewing_options = false;
        Memeber_cards[Member_position - 1].GetComponent<Pokemon_party_member>().Options.SetActive(false);
    }

    public void Recieve_item(Item item)
    {
        item_to_use = item;
    }
    public void Member_Selected(int Member_position)
    {
        if (storage.options.item_h.Using_item)
        {
            storage.options.item_h.selected_party_pkm = Memeber_cards[Member_position - 1].pkm;
            storage.options.item_h.Use_Item(item_to_use);
            member_indicator.transform.position = Memeber_cards[Member_position - 1].transform.position;
            member_indicator.SetActive(true);
        }
        else
        {
            if (Memeber_cards[Member_position - 1].GetComponent<Pokemon_party_member>().isEmpty)
            {
                Cancel();
            }
            else
            {
                if (moving)
                {
                    Selected_member = Member_position;
                    Move_Member(Member_to_Move);
                }
                else if (!viewing_options)
                {
                    if (Selected_member != 0)
                    {
                        Cancel();
                    }
                    Selected_member = Member_position;
                    viewing_options = true;
                    member_indicator.transform.position = Memeber_cards[Member_position - 1].transform.position;
                    member_indicator.SetActive(true);
                    Memeber_cards[Member_position - 1].GetComponent<Pokemon_party_member>().Options.SetActive(true);
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
        {
            Memeber_cards[i].GetComponent<Pokemon_party_member>().Options.SetActive(false);
        }
    }
    public void Move_Member(int Party_position)
    {
        Party_position--;
        if (party[Selected_member-1] != party[Party_position])
        {
            Pokemon Swap_store = party[Selected_member-1];
            party[Selected_member-1] = party[Party_position];
            party[Party_position] = Swap_store;
            moving = false;
            Member_to_Move = 0;
            Selected_member = 0;
            Refresh_Member_Cards();
            member_indicator.SetActive(false);
            storage.options.dialogue.Write_Info("You swapped " + Swap_store.Pokemon_name+ " with "+ party[Party_position].Pokemon_name,"Details");
            storage.options.dialogue.Dialouge_off(1f);
            storage.options.battle.Switch_In(0);

        }
    }
    public void Add_Member(Pokemon pokemon)
    { //add new pokemon after catch or event
        if (num_members<6)
        {
            party[num_members] = storage.Add_pokemon(pokemon);
            storage.all_pokemon.Add(party[num_members]);
            num_members++;
        }
        else
        {
            if (storage.num_pokemon < storage.max_num_pokemon)
            {
                storage.storage_operetation = false;
                storage.non_party_pokemon.Add(storage.Add_pokemon(pokemon));
                storage.storage_operetation = true;
                storage.all_pokemon.Add(storage.Add_pokemon(storage.non_party_pokemon[storage.num_pokemon-num_members-1]));
                storage.storage_operetation = false;
            }
        }
    }
    void sort_Members(int empty_position)
    {
        if(empty_position < party.Length-1)//empty pos in not at the end
        {
            for (int i = empty_position; i < party.Length - 1; i++)
            {
                party[i] = party[i + 1];
            }
            party[party.Length - 1] = null;
        }
    }
    public void Refresh_Member_Cards()
    {
        num_members = 0;
        foreach (Pokemon_party_member mon in Memeber_cards)
        {
                Memeber_cards[num_members].pkm = null;
                Memeber_cards[num_members].Reset_ui();
        }
        for (int i=0;i<6;i++)
        {
            if (party[i] != null)
            {
                Memeber_cards[num_members].pkm = party[i];
                Memeber_cards[num_members].Party_pos = num_members + 1;
                Memeber_cards[num_members].Set_Ui();
                num_members++;
            }
            else
            {
                Memeber_cards[i].Reset_ui();
            }
        }
    }
    public void Remove_Member(int Party_position)
    {
        //pc operation remove from party
        Party_position--;
        Pokemon member = party[Party_position];
        party[Party_position] = null;
        num_members--;
        storage.non_party_pokemon.Add(member);
        sort_Members(Party_position);
    }
}
