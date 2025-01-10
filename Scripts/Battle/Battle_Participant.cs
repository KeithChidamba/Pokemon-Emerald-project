using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Battle_Participant : MonoBehaviour
{
    public Abilities ability_h;
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
    public event Action<Battle_Participant> Onfaint;
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
    private void Get_exp(Battle_Participant enemy)
    {
        Battle_handler.instance.Distribute_EXP(pokemon.Calc_Exp(enemy.pokemon));
        Current_Enemies.Remove(enemy);
    }
    private void Get_EVs(Battle_Participant enemy)
    {
        foreach (string ev in enemy.pokemon.EVs)
        {
            float evAmount = float.Parse(ev.Substring(ev.Length - 1, 1));
            string evStat = ev.Substring(0 ,ev.Length - 1);
            PokemonOperations.GetEV(evStat,evAmount,pokemon);
        }
    }
    public void Check_Faint()
    {
        if (!is_active) return;
        if (pokemon.HP > 0) return;
        if (fainted) return;
        fainted = true;
        Dialogue_handler.instance.Battle_Info(pokemon.Pokemon_name+" fainted!");
        if (isPlayer)
            Invoke(nameof(Check_loss),1f);
        else
            if (!Battle_handler.instance.is_trainer_battle)
                Invoke(nameof(EndWildBattle),1f);
            else
                Invoke(nameof(EndTrainerBattle),1f);
    }
    void EndWildBattle()
    {
        Onfaint?.Invoke(this);
        Wild_pkm.instance.InBattle = false;
        Battle_handler.instance.End_Battle(true);
    }
    void EndTrainerBattle()
    {
        Onfaint?.Invoke(this);
        trainer.InBattle = false;
        Battle_handler.instance.End_Battle(true);
    }
    private void Check_loss()
    {
        Onfaint?.Invoke(this);
        int faint_count = 0;
        for(int i=0;i<Pokemon_party.instance.num_members;i++)
            if (Pokemon_party.instance.party[i].HP <= 0)
                faint_count++;
        if (faint_count == Pokemon_party.instance.num_members)
        {
            Battle_handler.instance.End_Battle(false);
            if(!Battle_handler.instance.is_trainer_battle)
                Wild_pkm.instance.InBattle = false;
        }
        else
        {//select next pokemon to switch in
            Pokemon_party.instance.Selected_member = Array.IndexOf(Battle_handler.instance.Battle_P, this)+1;
            Pokemon_party.instance.SwapOutNext = true;
            Game_ui_manager.instance.View_pkm_Party();
            Dialogue_handler.instance.Write_Info("Select a Pokemon to switch in","Details");
            Reset_pkm();
        }
    }
    void Deactivate_pkm()
    {
        is_active = false;
        Onfaint = null;
        Turn_Based_Combat.instance.OnTurnEnd -= status.Check_status;
        Turn_Based_Combat.instance.OnNewTurn -= status.StunCheck;
        Turn_Based_Combat.instance.OnMoveExecute -= status.Notify_Healing;
    }
    public void Reset_pkm()
    {
        data.Load_Stats();
        data.Reset_Battle_state(pokemon,false);
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
        if (pokemon.Status_effect == "BadlyPoison")
            pokemon.Status_effect = "Poison";
        Move_handler.instance.Set_Status(this, pokemon.Status_effect);
        Turn_Based_Combat.instance.OnTurnEnd += status.Check_status;
        Turn_Based_Combat.instance.OnNewTurn += status.StunCheck;
        Turn_Based_Combat.instance.OnMoveExecute += status.Notify_Healing;
        if (isPlayer)
        {
            pokemon.OnLevelUP += Reset_pkm;
            pokemon.OnLevelUp += Battle_handler.instance.LevelUpEvent;
            foreach (Battle_Participant p in Current_Enemies)
            {
                p.Onfaint += Get_exp;
                p.Onfaint += Get_EVs;
            }
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
        Current_Enemies.Clear();
    }
}
