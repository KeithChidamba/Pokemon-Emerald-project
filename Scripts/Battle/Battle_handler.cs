using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Battle_handler : MonoBehaviour
{
    public GameObject Battle_ui;
    public GameObject moves_ui;
    public GameObject options_ui;
    public Battle_Participant[] Battle_P = {null,null,null,null};
    public List<Pokemon> exp_recievers;
    public List<LevelUpEvent> levelUpQueue=new();
    public Text Move_pp, Move_type;
    public Text[] moves;
    public GameObject[] Move_btns;
    public bool is_trainer_battle = false;
    public bool isDouble_battle = false;
    public int Participant_count = 0;
    public bool displaying_info = false;
    public bool BattleOver = false;
    public bool BattleWon = false;
    public GameObject OverWorld;
    public List<GameObject> Backgrounds;
    public bool viewing_options = false;
    public bool choosing_move = false;
    public bool running_away = false;
    public bool Selected_Move = false;
    private int Current_Move = 0;
    public int Current_pkm_Enemy = 0;
    public bool Doing_move = false;
    public static Battle_handler instance;
    public event Action onBattleEnd;
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
                    Use_Move(Battle_P[Current_Move].pokemon.move_set[Current_Move],Battle_P[Current_Move]);//any of payer's 2 pkm using move
                else
                    Use_Move(Battle_P[0].pokemon.move_set[Current_Move],Battle_P[0]);//player using move
                choosing_move = false;
            }
            else
            {
                displaying_info = true;
                Dialogue_handler.instance.Write_Info("Click on who you will attack", "Battle info");//details
            }
        }
        if(choosing_move && (Input.GetKeyDown(KeyCode.Escape)))//exit move selection
        {
            View_options();
            choosing_move = false;
            Reset_move();
        }

        if (displaying_info || BattleOver)
        {
            options_ui.SetActive(false);
            viewing_options = false;
        }
        if (overworld_actions.instance.using_ui)
        {
            Wild_pkm.instance.CanAttack = false;
            options_ui.SetActive(false);
            viewing_options = false;
        }
        else
        {
            if (!choosing_move && !running_away && !Doing_move && !displaying_info && !Doing_move && !BattleOver)
                viewing_options = true;
            else
                viewing_options = false;
            if (viewing_options)
            { 
                Wild_pkm.instance.Invoke(nameof(Wild_pkm.instance.Can_Attack),1f);
                Dialogue_handler.instance.Write_Info("What will you do?", "Details");
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
        if (BattleOver) return;
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

    void Load_Area_bg()
    {
        foreach (GameObject g in Backgrounds)
            if (g.name==Encounter_handler.instance.current_area.Biome_name.ToLower())
                g.SetActive(true);
            else
                g.SetActive(false);
        Encounter_handler.instance.current_area = null;
    }
    public void Start_Battle(Pokemon enemy)//only ever be for wild battles
    {
        BattleOver = false;
        Load_Area_bg();
        Battle_P[0].pokemon = Pokemon_party.instance.party[0];
        Battle_P[0].Current_Enemies.Add(Battle_P[2]);
        Battle_P[2].pokemon = enemy;
        Wild_pkm.instance.pokemon_participant = Battle_P[2];
        levelUpQueue.Clear();
        AddToExpList(Battle_P[0].pokemon);
        foreach(Battle_Participant p in Battle_P)
            if (p.pokemon != null)
                Set_participants(p);
        Wild_pkm.instance.InBattle = true;
        set_battle();
    }
    /*public void Start_Battle(Pokemon[] enemies)//trainer battles, single and double
    {
        //double battle setup, 2v2 or 1v2
        BattleOver = false;
        Load_Area_bg();
        is_trainer_battle = true;
        for(int i = 0; i < 2; i++)//double battle always has 2 enemies enter
        {
             Battle_P[i+2].pokemon = enemies[i];
             //set your 2 pokemon's enemies
             Battle_P[0].Current_Enemies.Add(Battle_P[i + 2]);
             Battle_P[1].Current_Enemies.Add(Battle_P[i + 2]);
        }
        //Set_participants();
        set_battle();
    }*/
    public void Set_participants(Battle_Participant Participant)
    {
        if(!is_trainer_battle)
            Wild_pkm.instance.Enemy_pokemon = Battle_P[0];
        List<Pokemon> Alive_pkm=new();
        Participant.data.save_stats();
        Participant.Load_ui();
        Participant.ability_h.Set_ability();
        check_Participants();
        if (!Participant.isPlayer) return;
        //for switch-ins
        if (!Pokemon_party.instance.Swapping_in & !Pokemon_party.instance.SwapOutNext) return;
        foreach(Pokemon p in Pokemon_party.instance.party)
            if (p != null && p.HP>0)
                Alive_pkm.Add(p); 
        Participant.pokemon = Alive_pkm[0];//doesnt account for double battle, use current turn 
        AddToExpList(Participant.pokemon);
    }
    void check_Participants()
    {
        Participant_count = 0;
        foreach (Battle_Participant p in Battle_P)
            if(p.pokemon!=null)
                Participant_count++;
    }
    public void reload_participant_ui()
    {
        foreach(Battle_Participant p in Battle_P)
            if (p.pokemon != null)
                p.refresh_statusIMG();
    }
    void load_moves()
    {
        int j = 0;
        foreach(Move m in Battle_P[0].pokemon.move_set)
            if (m != null)
            {
                moves[j].text = Battle_P[0].pokemon.move_set[j].Move_name;
                Move_btns[j].SetActive(true);
                j++;
            }
        for (int i = j; i < 4; i++)//only show available moves
        {
            moves[i].text = "";
            Move_btns[i].SetActive(false);
        }
    }
    public void Use_Move(Move move,Battle_Participant user)
    {
        if(move.Powerpoints==0)return;
        move.Powerpoints--;
        Doing_move = true;
        choosing_move = false;
        moves_ui.SetActive(false);
        options_ui.SetActive(false);
        Turn current_turn = new Turn(move, user, Battle_P[Current_pkm_Enemy]);
        Pkm_Use_Move use_move = new Pkm_Use_Move(current_turn);
        Turn_Based_Combat.instance.SaveMove(use_move);
    }
    public void Reset_move()
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
        if (Battle_P[0].pokemon.move_set[Current_Move].Powerpoints == 0)
            Move_pp.color = Color.red;
        else
            Move_pp.color = Color.black;
        Move_type.text = Battle_P[0].pokemon.move_set[Current_Move].type.Type_name;
        Selected_Move = true;
        Move_btns[Current_Move].GetComponent<Button>().interactable = false;
    }
    IEnumerator DelayBattleEnd()
    {
        yield return new WaitUntil(() => levelUpQueue.Count==0);
        yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        if (running_away)
            Dialogue_handler.instance.Battle_Info(Game_Load.instance.player_data.Player_name + " ran away");
        else
            if (BattleWon)
                Dialogue_handler.instance.Battle_Info(Game_Load.instance.player_data.Player_name + " won the battle");
            else
            {
                Dialogue_handler.instance.Battle_Info("All your pokemon have fainted");
                Area_manager.instance.Switch_Area("Poke Center",0f);
            }
        yield return new WaitForSeconds(2f);
        end_battle_ui();
        yield return null;
    }
    public void LevelUpEvent(Pokemon pkm)
    {
        levelUpQueue.Add(new LevelUpEvent(pkm));
        StartCoroutine(LevelUp_Sequence());
    } 
    IEnumerator LevelUp_Sequence()
    {
        foreach (LevelUpEvent pkm in new List<LevelUpEvent>(levelUpQueue))
        {
            yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
            Dialogue_handler.instance.Battle_Info(pkm.pokemon.Pokemon_name+" leveled up!");
            yield return new WaitUntil(() => !displaying_info);
            pkm.Execute();
            yield return new WaitForSeconds(.5f);
            if (PokemonOperations.LearningNewMove)
                if (pkm.pokemon.move_set.Count > 3)
                {
                    yield return new WaitUntil(() => Options_manager.instance.ChoosingNewMove);
                    yield return new WaitForSeconds(.5f);
                    if (Pokemon_Details.instance.LearningMove)
                    {
                        yield return new WaitUntil(() => !Pokemon_Details.instance.LearningMove);
                        yield return new WaitForSeconds(1f);
                    }
                }
        }
        yield return new WaitForSeconds(.5f);
        levelUpQueue.Clear();
        if(BattleOver)
            End_Battle(BattleWon);
        yield return null;
    }
    public void End_Battle(bool Haswon)
    {
        BattleWon = Haswon;
        BattleOver = true;
        StartCoroutine(DelayBattleEnd());
    }
    void AddToExpList(Pokemon pkm)
    {
        if(!exp_recievers.Contains(pkm))
            exp_recievers.Add(pkm);
    }
    public void Distribute_EXP(int exp_from_enemy)
    {
        exp_recievers.RemoveAll(p => p.HP <= 0);
        if(exp_recievers.Count<1)return;
        Debug.Log(exp_from_enemy+" exp from enemy");
        if (exp_recievers.Count == 1)//let the pokemon with exp share get all exp if it fought alone
        {
            exp_recievers[0].Recieve_exp(exp_from_enemy);
            exp_recievers.Clear();
            return;
        }
        foreach(Pokemon p in Pokemon_party.instance.party)//exp share split, assuming there's only ever 1 exp share in the game
            if (p != null && p.HP>0 && p.HeldItem!=null)
                if(p.HeldItem.Item_name == "Exp Share")
                {
                    p.Recieve_exp(exp_from_enemy / 2);
                    Debug.Log(p.Pokemon_name + " recieved " + exp_from_enemy / 2f + " exp using exp share");
                    exp_from_enemy /= 2;
                    break;
                }
        int exp = exp_from_enemy / exp_recievers.Count;
        foreach (Pokemon p in exp_recievers)
        {
            if(p.HeldItem == null)
                p.Recieve_exp(exp);
            else
                if (p.HeldItem.Item_name != "Exp Share")
                    p.Recieve_exp(exp);
        }
        exp_recievers.Clear();
    }
    void end_battle_ui()
    {
        onBattleEnd?.Invoke();
        Dialogue_handler.instance.Dialouge_off();
        Options_manager.instance.playerInBattle = false;
        overworld_actions.instance.doing_action = false;
        Battle_ui.SetActive(false);
        options_ui.SetActive(false);
        foreach (Battle_Participant p in Battle_P)
            if(p.pokemon!=null)
            {
                p.data.Load_Stats();
                p.data.Reset_Battle_state(p.pokemon,true);
                p.pokemon = null;
                p.Unload_ui();
            }
        Encounter_handler.instance.Reset_trigger();
        OverWorld.SetActive(true);
        Dialogue_handler.instance.can_exit = true;
        BattleWon = false;
        BattleOver = false;
        StopAllCoroutines();
    }
    public void Run_away()//wild encounter is always single battle
    {
        running_away = true;
        displaying_info = true;
        if (!is_trainer_battle)
        {
            int random = Utility.Get_rand(1,11);
            if (Battle_P[0].pokemon.Current_level < Battle_P[0].Current_Enemies[0].pokemon.Current_level)//lower chance if weaker
                random--;
            if (random > 5)//initially 50/50 chance to run
                End_Battle(false);
            else
            {
                Dialogue_handler.instance.Battle_Info("Can't run away");
                Turn_Based_Combat.instance.Invoke(nameof(Turn_Based_Combat.instance.Next_turn),0.9f);
            }
        }
        else
            Dialogue_handler.instance.Battle_Info("Can't run away from trainer battle");
        Invoke(nameof(run_Off),1f);
    }
void run_Off()
{
    Wild_pkm.instance.InBattle = false;
    running_away = false;
    displaying_info = false;
}
    public void Fight()
    {
        choosing_move = true;
        viewing_options = false;
        options_ui.SetActive(false);
        moves_ui.SetActive(true);
        load_moves();
    }
}
