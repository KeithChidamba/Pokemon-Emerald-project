using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Battle_handler : MonoBehaviour
{
    public GameObject Battle_ui;
    public GameObject moves_ui;
    public GameObject options_ui;
    public Options_manager options_manager;
    public Battle_Participant[] Battle_P = {null,null,null,null};
    public Text Move_pp, Move_type;
    public Text[] moves;
    public GameObject[] Move_btns;
    public bool is_trainer_battle = false;
    public bool isDouble_battle = false;
    public GameObject OverWorld;
    public bool viewing_options = false;
    public bool choosing_move = false;
    public bool running_away = false;
    public bool Selected_Move = false;
    private int Current_Move = 0;
    public bool Doing_move = false;
    public Move_handler move_h;
    void Update()
    {
        if (options_manager.playerInBattle)
        {
            if (Selected_Move &&(Input.GetKeyDown(KeyCode.F)))
            {
                Use_Move(Battle_P[0].pokemon.move_set[Current_Move]);
            }
            if(choosing_move && (Input.GetKeyDown(KeyCode.Escape)))//exit move selection
            {
                options_ui.SetActive(true);
                moves_ui.SetActive(false);
                choosing_move = false; 
                Selected_Move = false; 
                Current_Move = 0;
                Move_btns[Current_Move].GetComponent<Button>().interactable = true;
            }
            if (options_manager.player.using_ui)
            {
                options_ui.SetActive(false);
                viewing_options = false;
            }
            else
            {
                if (!choosing_move && !running_away && !Doing_move)
                {
                    viewing_options = true;//idle
                }
                else
                {
                    viewing_options = false;
                }
                if (viewing_options)
                {
                    options_manager.dialogue.Write_Info("What will you do?", "Details");
                    options_ui.SetActive(true);
                }
            }
        }
    }
    public void Switch_In(int participant)
    {
        //refresh current pokemon after switch in
        Battle_P[participant].pokemon = options_manager.ins_manager.set_Pokemon(options_manager.party.party[0]);
        for(int i = 0; i < 2; i++)
        {
            if (Battle_P[i].pokemon != null)
            {
                Battle_P[i + 2].Current_Enemies[i] = Battle_P[i].pokemon;
            }
        }
    }
    public void Start_Battle(Pokemon enemy)
    {
        isDouble_battle = false;
        //single battle setup
        if (enemy.has_trainer)
            is_trainer_battle = true;
        Battle_P[0].Current_Enemies[0] = enemy;
        Battle_P[2].pokemon = enemy;
        Switch_In(0);
        Switch_In(2);
        Battle_P[2].Current_Enemies[0] = Battle_P[0].pokemon;
        options_manager.playerInBattle = true;
        Battle_ui.SetActive(true);
        moves_ui.SetActive(false);
        options_ui.SetActive(true);
        Battle_P[0].Load_ui();
        Battle_P[0].ability_h.Set_ability();
        Battle_P[2].Load_ui();
        Battle_P[2].ability_h.Set_ability();
        OverWorld.SetActive(false);
    }
    public void Start_Battle(Pokemon[] enemies)
    {
        isDouble_battle = true;
        //double battle setup, 2v2 or 1v2
        int participant_count = 2;//first 2 spaces are player's pokemon
        if (enemies[0].has_trainer)
            is_trainer_battle = true;
        Battle_P[0].pokemon = options_manager.ins_manager.set_Pokemon(options_manager.party.party[0]);
        if (options_manager.party.num_members > 1)//if you have more than one pokemon send in another
        {
            Battle_P[1].pokemon = options_manager.ins_manager.set_Pokemon(options_manager.party.party[1]);
        }
        for(int i = 0; i < 2; i++)//double battle always has 2 enemies enter
        {
            Battle_P[participant_count].pokemon = enemies[i];
            participant_count++;
            //set to first enemy, enemies can be selected when attacking
            Battle_P[0].Current_Enemies[i] = enemies[i];
            Battle_P[1].Current_Enemies[i] = enemies[i];
        }
        for(int i = 0; i < 2; i++)
        {
            if (Battle_P[i].pokemon != null)
            {
                Battle_P[i + 2].Current_Enemies[i] = Battle_P[i].pokemon; //set enemies enemy equal to player's pkm
            }
        }
        options_manager.playerInBattle = true;
        Battle_ui.SetActive(true);
        moves_ui.SetActive(false);
        options_ui.SetActive(true);
        foreach(Battle_Participant p in Battle_P)
        {
            if (p != null)
            {
                p.Load_ui();
                p.ability_h.Set_ability();
            }
        }
        OverWorld.SetActive(false);
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
    void Use_Move(Move move)
    {
        Doing_move = true;
        choosing_move = false;
        moves_ui.SetActive(false);
        options_ui.SetActive(false);
       // move_h.Do_move(move,Battle_P[0].pokemon,);//selected enemy
    }
    public void Reset_move()
    {
        Selected_Move = false; 
        Move_btns[Current_Move].GetComponent<Button>().interactable = true;
        Current_Move = 0;
    }
    public void Select_Move(int move_num)
    { 
        Current_Move = move_num-1;
        Move_pp.text = "PP: " + Battle_P[0].pokemon.move_set[Current_Move].Powerpoints.ToString() + "/" + Battle_P[0].pokemon.move_set[Current_Move].max_Powerpoints.ToString();
        Move_type.text = Battle_P[0].pokemon.move_set[Current_Move].type.Type_name;
        Selected_Move = true;
        Move_btns[Current_Move].GetComponent<Button>().interactable = false;
    }

    private void End_Battle(bool hasWon)
    {
        if (hasWon)
        {
            options_manager.dialogue.Write_Info(options_manager.player_data.Player_name + " won the battle", "Details");
        }
        //get money if trainer, display msg of how much money
        options_manager.playerInBattle = false;
        options_manager.player.doing_action = false;
        Battle_ui.SetActive(false);
        options_ui.SetActive(false);
        for(int i=0;i<4;i++)//skip first because that is the player, which has different ui
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
        OverWorld.SetActive(true);
    }
    public void Run_away()//wild encounter is always single battle
    {
        running_away = true;
        if (!is_trainer_battle)
        {
            int random = Random.Range(1, 11);
            if (Battle_P[0].pokemon.Current_level < Battle_P[0].Current_Enemies[0].Current_level)//lower chance if weaker
                random--;
            if (random > 5)//initially 50/50 chance to run
            {
                End_Battle(false);
                options_manager.dialogue.Write_Info(options_manager.player_data.Player_name + " ran away", "Details");
                options_manager.dialogue.Dialouge_off(.7f);
            }
            else
            {
                options_manager.dialogue.Write_Info("Can't run away","Details");
            }
        }
        else
        {
            options_manager.dialogue.Write_Info("Can't run away from trainer battle","Details");
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
        foreach (Pokemon p in options_manager.party.party)
        {
            if (p.HP <= 0)
            {
                faint_count++;
            }
        }
        if (faint_count == options_manager.party.num_members)
        {
            End_Battle(false);
            options_manager.dialogue.Write_Info("All your pokemon have fainted","Details");
        }
    }
}