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
    public Held_Items heldItemHandler;
    public Enemy_trainer pokemonTrainerAI;
    public Battle_Data statData;
    public Pokemon pokemon;
    public string rawName;
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
    public bool canAttack = true;
    public bool isFlinched;
    public bool isConfused;
    public bool isInfatuated;
    public bool canBeDamaged = true;
    public SemiInvulnerabilityData semiInvulnerabilityData = new();
    public bool isSemiInvulnerable;
    public bool canEscape = true;
    public List<StatChangeData> StatChangeEffects = new();
    public Slider playerHpSlider;
    [FormerlySerializedAs("hpSliderColor")] public RawImage hpSliderImage;
    public Slider playerExpSlider;
    public GameObject[] singleBattleUI;
    public GameObject[] doubleBattleUI;
    public GameObject participantUI;
    public PreviousMove previousMove;
    public TurnCoolDown currentCoolDown = new ();
    public Type additionalTypeImmunity;
    public List<TypeImmunityNegation> immunityNegations = new();
    public List<Pokemon> expReceivers;
    private bool _expEventDelay;
    public Action OnPokemonFainted;
    private Action OnFaintCheck;
    public List<Barrier> barriers = new();
    [SerializeField]private Battle_Participant recentAttacker;
    public Animator statusEffectAnimator;
    private Vector2 _defaultImagePosition;
    private void Start()
    {
        heldItemHandler = GetComponent<Held_Items>();
        statusHandler = GetComponent<Participant_Status>();
        abilityHandler = GetComponent<AbilityHandler>();
        statData = GetComponent<Battle_Data>();
        Turn_Based_Combat.Instance.OnNewTurn += CheckBarrierSharing;
        Turn_Based_Combat.Instance.OnTurnsCompleted += CheckBarrierDuration;
        currentCoolDown.UpdateCoolDown(0,null, "",false,false);
        currentCoolDown.participant = this;
        _defaultImagePosition = pokemonImage.rectTransform.anchoredPosition;
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
    private IEnumerator DistributeExp(int expFromEnemy)
    {
        _expEventDelay = true;
        
        // Remove fainted or invalid Pokémon
        expReceivers.RemoveAll(p => p.hp <= 0);
        //only player pokemon receive exp
        expReceivers.RemoveAll(p => !Pokemon_party.Instance.party.Contains(p));
        if (expReceivers.Count < 1) yield break;

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
            while (expShareHolders.Count > 0)
            {
                var holder = expShareHolders[0];
                yield return holder.ReceiveExperienceAndDisplay(shareExpPerHolder);
                expShareHolders.RemoveAt(0);
            }
        }

        // Distribute remaining 50% among participants
        var participantTotalExp = totalExp - expShareTotal; 
        if (participants.Count > 0)
        {
            var shareExpPerParticipant = participantTotalExp / participants.Count;
            while (participants.Count > 0)
            {
                var participant = participants[0];
                yield return participant.ReceiveExperienceAndDisplay(shareExpPerParticipant);
                participants.RemoveAt(0);
            }
        }

        _expEventDelay = false;
        expReceivers.Clear();
        
    }
    private void CheckIfFainted(Battle_Participant attacker)
    {
        recentAttacker = attacker ?? recentAttacker;
        if (!isActive) return;
        if (pokemon.hp > 0) return;
        pokemon.statusEffect = StatusEffect.None;
        Battle_handler.Instance.faintQueue.Add(this);
        pokemon.DetermineFriendshipLevelChange(
            false, FriendshipModifier.Fainted);
        OnPokemonFainted?.Invoke();
        if (!Turn_Based_Combat.Instance.faintEventDelay)
            Battle_handler.Instance.StartFaintEvent();
    }
    public IEnumerator HandleFaintLogic()
    {
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        if (!isPlayer)
        {
            //let the enemy that knocked you out, calculate exp 
            var enemyResponsible = recentAttacker.pokemon;
            
            yield return DistributeExp(enemyResponsible.CalculateExperience(pokemon));
            
            yield return new WaitUntil(() => !_expEventDelay);
            
            foreach (var enemy in currentEnemies)
                if(enemy.isActive)
                    GiveEVs(enemy);
            
            if (!Battle_handler.Instance.isTrainerBattle)
                EndWildBattle();
            else
            {
                ResetParticipantState();
                yield return pokemonTrainerAI.CheckIfLoss();
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
                Pokemon_party.Instance.OnMemberSelected += StartPokemonPartySwap; 
                Game_ui_manager.Instance.ViewPokemonParty();
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

    private void StartPokemonPartySwap(int memberPosition)
    {
        Pokemon_party.Instance.OnMemberSelected -= StartPokemonPartySwap; 
        StartCoroutine(Pokemon_party.Instance.SwapMemberInBattle(memberPosition));
    }
    public void DeactivateParticipant()
    {
        if (!isActive) return;
        isActive = false;
        currentEnemies.Clear();       
        barriers.Clear();
        StatChangeEffects.Clear();
        
        pokemonImage.rectTransform.anchoredPosition = _defaultImagePosition;
        pokemonImage.color = Color.white;
        
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.CheckTrapDuration;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnNewTurn -= statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.ConfusionCheck;
        Turn_Based_Combat.Instance.OnMoveExecute -= statusHandler.NotifyHealing;
        Battle_handler.Instance.OnBattleEnd -= DeactivateParticipant;
        pokemon.OnHealthChanged -= CheckIfFainted;
        //reset move data in case of in-battle modification
        pokemon.ResetMoveData();
    }
    public void ResetParticipantState()
    {
        statData.LoadActualStats();
        statData.ResetBattleState(pokemon);
        abilityHandler.ResetState();
        isSemiInvulnerable = false;
        semiInvulnerabilityData.ResetState();
        canEscape = true;
        previousMove = null;
        additionalTypeImmunity = null;
        OnPokemonFainted = null;
        recentAttacker = null;
        currentCoolDown.ResetState();
        immunityNegations.Clear();
        if (isPlayer)
        {
            pokemon.OnEvolutionSuccessful -= AddToEvolutionQueue;
            pokemon.OnLevelUp -= ResetParticipantStateAfterLevelUp;
        }
        pokemon.OnHealthChanged -= CheckIfFainted;
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
        var protection = isIncrease? StatChangeability.ImmuneToIncrease
            :StatChangeability.ImmuneToDecrease;
        return StatChangeEffects.Any(s => s.Changeability == protection);
    }
    private void CheckBarrierDuration()
    {
        if (barriers.Count == 0) return;

        foreach (var barrier in barriers)
            barrier.barrierDuration--;

        barriers.RemoveAll(b => b.barrierDuration < 1);
    }
    private void CheckBarrierSharing()
    {
        if (barriers.Count == 0) return;
        
        if (Battle_handler.Instance.isDoubleBattle)
        {
            var partner= Battle_handler.Instance.battleParticipants[GetPartnerIndex()];
            if (!partner.isActive) return;
            
            foreach (var barrier in barriers)
            {
                if (!Move_handler.Instance.HasDuplicateBarrier(partner, barrier.barrierName, false))
                {
                    var barrierCopy = new Barrier(barrier.barrierName, barrier.barrierEffect, barrier.barrierDuration);
                    partner.barriers.Add(barrierCopy);
                }
            }
        }
    }
    private void UpdateUI()
    {
        PokemonOperations.UpdateHealthPhase(pokemon,hpSliderImage); 
        pokemonNameText.text = rawName;
        pokemonLevelText.text = "Lv: " + pokemon.currentLevel;
        if (isPlayer && !Battle_handler.Instance.isDoubleBattle)
        {
            pokemonHealthText.text = pokemon.hp + "/" + pokemon.maxHp;
            playerExpSlider.value = pokemon.currentExpAmount;
            playerExpSlider.maxValue = pokemon.nextLevelExpAmount;
            playerExpSlider.minValue = pokemon.currentLevelExpAmount;
        }
        playerHpSlider.value = pokemon.hp;
        playerHpSlider.maxValue = pokemon.maxHp;
        if(pokemon.hp<=0) pokemon.hp = 0;
    }
    public void RefreshStatusEffectImage()
    {
        if (pokemon.statusEffect == StatusEffect.None)
            statusImage.gameObject.SetActive(false);
        else
        {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(AssetDirectory.Status) 
             + pokemon.statusEffect.ToString().ToLower());
        }
    }
    private void ActivateUI(GameObject[]arr,bool on)
    {
        foreach (var obj in arr)
            obj.SetActive(on);
    }
    private void AddToEvolutionQueue(int evolutionIndex)
    {
        var evoData = new EvolutionInBattleData();
        evoData.participantToEvolve = this;
        evoData.evolutionIndex = evolutionIndex;
        Battle_handler.Instance.evolutionQueue.Add(evoData);
    }
    public void ActivateParticipant()
    {
        RefreshStatusEffectImage();
        playerHpSlider.minValue = 0;
        isActive = true;
        pokemonImage.sprite = isPlayer?pokemon.backPicture : pokemon.frontPicture;
        rawName = (isEnemy)? pokemon.pokemonName.Replace("Foe ", "") : pokemon.pokemonName;
        ActivateGenderImage();
        if (pokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            pokemon.statusEffect = StatusEffect.Poison;
        }
        Move_handler.Instance.ApplyStatusToVictim(this, pokemon.statusEffect);
        Battle_handler.Instance.OnBattleEnd += DeactivateParticipant;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.CheckTrapDuration;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.CheckStatDropImmunity;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.ConfusionCheck;
        Turn_Based_Combat.Instance.OnNewTurn += statusHandler.StunCheck;
        Turn_Based_Combat.Instance.OnMoveExecute += statusHandler.NotifyHealing;
        pokemon.OnHealthChanged += CheckIfFainted;
        pokemon.OnEvolutionSuccessful += AddToEvolutionQueue;
        pokemon.OnLevelUp +=  ResetParticipantStateAfterLevelUp;
        pokemon.OnNewLevel += statData.SaveActualStats;
        ActivateUI(doubleBattleUI, Battle_handler.Instance.isDoubleBattle);
        ActivateUI(singleBattleUI, !Battle_handler.Instance.isDoubleBattle);
    }
    private void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
            pokemonGenderImage.sprite = Resources.Load<Sprite>(
                Save_manager.GetDirectory(AssetDirectory.UI) 
                + pokemon.gender.ToString().ToLower());
        else
            pokemonGenderImage.gameObject.SetActive(false);
    }
    public void DeactivateUI()
    {
        participantUI.SetActive(false);
    }
}
