using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wild_pokemon_battle : MonoBehaviour
{
    public GameObject Battle_ui;
    public GameObject moves_ui;
    public GameObject options_ui;
    public Options_manager options_manager;
    public Slider player_hp;
    public Slider enemy_hp;
    public Slider player_exp;
    public Pokemon Currrent_pkm;
    public Pokemon Enemy_Currrent_pkm;
    public Text pkm_name,pkm_HP, enemy_name, pkm_lv, enemy_lv;
    public Text Move_pp, Move_type;
    public Image pkm_img, enemy_img;
    public Text[] moves;
    public GameObject[] Move_btns;
    private bool is_trainer_battle = false;
    public GameObject OverWorld;
    public bool viewing_options = false;
    public bool choosing_move = false;
    public bool running_away = false;
    void Update()
    {
        if (options_manager.playerInBattle)
        {
            if(choosing_move && (Input.GetKeyDown(KeyCode.Escape)))//exit move selection
            {
                options_ui.SetActive(true);
                moves_ui.SetActive(false);
                choosing_move = false;
            }
            if (options_manager.player.using_ui)
            {
                options_ui.SetActive(false);
                viewing_options = false;
            }
            else
            {
                if (!choosing_move && !running_away)
                {
                    viewing_options = true;//idle
                }
                else
                {
                    viewing_options = false;
                }
                if(viewing_options)
                    options_manager.dialogue.Write_Info("What will you do?", "Details");
                options_ui.SetActive(true);
            }
            //must be in update because pokemon hp can change in battle
            Slider_values(player_hp,Currrent_pkm);
            Exp_bar();
            Slider_values(enemy_hp,Enemy_Currrent_pkm);
        }
    }
    void Slider_values(Slider slider,Pokemon pkm)
    {
        slider.value = pkm.HP;
        slider.maxValue = pkm.max_HP;
        slider.minValue = 0;
    }
    void Exp_bar()
    {
        player_exp.value = Currrent_pkm.level_progress;
        player_exp.maxValue = 100;
        player_exp.minValue = 0;
    }

    void Load_ui()
    {
        pkm_img.sprite = Currrent_pkm.back_picture;
        enemy_img.sprite = Enemy_Currrent_pkm.front_picture;
        pkm_name.text = Currrent_pkm.Pokemon_name;
        pkm_lv.text = "Lv: "+Currrent_pkm.Current_level.ToString();
        enemy_name.text = Enemy_Currrent_pkm.Pokemon_name;
        enemy_lv.text = "Lv: "+Enemy_Currrent_pkm.Current_level.ToString();
        pkm_HP.text = Currrent_pkm.HP.ToString()+"/"+Currrent_pkm.max_HP.ToString();
    }
    public void Start_Battle(Pokemon wild_pkm)
    {
        if (wild_pkm.has_trainer)
            is_trainer_battle = true;
        Enemy_Currrent_pkm = wild_pkm;
        Currrent_pkm = options_manager.ins_manager.set_Pokemon(options_manager.party.party[0]);
        options_manager.playerInBattle = true;
        Battle_ui.SetActive(true);
        moves_ui.SetActive(false);
        options_ui.SetActive(true);
        Load_ui();
        OverWorld.SetActive(false);
    }

    void load_moves()
    {
        int j = 0;
        foreach(Move m in Currrent_pkm.move_set)
        {
            if (m != null)
            {
                moves[j].text = Currrent_pkm.move_set[j].Move_name;
                Move_btns[j].SetActive(true);
                j++;
            }
        }
        for (int i = j; i < 4; i++)
        {
            moves[i].text = "";
            Move_btns[i].SetActive(false);
        }
    }
    void Use_Move(Move move)
    {
        Debug.Log(move.Move_name+" : "+move.Move_damage);   //testing 
    }
    public void Select_Move(int move_num)
    { 
        move_num--;
        Move_pp.text = "PP: " + Currrent_pkm.move_set[move_num].Powerpoints.ToString() + "/" + Currrent_pkm.move_set[move_num].max_Powerpoints.ToString();;
        Move_type.text = Currrent_pkm.move_set[move_num].type.Type_name;
        Use_Move(Currrent_pkm.move_set[move_num]);
    }

    public void End_Battle(bool hasWon)
    {
        if (hasWon)//no money because it's an encounter
        {
            options_manager.dialogue.Write_Info(options_manager.player_data.Player_name + " won the battle", "Details");
        }
        options_manager.playerInBattle = false;
        options_manager.player.doing_action = false;
        Battle_ui.SetActive(false);
        options_ui.SetActive(false);
        Enemy_Currrent_pkm = null;
        Currrent_pkm = null;
        OverWorld.SetActive(true);
    }
    public void Run_away()
    {
        running_away = true;
        if (!is_trainer_battle)
        {
            int random = Random.Range(1, 11);
            if (Currrent_pkm.Current_level < Enemy_Currrent_pkm.Current_level)//lower chance if weaker
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
