using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Battle_handler : MonoBehaviour
{
    public GameObject Battle_ui;
    public GameObject moves_ui;
    public GameObject options_ui;
    public Battle_Participant[] Battle_Participants = {null,null,null,null};
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
    public Pokemon LastOpponent;
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
            if (Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy)
            {
                if(isDouble_battle)
                    Use_Move(Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set[Current_Move],
                        Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn]);//any of payer's 2 pkm using move
                else
                    Use_Move(Battle_Participants[0].pokemon.move_set[Current_Move],Battle_Participants[0]);//player using move
                choosing_move = false;
            }
            else
                Dialogue_handler.instance.Write_Info("Click on who you will attack", "Details");//details
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
                ResetAi();
                Dialogue_handler.instance.Write_Info("What will you do?", "Details");
                options_ui.SetActive(true);
            }
        }
        Battle_logic();
    }

    void ResetAi()
    {
        if (!is_trainer_battle)
            Wild_pkm.instance.Invoke(nameof(Wild_pkm.instance.Can_Attack),1f);
        else
            foreach(Battle_Participant p in Battle_Participants)
                if (p.pokemon != null & !p.isPlayer)
                    p.trainer.Invoke(nameof(p.trainer.Can_Attack),1f);
    }
    void Battle_logic()
    {
        if (!isDouble_battle && Turn_Based_Combat.instance.Current_pkm_turn == 0)//if single battle, auto aim at enemy
        {
            Current_pkm_Enemy = 2;
            Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy = true;
        }
    }

    public void View_options()
    {
        if (BattleOver) return;
        moves_ui.SetActive(false);
        options_ui.SetActive(true); 
    }
    public void Select_enemy(int choice)
    {
        if(Turn_Based_Combat.instance.Current_pkm_turn>1)return;//not player's turn
        if (isDouble_battle & choice < 2)
        {//cant attack own pokemon
            Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy = false;
            return;
        }
        Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].Selected_Enemy = true;
        Current_pkm_Enemy = choice;
    }
    private void Set_battle_ui()
    {
        Battle_ui.SetActive(true);
        OverWorld.SetActive(false);
    }
    void set_battle()
    {
        levelUpQueue.Clear();
        Options_manager.instance.playerInBattle = true;
        Set_battle_ui();
        Turn_Based_Combat.instance.Change_turn(-1,0);
    }

    void Load_Area_bg(Encounter_Area area)
    {
        foreach (GameObject g in Backgrounds)
            if (g.name==area.Biome_name.ToLower())
                g.SetActive(true);
            else
                g.SetActive(false);
    }
    public void StartWildBattle(Pokemon enemy)//only ever be for wild battles
    {
        BattleOver = false;
        is_trainer_battle = false;
        isDouble_battle = false;
        Load_Area_bg(Encounter_handler.instance.current_area);
        Battle_Participants[0].pokemon = Pokemon_party.instance.party[0];
        Battle_Participants[0].Current_Enemies.Add(Battle_Participants[2]);
        Battle_Participants[2].pokemon = enemy;
        Battle_Participants[2].Current_Enemies.Add(Battle_Participants[0]);
        Wild_pkm.instance.pokemon_participant = Battle_Participants[2];
        Wild_pkm.instance.Enemy_pokemon = Battle_Participants[0];
        Battle_Participants[2].AddToExpList(Battle_Participants[0].pokemon);
        foreach(Battle_Participant p in Battle_Participants)
            if (p.pokemon != null)
                Set_participants(p);
        Wild_pkm.instance.InBattle = true;
        set_battle();
        Encounter_handler.instance.current_area = null;
    }
    public void SetBattleType(List<string>trainerNames,string battleType)
    {
        switch (battleType)
        {
            case "single": 
                StartSingleBattle(trainerNames[0]);
                break;
            case "single-double": 
                StartSingleDoubleBattle(trainerNames[0]);
                break;
            case "double": 
                //StartDoubleBattle(trainerNames);
                break;
        }
    }
    private void StartSingleBattle(string trainerName)//single trainer battle
    {
        BattleOver = false;
        is_trainer_battle = true;
        isDouble_battle = false;
        Battle_Participants[0].pokemon = Pokemon_party.instance.party[0];
        Battle_Participants[0].Current_Enemies.Add(Battle_Participants[2]);
        Battle_Participants[2].Current_Enemies.Add(Battle_Participants[0]);
        Battle_Participants[2].trainer = Battle_Participants[2].GetComponent<Enemy_trainer>();
        Battle_Participants[2].trainer.StartBattle(trainerName,false);
        Battle_Participants[2].pokemon = Battle_Participants[2].trainer.TrainerParty[0];
        Load_Area_bg(Battle_Participants[2].trainer._TrainerData.TrainerLocation);
        Battle_Participants[2].AddToExpList(Battle_Participants[0].pokemon);
        foreach(Battle_Participant p in Battle_Participants)
            if (p.pokemon != null)
                Set_participants(p);
        Battle_Participants[2].trainer.InBattle = true;
        set_battle();
    }
    /*public void StartDoubleBattle(List<string> trainerNames)//trainer double battle
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
    private void StartSingleDoubleBattle(string trainerName)//trainer single double battles
    {
        BattleOver = false;
        is_trainer_battle = true;
        isDouble_battle = true;
        List<Pokemon> Alive_pkm=new();
        foreach (Pokemon p in Pokemon_party.instance.party)
            if (p != null && p.HP > 0)
                Alive_pkm.Add(p);
        Battle_Participants[0].pokemon = Alive_pkm[0];
        if(Pokemon_party.instance.num_members>1)
            Battle_Participants[1].pokemon = Alive_pkm[1];
        Battle_Participants[2].trainer = Battle_Participants[2].GetComponent<Enemy_trainer>();
        Battle_Participants[3].trainer = Battle_Participants[3].GetComponent<Enemy_trainer>();
        Battle_Participants[2].trainer.StartBattle(trainerName,false);
        Battle_Participants[3].trainer.StartBattle(trainerName,true);
        Battle_Participants[3].trainer._TrainerData = Battle_Participants[2].trainer._TrainerData;
        Battle_Participants[3].trainer.TrainerParty = Battle_Participants[2].trainer.TrainerParty;
        Battle_Participants[2].pokemon = Battle_Participants[2].trainer.TrainerParty[0];
        Battle_Participants[3].pokemon = Battle_Participants[3].trainer.TrainerParty[1]; 
        for(int i = 0; i < 2; i++)//double battle always has 2 enemies enter
        {
              if(Battle_Participants[i + 2].pokemon!=null)
              {
                  Battle_Participants[0].Current_Enemies.Add(Battle_Participants[i + 2]);
                  if (Pokemon_party.instance.num_members > 1)
                      Battle_Participants[1].Current_Enemies.Add(Battle_Participants[i + 2]);
              }
              if (Battle_Participants[i].pokemon != null)
              {
                  Battle_Participants[2].Current_Enemies.Add(Battle_Participants[i]);
                  Battle_Participants[3].Current_Enemies.Add(Battle_Participants[i]);
              }
        }
        for (int i = 0; i < 2; i++)
            if (Battle_Participants[i].pokemon != null)
                foreach (Battle_Participant enemy  in Battle_Participants[i].Current_Enemies)
                    enemy.AddToExpList(Battle_Participants[i].pokemon);
        foreach(Battle_Participant p in Battle_Participants)
            if (p.pokemon != null)
                Set_participants(p);
        Battle_Participants[2].trainer.InBattle = true;
        Battle_Participants[3].trainer.InBattle = true; 
        Load_Area_bg(Battle_Participants[2].trainer._TrainerData.TrainerLocation);
        set_battle();
    }
    public void Set_participants(Battle_Participant Participant)
    {
        List<Pokemon> Alive_pkm=new();
        if (Participant.isPlayer)
        { //for switch-ins
            if (Pokemon_party.instance.Swapping_in || Pokemon_party.instance.SwapOutNext)
            {
                foreach (Pokemon p in Pokemon_party.instance.party)
                    if (p != null && p.HP > 0)
                        Alive_pkm.Add(p);
                Participant.pokemon = Alive_pkm[Pokemon_party.instance.Selected_member - 1];
                foreach (Battle_Participant enemyParticipant  in Participant.Current_Enemies)
                    enemyParticipant.AddToExpList(Participant.pokemon);
            }
        }
        else
        {//add player participants to get exp from switched in enemy
            foreach (Battle_Participant playerParticipant  in Participant.Current_Enemies)
                Participant.AddToExpList(playerParticipant.pokemon);
        }
        Participant.data.save_stats();
        Participant.Load_ui();
        Participant.ability_h.Set_ability();
        check_Participants();
    }
    public void check_Participants()
    {
        Participant_count = 0;
        foreach (Battle_Participant p in Battle_Participants)
            if(p.pokemon!=null)
                Participant_count++;
    }
    public void reload_participant_ui()
    {
        foreach(Battle_Participant p in Battle_Participants)
            if (p.pokemon != null)
                p.refresh_statusIMG();
    }
    void load_moves()
    {
        int j = 0;
        foreach(Move m in Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set)
            if (m != null)
            {
                moves[j].text = Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set[j].Move_name;
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
        Turn current_turn = new Turn(move, Array.IndexOf(Battle_Participants,user), Current_pkm_Enemy, user.pokemon.Pokemon_ID.ToString(),
            Battle_Participants[Current_pkm_Enemy].pokemon.Pokemon_ID.ToString());
        Turn_Based_Combat.instance.SaveMove(current_turn);
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
        Move_pp.text = "PP: " + Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set[Current_Move].Powerpoints+ "/" 
                       + Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set[Current_Move].max_Powerpoints;
        if (Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set[Current_Move].Powerpoints == 0)
            Move_pp.color = Color.red;
        else
            Move_pp.color = Color.black;
        Move_type.text = Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon.move_set[Current_Move].type.Type_name;
        Selected_Move = true;
        Move_btns[Current_Move].GetComponent<Button>().interactable = false;
    }
    int MoneyModifier()
    {
        foreach(Pokemon p in Pokemon_party.instance.party)
            if (p != null && p.HP>0 && p.HeldItem!=null)
                if(p.HeldItem.Item_name == "Amulet Coin")
                    return 2;
        return 1;
    }
    IEnumerator DelayBattleEnd()
    {
        yield return new WaitUntil(() => levelUpQueue.Count==0);
        yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        if (running_away)
            Dialogue_handler.instance.Battle_Info(Game_Load.instance.player_data.Player_name + " ran away");
        else
        {
            int BaseMoneyPayout=0;
            if(is_trainer_battle)
                BaseMoneyPayout=Battle_Participants[0].Current_Enemies[0].trainer._TrainerData.BaseMoneyPayout;
            if (BattleWon)
            {
                Dialogue_handler.instance.Battle_Info(Game_Load.instance.player_data.Player_name + " won the battle");
                if (is_trainer_battle)
                {
                    int MoneyGained = BaseMoneyPayout * LastOpponent.Current_level * MoneyModifier();
                    Game_Load.instance.player_data.player_Money += MoneyGained;
                    Dialogue_handler.instance.Battle_Info(Game_Load.instance.player_data.Player_name + " recieved P" + MoneyGained);
                }
            }
            else
            {
                if (is_trainer_battle)
                {
                    LastOpponent = Battle_Participants[0].Current_Enemies[0].pokemon;
                    Game_Load.instance.player_data.player_Money -= BaseMoneyPayout * Game_Load.instance.player_data.NumBadges
                                                                   * LastOpponent.Current_level;}
                if(!Wild_pkm.instance.RanAway)
                {
                    Dialogue_handler.instance.Battle_Info("All your pokemon have fainted");
                    Area_manager.instance.Switch_Area("Poke Center", 0f);
                }
            }
        }
        yield return new WaitForSeconds(2f);
        end_battle_ui();
        yield return null;
    }
    public void LevelUpEvent(Pokemon pkm)
    {
        LevelUpEvent PkmLevelUp=new LevelUpEvent(pkm);
        levelUpQueue.Add(PkmLevelUp);
        StartCoroutine(LevelUp_Sequence(PkmLevelUp));
    } 
    IEnumerator LevelUp_Sequence(LevelUpEvent pkmLevelUp)
    {
        yield return new WaitUntil(() => !Turn_Based_Combat.instance.LevelEventDelay);
        Turn_Based_Combat.instance.LevelEventDelay = true;
        yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        Dialogue_handler.instance.Battle_Info(pkmLevelUp.pokemon.Pokemon_name+" leveled up!");
        yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        pkmLevelUp.Execute();
        yield return new WaitForSeconds(0.5f);
        if (PokemonOperations.LearningNewMove)
        {
            if (pkmLevelUp.pokemon.move_set.Count > 3)
            {
                yield return new WaitUntil(() => Options_manager.instance.SelectedNewMoveOption);
                yield return new WaitForSeconds(0.5f);
                if (Pokemon_Details.instance.LearningMove)
                    yield return new WaitUntil(() => !Pokemon_Details.instance.LearningMove);
                else
                    Turn_Based_Combat.instance.LevelEventDelay = false;
            }
            yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        }
        else
        //incase if leveled up and didnt learn move
            levelUpQueue.Remove(pkmLevelUp);
        Turn_Based_Combat.instance.LevelEventDelay = false;
        if(BattleOver & levelUpQueue.Count==0)
            End_Battle(BattleWon);
        yield return null;
    }
    public void End_Battle(bool Haswon)
    {
        BattleWon = Haswon;
        BattleOver = true;
        StartCoroutine(DelayBattleEnd());
    }
    void end_battle_ui()
    {
        onBattleEnd?.Invoke();
        Dialogue_handler.instance.Dialouge_off();
        Options_manager.instance.playerInBattle = false;
        overworld_actions.instance.doing_action = false;
        Battle_ui.SetActive(false);
        options_ui.SetActive(false);
        LastOpponent = null;
        foreach (Battle_Participant p in Battle_Participants)
            if(p.pokemon!=null)
            {
                p.data.Load_Stats();
                p.data.Reset_Battle_state(p.pokemon,true);
                p.pokemon = null;
                p.previousMove = "";
                p.Unload_ui();
                if (p.trainer != null)
                    p.trainer.InBattle = false;
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
            if (Battle_Participants[0].pokemon.Current_level < Battle_Participants[0].Current_Enemies[0].pokemon.Current_level)//lower chance if weaker
                random--;
            if (random > 5) //initially 50/50 chance to run
            {
                Wild_pkm.instance.InBattle = false;
                End_Battle(false);
            }
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
