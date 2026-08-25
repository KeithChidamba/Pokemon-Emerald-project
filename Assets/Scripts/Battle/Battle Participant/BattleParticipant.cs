using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BattleParticipant : MonoBehaviour,IInjectable
{
    public BattleParticipantKey participantKey;
    [SerializeField]public AbilityHandler abilityHandler;
    [SerializeField]public BattleParticipantStatusHandler statusHandler;
    [SerializeField]public HeldItemHandler heldItemHandler;
    [SerializeField]public EnemyAiHandler pokemonTrainerAI;
    [SerializeField]public BattleParticipantStatData statData;

    public Pokemon pokemon;
    public string rawName;
    public List<BattleParticipant> currentEnemies;
    
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
    public bool canBeDamaged = true;
        
    public bool isFlinched;
    public bool canBeFlinched = true;
    
    public bool isConfused;
    
    public bool canBeInfatuated = true;
    public bool isInfatuated;
    
    public SemiInvulnerabilityData semiInvulnerabilityData = new();
    public bool isSemiInvulnerable;
    public bool canEscape = true;
    public List<StatChangeabilityData> statChangeEffects = new();
    
    public Slider playerHpSlider;
    [FormerlySerializedAs("hpSliderColor")] public RawImage hpSliderImage;
    public GameObject[] doubleBattleUI;
    public GameObject participantUI;
    
    public PreviousMove previousMoveData;
    public TurnCoolDown currentCoolDown;
    public Type additionalTypeImmunity;
    public List<TypeImmunityNegation> immunityNegations = new();
    public Slider playerExpSlider;
    public GameObject[] singleBattleUI;
    
    public List<Pokemon> expReceivers;
    private bool _expEventDelay;
    [SerializeField]private bool handlingFaintEvent;
    
    public List<Barrier> barriers = new();
    public StatusEffectAnimationHandler statusAnimationHandler;
    
    private Vector2 _defaultImagePosition;
    private Vector2 _defaultUIPosition;
    private RectTransform _uiRect;
    
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private GameUiHandler _gameUIHandler;
    private PokemonPartyHandler _pokemonPartyHandler;
    private WildPokemonAiHandler _wildPokemonHandler;
    private MoveSequenceHandler _moveUsageHandler;
    private DialogueHandler _dialogueHandler;
    private ServiceContainer _container;
    private PokemonOperations _pokemonOperationsHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _wildPokemonHandler = container.Resolve<WildPokemonAiHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _gameUIHandler = container.Resolve<GameUiHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _container = container;
        gameObject.SetActive(true);
    }
    public void OnInject()
    {
        heldItemHandler = new HeldItemHandler(_container,this);
        statusHandler = new BattleParticipantStatusHandler(_container,this);
        abilityHandler = new AbilityHandler(_container,this);
        statData = new BattleParticipantStatData(this);
        
        _turnBasedCombatHandler.OnNewTurn += CheckBarrierSharing;
        _turnBasedCombatHandler.OnTurnsCompleted += CheckBarrierDuration;
        currentCoolDown =  new(this, _moveUsageHandler);
        _defaultImagePosition = pokemonImage.rectTransform.anchoredPosition;
        _uiRect = participantUI.GetComponent<RectTransform>();
        _defaultUIPosition = _uiRect.anchoredPosition; 
        statusAnimationHandler.participant = this;
    }

    public IEnumerator SetupEnemyAi(TrainerData trainerData,BattleParticipant partner = null)
    {
        pokemonTrainerAI = new EnemyAiHandler(_container,this);
        
        yield return pokemonTrainerAI.SetupTrainerForBattle(trainerData);
        if (partner == null)
        {
            yield break;
        }
        //copy over team data to enemy partner
        partner.pokemonTrainerAI = new EnemyAiHandler(_container,GetPartner());
        partner.pokemonTrainerAI.CopyPartnerData(pokemonTrainerAI.GetPartyLink(), pokemonTrainerAI.trainerData);
    }
    
    private void Update()
    {
        if (!isActive) return;
        UpdateUI();
    }
    private void GiveEVs(BattleParticipant enemy)
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
        expReceivers.RemoveAll(p => !_pokemonPartyHandler.Party.Contains(p));
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
    public void BeginFaintEvent()
    {
        handlingFaintEvent = true;
    }
    public void EndFaintEvent()
    {
        handlingFaintEvent = false;
    }
    private void CheckIfFainted()
    {
        if (!isActive) return;
        if (pokemon.hp > 0) return;
        pokemon.statusEffect = StatusEffect.None;
        _battleHandler.AddFaintedParticipant(this);
        pokemon.DetermineFriendshipLevelChange(false, FriendshipModifier.Fainted);
    }
    public IEnumerator HandleFaintLogic()
    {
        yield return _dialogueHandler.AwaitAllDialogue();
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
                EndFaintEvent();
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
        var alivePokemon = _pokemonPartyHandler.GetLivingPokemonCount();
        if (alivePokemon==0)
        {
            _battleHandler.EndBattle(BattleEndState.PlayerLost);
        }
        else
        {//select next pokemon to switch in
            if ( (_battleHandler.isDoubleBattle && alivePokemon > 1) || 
            (!_battleHandler.isDoubleBattle && alivePokemon > 0) )
            {
                SetupSwitchOut();
                yield return new WaitUntil(() => !handlingFaintEvent);
            }
            else if (_battleHandler.isDoubleBattle && alivePokemon == 1)//1 left
            {
                isActive = false;
                DeactivateUI();
                _battleHandler.CheckParticipantStates();
                EndFaintEvent();
            }
        }
        yield return null;
    }

    public void SetupSwitchOut(PartyUsage usage = PartyUsage.SwapOut)
    {
        _pokemonPartyHandler.selectedMemberIndex = (int)participantKey;
        
        _pokemonPartyHandler.OnMemberSelected += StartPokemonPartySwap; 
        _gameUIHandler.ViewPokemonParty(usage);
        ResetParticipantState();
    }

    private void StartPokemonPartySwap(int memberIndex)
    {
        _pokemonPartyHandler.OnMemberSelected -= StartPokemonPartySwap; 
        StartCoroutine(_pokemonPartyHandler.SwapMemberWithoutTurnUsage(memberIndex,participantKey));
    }

    public void ResetImagePosition()
    {
        pokemonImage.rectTransform.anchoredPosition = _defaultImagePosition;
    }
    public void ResetUiPosition()
    {
        _uiRect.anchoredPosition = _defaultUIPosition;
    }
    public void DeactivateParticipant()
    {
        if (!isActive) return;
        isActive = false;
        currentEnemies.Clear();       
        barriers.Clear();
        statChangeEffects.Clear();

        ResetImagePosition();
        ResetUiPosition();
        pokemonImage.color = Color.white;
        
        _turnBasedCombatHandler.OnNewTurn -= statusHandler.StunCheck;
        _turnBasedCombatHandler.OnNewTurn -= statusHandler.CheckStatChangeImmunity;
        _turnBasedCombatHandler.UnsubscribeFromMoveExecution(statusHandler.CheckTrapDuration);
        _turnBasedCombatHandler.UnsubscribeFromMoveExecution(statusHandler.ConfusionCheck);
        _turnBasedCombatHandler.UnsubscribeFromMoveExecution(statusHandler.NotifyHealing);
        
        pokemon.OnHealthChanged -= CheckIfFainted;
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
        previousMoveData = null;
        additionalTypeImmunity = null;
        
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
    public BattleParticipantKey GetPartnerKey()
    {
        switch (participantKey)
        {
            case BattleParticipantKey.PlayerPartner:
                return BattleParticipantKey.Player;
            case BattleParticipantKey.Enemy:
                return BattleParticipantKey.EnemyPartner;
            case BattleParticipantKey.EnemyPartner:
                return BattleParticipantKey.Enemy;
        }
        return BattleParticipantKey.PlayerPartner;
    }
    public BattleParticipant GetPartner()
    {
        return _battleHandler.GetParticipant(GetPartnerKey());
    }

    public bool ProtectedFromStatChange(bool isIncrease)
    {
        var protection = isIncrease? StatChangeability.ImmuneToIncrease
            :StatChangeability.ImmuneToDecrease;
        return statChangeEffects.Any(s => s.changeability == protection);
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
            var partner = GetPartner();
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
                DirectoryHandler.GetDirectory(AssetDirectory.Status) 
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
        {
            Debug.LogWarning($"multi level up on {participantKey}, check logic");
            //in-case of instant multi-level up
            return;
        }
        var evoData = new EvolutionInBattleData();
        evoData.participantToEvolve = this;
        evoData.evolutionIndex = evolutionIndex;
        _battleHandler.evolutionQueue.Add(evoData);
    }
    public void ActivateParticipant(bool initialCall)
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
        
        if (initialCall)
        {
            _turnBasedCombatHandler.SubToMoveExecution(statusHandler.CheckTrapDuration);
            _turnBasedCombatHandler.OnNewTurn += statusHandler.CheckStatChangeImmunity;
            _turnBasedCombatHandler.SubToMoveExecution(statusHandler.ConfusionCheck);
            _turnBasedCombatHandler.OnNewTurn += statusHandler.StunCheck;
            _turnBasedCombatHandler.SubToMoveExecution(statusHandler.NotifyHealing);
        }
        
        ActivateUI(doubleBattleUI, _battleHandler.isDoubleBattle);
        ActivateUI(singleBattleUI, !_battleHandler.isDoubleBattle);
        
        //these are reset each time participant is reset
        pokemon.OnHealthChanged += CheckIfFainted;
        if (isPlayer)
        {
            pokemon.OnEvolutionSuccessful += AddToEvolutionQueue;
            pokemon.OnLevelUp +=  ResetParticipantStateAfterLevelUp;
            pokemon.OnNewLevel += statData.SaveActualStats;
        }
    }

    private void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(pokemon.hasGender)
        {
            pokemonGenderImage.sprite = Resources.Load<Sprite>(
                DirectoryHandler.GetDirectory(AssetDirectory.UI)
                + pokemon.gender.ToString().ToLower());
        }
        else
        {
            pokemonGenderImage.gameObject.SetActive(false);
        }
    }
    public void DeactivateUI()
    {
        participantUI.SetActive(false);
    }
}
