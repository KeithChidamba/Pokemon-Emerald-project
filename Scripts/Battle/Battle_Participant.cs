using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public Action OnPokemonFainted;
    public List<Barrier> Barrieirs = new();
    private void Start()
    {
        statusHandler = GetComponent<Participant_Status>();
        abilityHandler = GetComponent<AbilityHandler>();
        statData = GetComponent<Battle_Data>();
        Turn_Based_Combat.Instance.OnNewTurn += CheckBarrierSharing;
        Turn_Based_Combat.Instance.OnTurnsCompleted += CheckBarrierDuration;
        Battle_handler.Instance.OnBattleEnd += DeactivatePokemon;
    }
    private void Update()
    {
        if (!isActive) return;
        UpdateUI();
    }
    private void GiveEVs(Battle_Participant enemy)
    {
        foreach (string ev in pokemon.effortValues)
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
        expReceivers.RemoveAll(p => p.hp <= 0);
        expReceivers.RemoveAll(p => !Pokemon_party.Instance.party.Contains(p));//only player pokemon receive exp
        if (expReceivers.Count < 1) return;

        // Separate holders and participants
        var expShareHolders = expReceivers
            .Where(p => p.hasItem && p.heldItem.itemName == "Exp Share")
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
    private void CheckIfFainted()
    {
        if (!isActive) return;
        if (fainted) return; 
        fainted = (pokemon.hp <= 0);
        if (pokemon.hp > 0) return;
        OnPokemonFainted?.Invoke();
        Turn_Based_Combat.Instance.faintEventDelay = true;
        Dialogue_handler.Instance.DisplayBattleInfo(pokemon.pokemonName+" fainted!");
        pokemon.statusEffect = "None";
        pokemon.DetermineFriendshipLevelChange(false,"Fainted");
        StartCoroutine(HandleFaintLogic());
    }

    private IEnumerator HandleFaintLogic()
    {
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        if (!isPlayer)
        {
            //let the enemy that knocked you out, calculate exp 
            var enemyResponsible =
                Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon;
            
            DistributeExp(enemyResponsible.CalculateExperience(pokemon));
            
            foreach (var enemy in currentEnemies)
                if(enemy.isActive)
                    GiveEVs(enemy);
            
            yield return new WaitUntil(() => !Turn_Based_Combat.Instance.levelEventDelay);
            if (!Battle_handler.Instance.isTrainerBattle)
                EndWildBattle();
            else
            {
                ResetParticipantState();
                pokemonTrainerAI.CheckIfLoss();
            }
        }
        else
            CheckIfLoss();
    }
    public void EndWildBattle()
    {
        statData.ResetBattleState(pokemon,false);
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
                Battle_handler.Instance.CheckParticipantStates();
                Turn_Based_Combat.Instance.faintEventDelay = false;
            }
        }
    }
    public void DeactivatePokemon()
    {
        isActive = false;
        currentEnemies.Clear();
        Turn_Based_Combat.Instance.OnTurnsCompleted -= statusHandler.CheckStatus;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.NotifyHealing;
    }
    public void ResetParticipantState()
    {
        statData.LoadActualStats();
        statData.ResetBattleState(pokemon,false);
        abilityHandler.ResetState();
        canEscape = true;
        additionalTypeImmunity = null;
        if (isPlayer) pokemon.OnLevelUp -= _resetHandler;
    }
    private void ResetParticipantStateAfterLevelUp()
    {
        statData.LoadActualStats();
        statData.ResetBattleState(pokemon,true);
    }
    public int GetPartnerIndex()
    {
        int participantIndex = Array.IndexOf(Battle_handler.Instance.battleParticipants, this);
        if (participantIndex == -1) return -1; // participant not found
        return (participantIndex % 2 == 0) ? participantIndex + 1 : participantIndex - 1;
    }

    private void CheckBarrierDuration()
    {
        if (Barrieirs.Count == 0) return;

        foreach (var barrier in Barrieirs)
            barrier.barrierDuration--;

        Barrieirs.RemoveAll(b => b.barrierDuration < 1);
    }
    private void CheckBarrierSharing()
    {
        if (Barrieirs.Count == 0) return;
        
        if (Battle_handler.Instance.isDoubleBattle)
        {
            var partner= Battle_handler.Instance.battleParticipants[GetPartnerIndex()];
            if (!partner.isActive) return;
            
            foreach (var barrier in Barrieirs)
            {
                if (!Move_handler.Instance.HasDuplicateBarrier(partner, barrier.barrierName, false))
                {
                    var barrierCopy = new Barrier(barrier.barrierName, barrier.barrierEffect, barrier.barrierDuration);
                    partner.Barrieirs.Add(barrierCopy);
                }
            }
        }
    }
    private void UpdateUI()
    {
        var rawName = (isEnemy)? pokemon.pokemonName.Replace("Foe ", "") : pokemon.pokemonName;
        pokemonNameText.text = rawName;
        pokemonLevelText.text = "Lv: " + pokemon.currentLevel;
        if (isPlayer)
        {
            pokemonImage.sprite = pokemon.backPicture;
            if (!Battle_handler.Instance.isDoubleBattle)
            {
                pokemonHealthText.text = pokemon.hp + "/" + pokemon.maxHp;
                SetExpBarValue();
            }
        }
        else
            pokemonImage.sprite = pokemon.frontPicture;
        playerHpSlider.value = pokemon.hp;
        playerHpSlider.maxValue = pokemon.maxHp;
        if(pokemon.hp<=0)
            pokemon.hp = 0;
    }

    public void RefreshStatusEffectImage()
    {
        if (pokemon.statusEffect == "None")
            statusImage.gameObject.SetActive(false);
        else
        {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = Resources.Load<Sprite>
            ("Pokemon_project_assets/Pokemon_obj/Status/" + pokemon.statusEffect.Replace(" ","").ToLower());
        }
    }
    private void ActivateUI(GameObject[]arr,bool on)
    {
        foreach (GameObject obj in arr)
            obj.SetActive(on);
    }
    private void SetExpBarValue()
    {
        playerExpSlider.value = ((pokemon.currentExpAmount/pokemon.nextLevelExpAmount)*100);
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
        if (pokemon.statusEffect == "Badly poison")
            pokemon.statusEffect = "Poison";
        Move_handler.Instance.ApplyStatusToVictim(this, pokemon.statusEffect);
        Turn_Based_Combat.Instance.OnTurnsCompleted += statusHandler.CheckStatus;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.NotifyHealing;
        pokemon.OnDamageTaken += CheckIfFainted;
        if (!isPlayer) return;
        _resetHandler = pkm => ResetParticipantStateAfterLevelUp();
        pokemon.OnLevelUp += _resetHandler;
        pokemon.OnLevelUp += Battle_handler.Instance.LevelUpEvent;
        pokemon.OnNewLevel += statData.SaveActualStats;
        ActivateUI(doubleBattleUI, Battle_handler.Instance.isDoubleBattle);
        ActivateUI(singleBattleUI, !Battle_handler.Instance.isDoubleBattle);
    }
    private void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
            pokemonGenderImage.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+pokemon.gender.ToLower());
        else
            pokemonGenderImage.gameObject.SetActive(false);
    }
    public void DeactivateUI()
    {
        participantUI.SetActive(false);
    }
}
