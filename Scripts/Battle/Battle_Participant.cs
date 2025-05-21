using System;
using System.Collections.Generic;
using System.Linq;
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
    public bool isEnemy = false;
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
        Battle_handler.Instance.OnBattleEnd += DeactivatePokemon;
    }
    private void Update()
    {
        if (!isActive) return;
        UpdateUI();
    }
    private void GiveExp(Pokemon enemy)
    {
        //let the enemy that knocked you out, calculate exp 
        DistributeExp(enemy.CalculateExperience(pokemon));
    }
    private void GiveEVs(Battle_Participant enemy)
    {
        foreach (string ev in pokemon.EVs)
        {
            var evAmount = float.Parse(ev.Substring(ev.Length - 1, 1));
            var evStat = ev.Substring(0 ,ev.Length - 1);
            PokemonOperations.CalculateEvForStat(evStat,evAmount,enemy.pokemon);
        }
    }
    public  void AddToExpList(Pokemon pkm)
    {
        if(!expReceivers.Contains(pkm))
            expReceivers.Add(pkm);
    }
    private void DistributeExp(int expFromEnemy)
    {
        // Remove fainted or invalid Pokémon
        expReceivers.RemoveAll(p => p.HP <= 0);
        expReceivers.RemoveAll(p => !Pokemon_party.Instance.party.Contains(p));//only player pokemon receive exp
        if (expReceivers.Count < 1) return;

        // Separate holders and participants
        var expShareHolders = expReceivers
            .Where(p => p.HasItem && p.HeldItem.itemName == "Exp Share")
            .ToList();

        var participants = expReceivers
            .Where(p => !expShareHolders.Contains(p))
            .ToList();

        var totalExp = expFromEnemy;

        // Distribute 50% to EXP Share holders
        var expShareTotal = totalExp / 2;
        if (expShareHolders.Count > 0)
        {
            var shareExpPerHolder = expShareTotal / expShareHolders.Count;
            foreach (var p in expShareHolders)
                p.ReceiveExperience(shareExpPerHolder);
        }

        // Distribute remaining 50% among participants
        var participantTotalExp = totalExp - expShareTotal; 
        if (participants.Count > 0)
        {
            var shareExpPerParticipant = participantTotalExp / participants.Count;
            foreach (var p in participants)
                p.ReceiveExperience(shareExpPerParticipant);
        }

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
        if (fainted) return; 
        fainted = (pokemon.HP <= 0);
        if (pokemon.HP > 0) return;
        
        Turn_Based_Combat.Instance.faintEventDelay = true;
        Dialogue_handler.Instance.DisplayBattleInfo(pokemon.Pokemon_name+" fainted!");
        pokemon.Status_effect = "None";

        if (!isPlayer)
        {
            GiveExp(Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon);
            foreach (var enemy in currentEnemies)
                if(enemy.isActive)
                    GiveEVs(enemy);
        }

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
        Battle_handler.Instance.EndBattle(true);
    }
    private void CheckIfLoss()
    {
        var alivePokemon = Pokemon_party.Instance.GetLivingPokemon();
        if (alivePokemon.Count==0)
        {
            Battle_handler.Instance.EndBattle(false);
            if(!Battle_handler.Instance.isTrainerBattle)
                Wild_pkm.Instance.inBattle = false;
        }
        else
        {//select next pokemon to switch in
            if ( (Battle_handler.Instance.isDoubleBattle && alivePokemon.Count > 1) || 
            (!Battle_handler.Instance.isDoubleBattle && alivePokemon.Count > 0) )
            {
                Pokemon_party.Instance.selectedMemberIndex = Array.IndexOf(Battle_handler.Instance.battleParticipants, this)+1;
                Pokemon_party.Instance.swapOutNext = true;
                Game_ui_manager.Instance.ViewPokemonParty();
                Dialogue_handler.Instance.DisplayInfo("Select a Pokemon to switch in","Details",2f);
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
    public void DeactivatePokemon()
    {
        isActive = false;
        currentEnemies.Clear();
        Turn_Based_Combat.Instance.OnTurnEnd -= statusHandler.Check_status;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.NotifyHealing;
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
        var rawName = (isEnemy)? pokemon.Pokemon_name.Replace("Foe ", "") : pokemon.Pokemon_name;
        pokemonNameText.text = rawName;
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
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.NotifyHealing;
        if (!isPlayer) return;
        _resetHandler = pkm => ResetParticipantState();
        pokemon.OnLevelUp += _resetHandler;
        pokemon.OnLevelUp += Battle_handler.Instance.LevelUpEvent;
        pokemon.OnNewLevel += statData.SaveActualStats;
        ActivateUI(doubleBattleUI, Battle_handler.Instance.isDoubleBattle);
        ActivateUI(singleBattleUI, !Battle_handler.Instance.isDoubleBattle);
    }
    private void ActivateGenderImage()
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
