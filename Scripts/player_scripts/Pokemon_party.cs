
using UnityEngine;

public class Pokemon_party : MonoBehaviour
{
    public Pokemon[] party;
    public int num_members=0;
    public int Selected_member=0;
    public int Member_to_Move=0;
    public bool moving = false;
    public bool Swapping_in=false;
    public bool viewing_details = false;
    public bool viewing_options = false;
    public bool viewing_party = false;
    public Pokemon_party_member[] Memeber_cards;
    public GameObject party_ui;
    public GameObject member_indicator;
    private Item item_to_use;
    [SerializeField]private Pokemon_Details details;
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
        if (Options_manager.instance.playerInBattle)
        {
            Swapping_in = true;
            Battle_handler.instance.Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].Reset_pkm();
            Move_Member(Member_position);
        }
        else
        {
            if (num_members > 1)
            {
                moving = true;
                Member_to_Move = Member_position;
                viewing_options = false;
                Memeber_cards[Member_position - 1].GetComponent<Pokemon_party_member>().Options.SetActive(false);
            }
            else
            {
                Dialogue_handler.instance.Write_Info("There must be at least 2 Pokemon to swap","Details");
                Dialogue_handler.instance.Dialouge_off(1f);
            }
        }
    }

    public void Recieve_item(Item item)
    {
        item_to_use = item;
    }
    public void Member_Selected(int Member_position)
    {
        //selecting swap in
        if (Swapping_in)
            Move_Member(Member_position);
        else if (Item_handler.instance.Using_item)
        {
            Item_handler.instance.selected_party_pkm = Memeber_cards[Member_position - 1].pkm;
            Item_handler.instance.Use_Item(item_to_use);
            member_indicator.transform.position = Memeber_cards[Member_position - 1].transform.position;
            member_indicator.SetActive(true);
        }
        else
        {
            if (Memeber_cards[Member_position - 1].GetComponent<Pokemon_party_member>().isEmpty)
                Cancel();
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
                        Cancel();
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
        if (Swapping_in)
        {
            Selected_member = Party_position;
            swap(Party_position);
            Game_ui_manager.instance.Close_party();
            Swapping_in = false;
            Turn_Based_Combat.instance.Next_turn();
        }
        else
            if(party[Selected_member-1] != party[Party_position])
                swap(Party_position);
    }

    void swap(int Party_position)
    {
        Pokemon Swap_store = party[Selected_member-1];
        party[Selected_member-1] = party[Party_position];
        party[Party_position] = Swap_store;
        moving = false;
        Member_to_Move = 0;
        Selected_member = 0;
        Refresh_Member_Cards();
        member_indicator.SetActive(false);
        Battle_handler.instance.Set_pkm();
        if(!Swapping_in)
        {
            Dialogue_handler.instance.Write_Info("You swapped " + Swap_store.Pokemon_name+ " with "+ party[Party_position].Pokemon_name,"Details");
            Dialogue_handler.instance.Dialouge_off(1f);
        }
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
        if(empty_position < party.Length-1)//empty pos in not at the end
        {
            for (int i = empty_position; i < party.Length - 1; i++)
            {
                party[i] = party[i + 1];
            }
            party[party.Length - 1] = null;
        }
    }
    public void Refresh_Member_Cards()//call battle refresh
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
        pokemon_storage.instance.non_party_pokemon.Add(member);
        sort_Members(Party_position);
    }
}
