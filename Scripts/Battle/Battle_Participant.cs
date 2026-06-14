using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public abstract class BattleParticipantModule
{
    protected Battle_Participant participant;
    public void GetParticipant(Battle_Participant parentParticipant)
    {
        participant = parentParticipant;
    }
}
public class Battle_Participant : MonoBehaviour,IInjectable
{
    [SerializeField]public AbilityHandler abilityHandler;
    [SerializeField]public Participant_Status statusHandler;
    [SerializeField]public Held_Items heldItemHandler;
    [SerializeField]public Enemy_trainer pokemonTrainerAI;
    [SerializeField]public Battle_Data statData;

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
    public bool isActive;
    public bool activeForBattle;
    public bool canAttack = true;
    public bool isFlinched;
    public bool isConfused;
    public bool isInfatuated;
    public bool canBeDamaged = true;
    
    public SemiInvulnerabilityData semiInvulnerabilityData = new();
    public bool isSemiInvulnerable;
    public bool canEscape = true;
    public List<StatChangeData> statChangeEffects = new();
    
    public Slider playerHpSlider;
    [FormerlySerializedAs("hpSliderColor")] public RawImage hpSliderImage;
    public Slider playerExpSlider;
    public GameObject[] singleBattleUI;
    public GameObject[] doubleBattleUI;
    public GameObject participantUI;
    
    public PreviousMove previousMove;
    public TurnCoolDown currentCoolDown;
    public Type additionalTypeImmunity;
    public List<TypeImmunityNegation> immunityNegations = new();
    
    public List<Pokemon> expReceivers;
    private bool _expEventDelay;
    public event Action OnPokemonFainted;
    
    public List<Barrier> barriers = new();
    
    public Animator statusEffectAnimator;
    private Vector2 _defaultImagePosition;
    private Vector2 _defaultUIPosition;
    private RectTransform _uiRect;
    
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Game_ui_manager _gameUIHandler;
    private Pokemon_party _pokemonPartyHandler;
    private WildPokemonAiHandler _wildPokemonHandler;
    private Move_handler _moveUsageHandler;
    private Dialogue_handler _dialogueHandler;
    private ServiceContainer _container;
    private PokemonOperations _pokemonOperationsHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _wildPokemonHandler = container.Resolve<WildPokemonAiHandler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _container = container;
        gameObject.SetActive(true);
    }
    public void OnInject()
    {
        heldItemHandler = new Held_Items(_container);
        statusHandler = new Participant_Status(_container);
        abilityHandler = new AbilityHandler(_container);
        statData = new Battle_Data();
        
        heldItemHandler.GetParticipant(this);
        statusHandler.GetParticipant(this);
        abilityHandler.GetParticipant(this);
        statData.GetParticipant(this);
        
        _turnBasedCombatHandler.OnNewTurn += CheckBarrierSharing;
        _turnBasedCombatHandler.OnTurnsCompleted += CheckBarrierDuration;
        currentCoolDown =  new(this, _moveUsageHandler);
        _defaultImagePosition = pokemonImage.rectTransform.anchoredPosition;
        _uiRect = participantUI.GetComponent<RectTransform>();
        _defaultUIPosition = _uiRect.anchoredPosition; 
    }

    public IEnumerator SetupEnemyAi(TrainerData trainerData,Battle_Participant partner = null)
    {
        pokemonTrainerAI = new Enemy_trainer(_container);
        pokemonTrainerAI.GetParticipant(this);
        yield return pokemonTrainerAI.SetupTrainerForBattle(trainerData);
        if (partner == null)
        {
            yield break;
        }
        //copy over team data to enemy partner
        partner.pokemonTrainerAI = new Enemy_trainer(_container);
        partner.pokemonTrainerAI.GetParticipant(partner);
        partner.pokemonTrainerAI.trainerParty = pokemonTrainerAI.trainerParty;
        partner.pokemonTrainerAI.trainerData = pokemonTrainerAI.trainerData;
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
            _pokemonOperationsHandler.CalculateEvForStat(ev.stat,ev.eVAmount,enemy.pokemon);
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
        expReceivers.RemoveAll(p => !_pokemonPartyHandler.party.Contains(p));
        if (expReceivers.Count < 1) yield break;

        // Separate holders and participants
        var expShareHolders = new List<Pokemon>();
        foreach (var receiverPokemon in expReceivers)
        {
            if (!receiverPokemon.hasItem) continue;
            var expHeldItem = receiverPokemon.heldItem.GetDynamicModule<ExpModifierInfo>();
            if (expHeldItem != null)
            {
                var hasExpShare = expHeldItem.modifier == ExpModifier.ExpShare;
                if (hasExpShare)
                {
                    expShareHolders.Add(receiverPokemon);
                }
            }
            
        }
        
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
        if (!isActive) return;
        if (pokemon.hp > 0) return;
        pokemon.statusEffect = StatusEffect.None;
        _battleHandler.faintQueue.Add(this);
        pokemon.DetermineFriendshipLevelChange(
            false, FriendshipModifier.Fainted);
        OnPokemonFainted?.Invoke();
        if (!_turnBasedCombatHandler.faintEventDelay)
            _battleHandler.StartFaintEvent();
    }
    public IEnumerator HandleFaintLogic()
    {
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        if (!isPlayer)
        {
            yield return DistributeExp(pokemon.CalculateExperience());
            
            yield return new WaitUntil(() => !_expEventDelay);
            
            foreach (var enemy in currentEnemies)
                if(enemy.isActive)
                    GiveEVs(enemy);

            if (!_battleHandler.isTrainerBattle)
            {
                yield return _wildPokemonHandler.EndWildBattle();
                _turnBasedCombatHandler.faintEventDelay = false;
            }
            else
            {
                ResetParticipantState();
                yield return pokemonTrainerAI.CheckIfLoss();
            }
        }
        else
        {
            yield return CheckIfPlayerLoss();
        }
    }
    
    private IEnumerator CheckIfPlayerLoss()
    {
        var alivePokemon = _pokemonPartyHandler.GetLivingPokemon();
        if (alivePokemon.Count==0)
        {
            _battleHandler.EndBattle(BattleEndState.PlayerLost);
        }
        else
        {//select next pokemon to switch in
            if ( (_battleHandler.isDoubleBattle && alivePokemon.Count > 1) || 
            (!_battleHandler.isDoubleBattle && alivePokemon.Count > 0) )
            {
                _battleHandler.OnSwitchIn += ResetEvent;
                SetupSwitchOut();
                yield return new WaitUntil(() => !_turnBasedCombatHandler.faintEventDelay);
                void ResetEvent()
                {
                    _battleHandler.OnSwitchIn -= ResetEvent;
                    _turnBasedCombatHandler.faintEventDelay = false;
                }
            }
            else if (_battleHandler.isDoubleBattle && alivePokemon.Count == 1)//1 left
            {
                isActive = false;
                DeactivateUI();
                _battleHandler.CheckParticipantStates();
                _turnBasedCombatHandler.faintEventDelay = false;
            }
        }
        yield return null;
    }

    public void SetupSwitchOut()
    {
        _pokemonPartyHandler.selectedMemberNumber = Array.IndexOf(_battleHandler.battleParticipants, this)+1;
        _pokemonPartyHandler.swapOutNext = true;
        
        _pokemonPartyHandler.OnMemberSelected += StartPokemonPartySwap; 
        _gameUIHandler.ViewPokemonParty();
        ResetParticipantState();
    }

    private void StartPokemonPartySwap(int memberPosition)
    {
        _pokemonPartyHandler.OnMemberSelected -= StartPokemonPartySwap; 
        StartCoroutine(_pokemonPartyHandler.SwapMemberWithoutTurnUsage(memberPosition));
    }
    public void DeactivateParticipant()
    {
        if (!isActive) return;
        isActive = false;
        currentEnemies.Clear();       
        barriers.Clear();
        statChangeEffects.Clear();
        
        pokemonImage.rectTransform.anchoredPosition = _defaultImagePosition;
        _uiRect.anchoredPosition = _defaultUIPosition;
        
        pokemonImage.color = Color.white;
       
        _turnBasedCombatHandler.OnMoveExecute -= statusHandler.CheckTrapDuration;
        _turnBasedCombatHandler.OnNewTurn -= statusHandler.StunCheck;
        _turnBasedCombatHandler.OnNewTurn -= statusHandler.CheckStatDropImmunity;
        _turnBasedCombatHandler.OnMoveExecute -= statusHandler.ConfusionCheck;
        _turnBasedCombatHandler.OnMoveExecute -= statusHandler.NotifyHealing;
        _battleHandler.OnBattleEnd -= DeactivateParticipant;
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
        currentCoolDown.ResetState();
        immunityNegations.Clear();
        if (isPlayer)
        {
            pokemon.pokemonDisplayName = pokemon.pokemonName;
            pokemon.OnEvolutionSuccessful -= AddToEvolutionQueue;
            pokemon.OnNewLevel -= statData.SaveActualStats;
            pokemon.OnLevelUp -= ResetParticipantStateAfterLevelUp;
        }
        pokemon.OnHealthChanged -= CheckIfFainted;
        if (pokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            pokemon.statusEffect = StatusEffect.Poison;
        }
    }
    private void ResetParticipantStateAfterLevelUp(Pokemon pokemonAfterLevelUp)
    {
        statData.LoadActualStats();
        //same pokemon as the one in this class
        statData.ResetBattleState(pokemonAfterLevelUp,true);
    }
    public int GetPartnerIndex()
    {
        int participantIndex = Array.IndexOf(_battleHandler.battleParticipants, this);
        if (participantIndex == -1) return -1; // participant not found
        return (participantIndex % 2 == 0) ? participantIndex + 1 : participantIndex - 1;
    }

    public bool ProtectedFromStatChange(bool isIncrease)
    {
        var protection = isIncrease? StatChangeability.ImmuneToIncrease
            :StatChangeability.ImmuneToDecrease;
        return statChangeEffects.Any(s => s.Changeability == protection);
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
        
        if (_battleHandler.isDoubleBattle)
        {
            var partner= _battleHandler.battleParticipants[GetPartnerIndex()];
            if (!partner.isActive) return;
            
            foreach (var barrier in barriers)
            {
                if (!_moveUsageHandler.HasDuplicateBarrier(partner, barrier.barrierName, false))
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
        if (isPlayer && !_battleHandler.isDoubleBattle)
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
                SaveDataHandler.GetDirectory(AssetDirectory.Status) 
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
        if (_battleHandler.evolutionQueue.Any(evo=> evo.participantToEvolve==this))
        {//in-case of instant multi-level up
            return;
        }
        var evoData = new EvolutionInBattleData();
        evoData.participantToEvolve = this;
        evoData.evolutionIndex = evolutionIndex;
        _battleHandler.evolutionQueue.Add(evoData);
    }
    public void ActivateParticipant()
    {
        RefreshStatusEffectImage();
        playerHpSlider.minValue = 0;
        isActive = true;
        pokemonImage.sprite = isPlayer?pokemon.backPicture : pokemon.frontPicture;
        rawName = isPlayer? pokemon.pokemonDisplayName :  pokemon.pokemonDisplayName.Replace("Foe ", "");
        ActivateGenderImage();
        if (pokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            pokemon.statusEffect = StatusEffect.Poison;
        }
        _moveUsageHandler.ApplyStatusToVictim(this, pokemon.statusEffect);
        _battleHandler.OnBattleEnd += DeactivateParticipant;
        
        _turnBasedCombatHandler.OnMoveExecute += statusHandler.CheckTrapDuration;
        _turnBasedCombatHandler.OnNewTurn += statusHandler.CheckStatDropImmunity;
        _turnBasedCombatHandler.OnMoveExecute += statusHandler.ConfusionCheck;
        _turnBasedCombatHandler.OnNewTurn += statusHandler.StunCheck;
        _turnBasedCombatHandler.OnMoveExecute += statusHandler.NotifyHealing;
        pokemon.OnHealthChanged += CheckIfFainted;
        ActivateUI(doubleBattleUI, _battleHandler.isDoubleBattle);
        ActivateUI(singleBattleUI, !_battleHandler.isDoubleBattle);
        
        if (!isPlayer) return;
        pokemon.OnEvolutionSuccessful += AddToEvolutionQueue;
        pokemon.OnLevelUp +=  ResetParticipantStateAfterLevelUp;
        pokemon.OnNewLevel += statData.SaveActualStats;
    }
    private void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
            pokemonGenderImage.sprite = Resources.Load<Sprite>(
                SaveDataHandler.GetDirectory(AssetDirectory.UI) 
                + pokemon.gender.ToString().ToLower());
        else
            pokemonGenderImage.gameObject.SetActive(false);
    }
    public void DeactivateUI()
    {
        participantUI.SetActive(false);
    }
}
