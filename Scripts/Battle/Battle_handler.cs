using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Battle_handler : MonoBehaviour
{
    public GameObject Battle_ui;
    public GameObject moves_ui;
    public GameObject options_ui;
    public Battle_Participant[] Battle_P = {null,null,null,null};
    public Text Move_pp, Move_type;
    public Text[] moves;
    public GameObject[] Move_btns;
    public bool is_trainer_battle = false;
    public bool isDouble_battle = false;
    public int Participant_count=0;
    public GameObject OverWorld;
    public bool viewing_options = false;
    public bool choosing_move = false;
    public bool running_away = false;
    public bool Selected_Move = false;
    private int Current_Move = 0;
    public int Current_pkm_Enemy = 0;
    public bool Doing_move = false;
    public static Battle_handler instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    void Update()
    {
        if (!Options_manager.instance.playerInBattle) return;
            Handle_battle();
    }

    private void Handle_battle()
    {
        if (Selected_Move &&(Input.GetKeyDown(KeyCode.F)) )
        {
            if (Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy)
            {
                if(isDouble_battle)
                    Use_Move(Battle_P[Current_Move].pokemon.move_set[Current_Move],Battle_P[Current_Move].pokemon);//any of payer's 2 pkm using move
                else
                    Use_Move(Battle_P[0].pokemon.move_set[Current_Move],Battle_P[0].pokemon);//player using move
                choosing_move = false;
            }
            else
                Dialogue_handler.instance.Write_Info("Click on who you will attack", "Battle info");
        }
        if(choosing_move && (Input.GetKeyDown(KeyCode.Escape)))//exit move selection
        {
            View_options();
            choosing_move = false;
            Reset_move();
        }
        if (overworld_actions.instance.using_ui)
        {
            options_ui.SetActive(false);
            viewing_options = false;
        }
        else
        {
            if (!choosing_move && !running_away && !Doing_move)
                viewing_options = true;
            else
                viewing_options = false;
            if (viewing_options)
            {
                Dialogue_handler.instance.Write_Info("What will you do?", "Battle info");
                options_ui.SetActive(true);
            }
        }
        Battle_logic();
    }

    void Battle_logic()
    {
        if (!isDouble_battle && Turn_Based_Combat.instance.Current_pkm_turn == 0)//if single battle, auto aim at enemy
        {
            Current_pkm_Enemy = 2;
            Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy = true;
        }
    }

    public void View_options()
    {
        moves_ui.SetActive(false);
        options_ui.SetActive(true); 
    }
    public void Select_player()
    {
        //enemy choosing player
        Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy = true;
        Current_pkm_Enemy = 0;
    }
    public void Select_enemy(int choice)
    {
        if(Turn_Based_Combat.instance.Current_pkm_turn>1)return;//not player's turn
        Battle_P[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy = true;
        Current_pkm_Enemy = choice;
    }
    private void Set_battle_ui()
    {
        Battle_ui.SetActive(true);
        OverWorld.SetActive(false);
    }
    void set_battle()
    {
        Options_manager.instance.playerInBattle = true;
        Set_battle_ui();
        Turn_Based_Combat.instance.Change_turn(-1,0);
    }
    public void Start_Battle(Pokemon enemy)//only ever be for wild battles
    {
        Battle_P[0].Current_Enemies[0] = enemy;
        Battle_P[2].pokemon = enemy;
        Wild_pkm.instance.pokemon = enemy;
        Set_pkm();
        Wild_pkm.instance.InBattle = true;
        set_battle();
    }
    public void Start_Battle(Pokemon[] enemies)//trainer battles, single and double
    {
        //double battle setup, 2v2 or 1v2
        is_trainer_battle = true;
        for(int i = 0; i < 2; i++)//double battle always has 2 enemies enter
        {
             Battle_P[i+2].pokemon = enemies[i];
             //set your 2 pokemon's enemies
             Battle_P[0].Current_Enemies[i] = enemies[i];
             Battle_P[1].Current_Enemies[i] = enemies[i];
        }
        Set_pkm();
        set_battle();
    }
    public void Set_pkm()
    {
        Battle_P[0].pokemon = Obj_Instance.set_Pokemon(Pokemon_party.instance.party[0]);
        if (Pokemon_party.instance.num_members > 1 && isDouble_battle)//if you have more than one pokemon send in another
        {
            Battle_P[1].pokemon = Obj_Instance.set_Pokemon(Pokemon_party.instance.party[1]);
        }
        for(int i = 0; i < 2; i++)
        {
            if (Battle_P[i].pokemon != null)
            {
                Battle_P[i + 2].Current_Enemies[i] = Battle_P[i].pokemon; //set enemies enemy equal to player's pkm
            }
        }
        Wild_pkm.instance.Enemy_pokemon = Battle_P[0].pokemon;
        Participant_count = 0;
        foreach(Battle_Participant p in Battle_P)
        {
            if (p.pokemon != null)
            {
                p.Load_ui();
                p.ability_h.Set_ability();
                Participant_count++;
            }
        }
    }
    void load_moves()
    {
        int j = 0;
        foreach(Move m in Battle_P[0].pokemon.move_set)
        {
            if (m != null)
            {
                moves[j].text = Battle_P[0].pokemon.move_set[j].Move_name;
                Move_btns[j].SetActive(true);
                j++;
            }
        }
        for (int i = j; i < 4; i++)//only show available moves
        {
            moves[i].text = "";
            Move_btns[i].SetActive(false);
        }
    }
    public void Use_Move(Move move,Pokemon user)
    {
        Doing_move = true;
        choosing_move = false;
        moves_ui.SetActive(false);
        options_ui.SetActive(false);
        Turn current_turn = new Turn(move, user, Battle_P[Current_pkm_Enemy].pokemon);
        Pkm_Use_Move use_move = new Pkm_Use_Move(current_turn);
        Turn_Based_Combat.instance.SaveMove(use_move);
    }
    private void Reset_move()
    {
        Selected_Move = false; 
        Move_btns[Current_Move].GetComponent<Button>().interactable = true;
        Current_Move = 0;
    }
    public void Select_Move(int move_num)
    {
        Reset_move();
        Current_Move = move_num-1;
        Move_pp.text = "PP: " + Battle_P[0].pokemon.move_set[Current_Move].Powerpoints+ "/" + Battle_P[0].pokemon.move_set[Current_Move].max_Powerpoints;
        Move_type.text = Battle_P[0].pokemon.move_set[Current_Move].type.Type_name;
        Selected_Move = true;
        Move_btns[Current_Move].GetComponent<Button>().interactable = false;
    }

    private void End_Battle(bool hasWon)
    {
        if (hasWon)
        {
            Dialogue_handler.instance.Write_Info(Game_Load.instance.player_data.Player_name + " won the battle", "Battle info");
        }
        //get money if trainer, display msg of how much money
        Options_manager.instance.playerInBattle = false;
        overworld_actions.instance.doing_action = false;
        Battle_ui.SetActive(false);
        options_ui.SetActive(false);
        for(int i=0;i<4;i++)
        {
            if (Battle_P[i] != null)
            {
                Battle_P[i].is_active = false;
            }
        }
        foreach (Battle_Participant p in Battle_P)
        {
            p.pokemon = null;
            p.Unload_ui();
        }
        Encounter_handler.instance.Reset_trigger();
        OverWorld.SetActive(true);
    }
    public void Run_away()//wild encounter is always single battle
    {
        running_away = true;
        if (!is_trainer_battle)
        {
            int random = Utility.Get_rand(1,11);
            if (Battle_P[0].pokemon.Current_level < Battle_P[0].Current_Enemies[0].Current_level)//lower chance if weaker
                random--;
            if (random > 5)//initially 50/50 chance to run
            {
                End_Battle(false);
                Dialogue_handler.instance.Write_Info(Game_Load.instance.player_data.Player_name + " ran away", "Battle info");
                Dialogue_handler.instance.Dialouge_off(.7f);
            }
            else
            {
                Dialogue_handler.instance.Write_Info("Can't run away","Battle info");
            }
        }
        else
        {
            Dialogue_handler.instance.Write_Info("Can't run away from trainer battle","Battle info");
        }
        Invoke(nameof(run_Off),1f);
    }
void run_Off()
{
    running_away = false;
}
    public void Fight()
    {
        choosing_move = true;
        viewing_options = false;
        options_ui.SetActive(false);
        moves_ui.SetActive(true);
        load_moves();
    }
    public void Check_win()//run every turn
    {
        int faint_count = 0;
        foreach (Pokemon p in Pokemon_party.instance.party)
        {
            if (p.HP <= 0)
            {
                faint_count++;
            }
        }
        if (faint_count == Pokemon_party.instance.num_members)
        {
            End_Battle(false);
            Dialogue_handler.instance.Write_Info("All your pokemon have fainted","Battle info");
        }
    }
}
