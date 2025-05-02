using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Battle_Participant : MonoBehaviour
{
    public AbilityHandler ability_h;
    public Participant_Status status;
    public Enemy_trainer trainer;
    public Battle_Data data;
    public Pokemon pokemon;
    public List<Battle_Participant> Current_Enemies;
    public Image pkm_img;
    public Image status_img;
    public Image Gender_img;
    public Text pkm_name, pkm_HP, pkm_lv;
    public bool isPlayer = false;
    public bool is_active = false;
    public bool fainted = false;
    public Slider player_hp;
    public Slider player_exp;
    public GameObject[] single_battle_ui;
    public GameObject[] Double_battle_ui;
    public GameObject participant_ui;
    public bool Selected_Enemy = false;
    public string previousMove="";
    public Type AddtionalTypeImmunity;
    public List<Pokemon> exp_recievers;
    public bool CanEscape = true;
    private void Start()
    {
        status = GetComponent<Participant_Status>();
        data = GetComponent<Battle_Data>();
        Turn_Based_Combat.instance.OnTurnEnd += Check_Faint;
        Move_handler.instance.OnMoveEnd += Check_Faint;
        Turn_Based_Combat.instance.OnMoveExecute += Check_Faint;
        Battle_handler.instance.onBattleEnd += Deactivate_pkm;
    }
    private void Update()
    {
        if (!is_active) return;
        update_ui();
    }
    private void Give_exp()
    {
        Distribute_EXP(pokemon.Calc_Exp(pokemon));
    }
    private void Give_EVs(Battle_Participant enemy)
    {
        foreach (string ev in pokemon.EVs)
        {
            float evAmount = float.Parse(ev.Substring(ev.Length - 1, 1));
            string evStat = ev.Substring(0 ,ev.Length - 1);
            PokemonOperations.GetEV(evStat,evAmount,enemy.pokemon);
        }
    }
    public  void AddToExpList(Pokemon pkm)
    {
        if(!exp_recievers.Contains(pkm))
            exp_recievers.Add(pkm);
    }
    private void Distribute_EXP(int exp_from_enemy)
    {
        exp_recievers.RemoveAll(p => p.HP <= 0);
        if(exp_recievers.Count<1)return;
        if (exp_recievers.Count == 1)//let the pokemon with exp share get all exp if it fought alone
        {
            exp_recievers[0].Recieve_exp(exp_from_enemy);
            exp_recievers.Clear();
            return;
        }
        foreach(Pokemon p in Pokemon_party.instance.party)//exp share split, assuming there's only ever 1 exp share in the game
            if (p != null && p.HP>0 && p.HasItem)
                if(p.HeldItem.Item_name == "Exp Share")
                {
                    p.Recieve_exp(exp_from_enemy / 2);
                    exp_from_enemy /= 2;
                    break;
                }
        int exp = exp_from_enemy / exp_recievers.Count;
        foreach (Pokemon p in exp_recievers)
        {
            if(!p.HasItem)
                p.Recieve_exp(exp);
            else
            if (p.HeldItem.Item_name != "Exp Share")
                p.Recieve_exp(exp);
        }
        exp_recievers.Clear();
    }
    public void Check_Faint()
    {
        if (pokemon != null)
            if(pokemon.HP > 0 & !is_active)
            {is_active = true;participant_ui.SetActive(true);}
        if (!is_active) return;
        fainted = (pokemon.HP <= 0);
        if (pokemon.HP > 0) return;
        Turn_Based_Combat.instance.FainEventDelay = true;
        Dialogue_handler.instance.Battle_Info(pokemon.Pokemon_name+" fainted!");
        pokemon.Status_effect = "None";
        Give_exp();
        foreach (Battle_Participant enemy in Current_Enemies)
            if(enemy.pokemon!=null)
                Give_EVs(enemy);
        if (isPlayer)
            Invoke(nameof(Check_loss),1f);
        else
            if (!Battle_handler.instance.is_trainer_battle)
                Invoke(nameof(EndWildBattle),1f);
            else
            {
                Reset_pkm();
                trainer.Invoke(nameof(trainer.CheckLoss),1f);
            }
    }
    public void EndWildBattle()
    {
        Wild_pkm.instance.InBattle = false;
        Turn_Based_Combat.instance.FainEventDelay = false;
        Battle_handler.instance.End_Battle(true);
    }
    private void Check_loss()
    {
        int numAlive=Pokemon_party.instance.num_members;
        for(int i=0;i<Pokemon_party.instance.num_members;i++)
            if (Pokemon_party.instance.party[i].HP <= 0)
                numAlive--;
        if (numAlive==0)
        {
            Battle_handler.instance.End_Battle(false);
            if(!Battle_handler.instance.is_trainer_battle)
                Wild_pkm.instance.InBattle = false;
        }
        else
        {//select next pokemon to switch in
            if ( (Battle_handler.instance.isDouble_battle && numAlive > 1) || 
            (!Battle_handler.instance.isDouble_battle && numAlive > 0) )
            {
                Pokemon_party.instance.Selected_member = Array.IndexOf(Battle_handler.instance.Battle_Participants, this)+1;
                Pokemon_party.instance.SwapOutNext = true;
                Game_ui_manager.instance.View_pkm_Party();
                Dialogue_handler.instance.Write_Info("Select a Pokemon to switch in","Details",2f);
                Reset_pkm();
            }
            else if (Battle_handler.instance.isDouble_battle && numAlive == 1)//1 left
            {
                is_active = false;
                Unload_ui();
                Battle_handler.instance.check_Participants();
                Turn_Based_Combat.instance.FainEventDelay = false;
            }
        }
    }
    public void Deactivate_pkm()
    {
        is_active = false;
        Current_Enemies.Clear();
        Turn_Based_Combat.instance.OnTurnEnd -= status.Check_status;
        Turn_Based_Combat.instance.OnNewTurn -= status.StunCheck;
        Turn_Based_Combat.instance.OnMoveExecute -= status.Notify_Healing;
    }
    public void Reset_pkm()
    {
        data.Load_Stats();
        data.Reset_Battle_state(pokemon,false);
        if (isPlayer)
            pokemon.OnLevelUP -= Reset_pkm;
        ability_h.ResetState();
        CanEscape = true;
        AddtionalTypeImmunity = null;
    }
    private void update_ui()
    {
        pkm_name.text = pokemon.Pokemon_name;
        pkm_lv.text = "Lv: " + pokemon.Current_level;
        if (isPlayer)
        {
            pkm_img.sprite = pokemon.back_picture;
            if (!Battle_handler.instance.isDouble_battle)
            {
                pkm_HP.text = pokemon.HP + "/" + pokemon.max_HP;
                Exp_bar();
            }
        }
        else
            pkm_img.sprite = pokemon.front_picture;
        player_hp.value = pokemon.HP;
        player_hp.maxValue = pokemon.max_HP;
        if(pokemon.HP<=0)
            pokemon.HP = 0;
    }

    public void refresh_statusIMG()
    {
        if (pokemon.Status_effect == "None")
            status_img.gameObject.SetActive(false);
        else
        {
            status_img.gameObject.SetActive(true);
            status_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/Pokemon_obj/Status/" + pokemon.Status_effect.ToLower());
        }
    }
    void UI_visible(GameObject[]arr,bool on)
    {
        foreach (GameObject obj in arr)
            obj.SetActive(on);
    }
    void Exp_bar()
    {
        player_exp.value = ((pokemon.CurrentExpAmount/pokemon.NextLvExpAmount)*100);
        player_exp.maxValue = 100;
        player_exp.minValue = 0;
    }
    public void Load_ui()
    {
        refresh_statusIMG();
        player_hp.minValue = 0;
        fainted = false;
        is_active = true;
        participant_ui.SetActive(true);
        gender_img();
        if (pokemon.Status_effect == "Badly poison")
            pokemon.Status_effect = "Poison";
        Move_handler.instance.Set_Status(this, pokemon.Status_effect);
        Turn_Based_Combat.instance.OnTurnEnd += status.Check_status;
        Turn_Based_Combat.instance.OnNewTurn += status.StunCheck;
        Turn_Based_Combat.instance.OnMoveExecute += status.Notify_Healing;
        if (isPlayer)
        {
            pokemon.OnLevelUP += Reset_pkm;
            pokemon.OnLevelUp += Battle_handler.instance.LevelUpEvent;
            pokemon.OnNewLevel += data.save_stats;
            if (Battle_handler.instance.isDouble_battle)
            {
                UI_visible(Double_battle_ui, true);
                UI_visible(single_battle_ui, false);
            }
            else
            {
                UI_visible(Double_battle_ui, false);
                UI_visible(single_battle_ui, true);
            }
        }
    }
    void gender_img()
    {
        Gender_img.gameObject.SetActive(true);
        if(pokemon.has_gender)
            Gender_img.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+pokemon.Gender.ToLower());
        else
            Gender_img.gameObject.SetActive(false);
    }
    public void Unload_ui()
    {
        participant_ui.SetActive(false);
    }
}
