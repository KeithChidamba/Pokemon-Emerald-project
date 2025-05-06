using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Battle_Participant : MonoBehaviour
{
    public AbilityHandler abilityHandler;
    public Participant_Status statusHandler;
    public Enemy_trainer pokemonTrainerAI;
    public Battle_Data statData;
    public Pokemon pokemon;
    public List<Battle_Participant> currentEnemies;
    public Image pokemonImage;
    public Image statusImage;
    public Image pokemonGenderImage;
    public Text pokemonNameText;
    public Text pokemonHealthText;
    public Text pokemonLevelText;
    public bool isPlayer = false;
    public bool isActive = false;
    public bool fainted = false;
    public Slider playerHpSlider;
    public Slider playerExpSlider;
    public GameObject[] singleBattleUI;
    public GameObject[] doubleBattleUI;
    public GameObject participantUI;
    public bool enemySelected = false;
    public string previousMove="";
    public Type additionalTypeImmunity;
    public List<Pokemon> expReceivers;
    public bool canEscape = true;
    private Action<Pokemon> _resetHandler;
    private void Start()
    {
        statusHandler = GetComponent<Participant_Status>();
        abilityHandler = GetComponent<AbilityHandler>();
        statData = GetComponent<Battle_Data>();
        Turn_Based_Combat.Instance.OnTurnEnd += CheckIfFainted;
        Move_handler.Instance.OnMoveEnd += CheckIfFainted;
        Turn_Based_Combat.Instance.OnMoveExecute += CheckIfFainted;
        Battle_handler.Instance.OnBattleEnd += Deactivate_pkm;
    }
    private void Update()
    {
        if (!isActive) return;
        UpdateUI();
    }
    private void Give_exp()
    {
        Distribute_EXP(pokemon.CalculateExp(pokemon));
    }
    private void Give_EVs(Battle_Participant enemy)
    {
        foreach (string ev in pokemon.EVs)
        {
            var evAmount = float.Parse(ev.Substring(ev.Length - 1, 1));
            var evStat = ev.Substring(0 ,ev.Length - 1);
            PokemonOperations.GetEV(evStat,evAmount,enemy.pokemon);
        }
    }
    public  void AddToExpList(Pokemon pkm)
    {
        if(!expReceivers.Contains(pkm))
            expReceivers.Add(pkm);
    }
    private void Distribute_EXP(int exp_from_enemy)
    {
        expReceivers.RemoveAll(p => p.HP <= 0);
        if(expReceivers.Count<1)return;
        if (expReceivers.Count == 1)//let the pokemon with exp share get all exp if it fought alone
        {
            expReceivers[0].Recieve_exp(exp_from_enemy);
            expReceivers.Clear();
            return;
        }
        foreach(Pokemon p in Pokemon_party.instance.party)//exp share split, assuming there's only ever 1 exp share in the game
            if (p != null && p.HP>0 && p.HasItem)
                if(p.HeldItem.itemName == "Exp Share")
                {
                    p.Recieve_exp(exp_from_enemy / 2);
                    exp_from_enemy /= 2;
                    break;
                }
        var exp = exp_from_enemy / expReceivers.Count;
        foreach (Pokemon p in expReceivers)
            if(!p.HasItem | p.HeldItem.itemName != "Exp Share")
                p.Recieve_exp(exp);
        expReceivers.Clear();
    }
    public void CheckIfFainted()
    {
        if (pokemon != null)
        {//if revived during double battle for example
            if (pokemon.HP > 0 & !isActive)
            {
                isActive = true;
                participantUI.SetActive(true);
            }
        }
        if (!isActive) return;
        fainted = (pokemon.HP <= 0);
        if (pokemon.HP > 0) return;
        Turn_Based_Combat.Instance.faintEventDelay = true;
        Dialogue_handler.instance.Battle_Info(pokemon.Pokemon_name+" fainted!");
        pokemon.Status_effect = "None";
        Give_exp();
        foreach (Battle_Participant enemy in currentEnemies)
            if(enemy.pokemon!=null)
                Give_EVs(enemy);
        if (isPlayer)
            Invoke(nameof(CheckIfLoss),1f);
        else
            if (!Battle_handler.Instance.isTrainerBattle)
                Invoke(nameof(EndWildBattle),1f);
            else
            {
                ResetParticipantState();
                pokemonTrainerAI.Invoke(nameof(pokemonTrainerAI.CheckIfLoss),1f);
            }
    }
    public void EndWildBattle()
    {
        Wild_pkm.Instance.inBattle = false;
        Turn_Based_Combat.Instance.faintEventDelay = false;
        Battle_handler.Instance.End_Battle(true);
    }
    private void CheckIfLoss()
    {
        List<Pokemon> alivePokemon = Pokemon_party.instance.GetLivingPokemon();
        if (alivePokemon.Count==0)
        {
            Battle_handler.Instance.End_Battle(false);
            if(!Battle_handler.Instance.isTrainerBattle)
                Wild_pkm.Instance.inBattle = false;
        }
        else
        {//select next pokemon to switch in
            if ( (Battle_handler.Instance.isDoubleBattle && alivePokemon.Count > 1) || 
            (!Battle_handler.Instance.isDoubleBattle && alivePokemon.Count > 0) )
            {
                Pokemon_party.instance.Selected_member = Array.IndexOf(Battle_handler.Instance.battleParticipants, this)+1;
                Pokemon_party.instance.SwapOutNext = true;
                Game_ui_manager.instance.View_pkm_Party();
                Dialogue_handler.instance.Write_Info("Select a Pokemon to switch in","Details",2f);
                ResetParticipantState();
            }
            else if (Battle_handler.Instance.isDoubleBattle && alivePokemon.Count == 1)//1 left
            {
                isActive = false;
                DeactivateUI();
                Battle_handler.Instance.CountParticipants();
                Turn_Based_Combat.Instance.faintEventDelay = false;
            }
        }
    }
    public void Deactivate_pkm()
    {
        isActive = false;
        currentEnemies.Clear();
        Turn_Based_Combat.Instance.OnTurnEnd -= statusHandler.Check_status;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.Notify_Healing;
    }
    public void ResetParticipantState()
    {
        statData.LoadActualStats();
        statData.ResetBattleState(pokemon,false);
        if (isPlayer)
            pokemon.OnLevelUp -= _resetHandler;
        abilityHandler.ResetState();
        canEscape = true;
        additionalTypeImmunity = null;
    }
    private void UpdateUI()
    {
        pokemonNameText.text = pokemon.Pokemon_name;
        pokemonLevelText.text = "Lv: " + pokemon.Current_level;
        if (isPlayer)
        {
            pokemonImage.sprite = pokemon.back_picture;
            if (!Battle_handler.Instance.isDoubleBattle)
            {
                pokemonHealthText.text = pokemon.HP + "/" + pokemon.max_HP;
                SetExpBarValue();
            }
        }
        else
            pokemonImage.sprite = pokemon.front_picture;
        playerHpSlider.value = pokemon.HP;
        playerHpSlider.maxValue = pokemon.max_HP;
        if(pokemon.HP<=0)
            pokemon.HP = 0;
    }

    public void RefreshStatusEffectImage()
    {
        if (pokemon.Status_effect == "None")
            statusImage.gameObject.SetActive(false);
        else
        {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = Resources.Load<Sprite>("Pokemon_project_assets/Pokemon_obj/Status/" 
                                                        + pokemon.Status_effect.Replace(" ","").ToLower());
        }
    }
    void ActivateUI(GameObject[]arr,bool on)
    {
        foreach (GameObject obj in arr)
            obj.SetActive(on);
    }
    void SetExpBarValue()
    {
        playerExpSlider.value = ((pokemon.CurrentExpAmount/pokemon.NextLevelExpAmount)*100);
        playerExpSlider.maxValue = 100;
        playerExpSlider.minValue = 0;
    }
    public void ActivateParticipant()
    {
        RefreshStatusEffectImage();
        playerHpSlider.minValue = 0;
        fainted = false;
        isActive = true;
        participantUI.SetActive(true);
        ActivateGenderImage();
        if (pokemon.Status_effect == "Badly poison")
            pokemon.Status_effect = "Poison";
        Move_handler.Instance.ApplyStatusToVictim(this, pokemon.Status_effect);
        Turn_Based_Combat.Instance.OnTurnEnd += statusHandler.Check_status;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.Notify_Healing;
        if (isPlayer)
        {
            _resetHandler = pkm => ResetParticipantState();
            pokemon.OnLevelUp += _resetHandler;
            pokemon.OnLevelUp += Battle_handler.Instance.LevelUpEvent;
            pokemon.OnNewLevel += statData.SaveActualStats;
            if (Battle_handler.Instance.isDoubleBattle)
            {
                ActivateUI(doubleBattleUI, true);
                ActivateUI(singleBattleUI, false);
            }
            else
            {
                ActivateUI(doubleBattleUI, false);
                ActivateUI(singleBattleUI, true);
            }
        }
    }
    void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(pokemon.has_gender)
            pokemonGenderImage.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+pokemon.Gender.ToLower());
        else
            pokemonGenderImage.gameObject.SetActive(false);
    }
    public void DeactivateUI()
    {
        participantUI.SetActive(false);
    }
}
