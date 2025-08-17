using System;
using System.Collections;
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
    public bool isPlayer;
    public bool isEnemy;
    public bool isActive;
    public bool fainted;
    public bool canAttack = true;
    public bool isFlinched;
    public bool isConfused;
    public bool canBeDamaged = true;
    public bool canEscape = true;
    
    public List<StatChangeData> StatChangeEffects = new();
    public Slider playerHpSlider;
    [FormerlySerializedAs("hpSliderColor")] public RawImage hpSliderImage;
    public Slider playerExpSlider;
    public GameObject[] singleBattleUI;
    public GameObject[] doubleBattleUI;
    public GameObject participantUI;
    public PreviousMove PreviousMove;
    public TurnCoolDown currentCoolDown;
    public Type additionalTypeImmunity;
    public List<TypeImmunityNegation> ImmunityNegations = new();
    public List<Pokemon> expReceivers;
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
        OnPokemonFainted += Battle_handler.Instance.FaintEvent;
    }
    private void Update()
    {
        if (!isActive) return;
        UpdateUI();
    }
    private void GiveEVs(Battle_Participant enemy)
    {
        foreach (var ev in pokemon.effortValues)
        {
            PokemonOperations.CalculateEvForStat(ev.stat,ev.eVAmount,enemy.pokemon);
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
    private IEnumerator CheckIfFainted()
    {
        if (!fainted && isActive)
        {
            fainted = pokemon.hp <= 0;
            if (pokemon.hp <= 0)
            {
                pokemon.statusEffect = PokemonOperations.StatusEffect.None;
                Battle_handler.Instance.faintQueue.Add(this);
                yield return new WaitUntil(() => !Move_handler.Instance.processingOrder);
                pokemon.DetermineFriendshipLevelChange(
                    false, PokemonOperations.FriendshipModifier.Fainted);
                if(!Turn_Based_Combat.Instance.faintEventDelay)
                    OnPokemonFainted?.Invoke();
            }
        }
        yield return null;
    }
    public IEnumerator HandleFaintLogic()
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
        else CheckIfLoss();
    }
    public void EndWildBattle()
    {
        statData.ResetBattleState(pokemon);
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
                Pokemon_party.Instance.selectedMemberNumber = Array.IndexOf(Battle_handler.Instance.battleParticipants, this)+1;
                Pokemon_party.Instance.swapOutNext = true;
                Game_ui_manager.Instance.ViewPokemonParty();
                Dialogue_handler.Instance.DisplayDetails("Select a Pokemon to switch in");
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
        StatChangeEffects.Clear();
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.CheckTrapDuration;
        Turn_Based_Combat.Instance.OnTurnsCompleted -= statusHandler.CheckStatus;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.ConfusionCheck;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.NotifyHealing;
    }
    public void ResetParticipantState()
    {
        statData.LoadActualStats();
        statData.ResetBattleState(pokemon);
        abilityHandler.ResetState();
        canEscape = true;
        PreviousMove = null;
        additionalTypeImmunity = null;
        OnPokemonFainted = null;
        ImmunityNegations.Clear();
        if (isPlayer) pokemon.OnLevelUp -= ResetParticipantStateAfterLevelUp;
    }
    private void ResetParticipantStateAfterLevelUp(Pokemon pokemonAfterLevelUp)
    {
        statData.LoadActualStats();
        //same pokemon as the one in this class
        statData.ResetBattleState(pokemonAfterLevelUp,true);
    }
    public int GetPartnerIndex()
    {
        int participantIndex = Array.IndexOf(Battle_handler.Instance.battleParticipants, this);
        if (participantIndex == -1) return -1; // participant not found
        return (participantIndex % 2 == 0) ? participantIndex + 1 : participantIndex - 1;
    }

    public bool ProtectedFromStatChange(bool isIncrease)
    {
        var protection = isIncrease? StatChangeData.StatChangeability.ImmuneToIncrease
            :StatChangeData.StatChangeability.ImmuneToDecrease;
        return StatChangeEffects.Any(s => s.Changeability == protection);
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
        if(pokemon.hp<=0) pokemon.hp = 0;
        PokemonOperations.UpdateHealthPhase(pokemon,hpSliderImage);
    }
    public void RefreshStatusEffectImage()
    {
        if (pokemon.statusEffect == PokemonOperations.StatusEffect.None)
            statusImage.gameObject.SetActive(false);
        else
        {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(Save_manager.AssetDirectory.Status) 
             + pokemon.statusEffect.ToString().ToLower());
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
        if (pokemon.statusEffect == PokemonOperations.StatusEffect.BadlyPoison)
        {
            pokemon.statusEffect = PokemonOperations.StatusEffect.Poison;
        }
        Move_handler.Instance.ApplyStatusToVictim(this, pokemon.statusEffect);
        Turn_Based_Combat.Instance.OnTurnsCompleted += statusHandler.CheckStatus;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.CheckTrapDuration;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.ConfusionCheck;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.NotifyHealing;
        pokemon.OnDamageTaken += ()=> StartCoroutine(CheckIfFainted());
        if (!isPlayer) return;
        pokemon.OnLevelUp +=  ResetParticipantStateAfterLevelUp;
        pokemon.OnLevelUp += Battle_handler.Instance.LevelUpEvent;
        pokemon.OnNewLevel += statData.SaveActualStats;
        ActivateUI(doubleBattleUI, Battle_handler.Instance.isDoubleBattle);
        ActivateUI(singleBattleUI, !Battle_handler.Instance.isDoubleBattle);
    }
    private void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
            pokemonGenderImage.sprite = Resources.Load<Sprite>("Pokemon_project_assets/ui/"+pokemon.gender.ToString().ToLower());
        else
            pokemonGenderImage.gameObject.SetActive(false);
    }
    public void DeactivateUI()
    {
        participantUI.SetActive(false);
    }
}
