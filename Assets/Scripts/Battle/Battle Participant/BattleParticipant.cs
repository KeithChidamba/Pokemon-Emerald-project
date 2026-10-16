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

    public Pokemon Pokemon { get; private set; }
    public PokemonStateHandler pokemonState;
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
    public bool awaitingRevival;
    
    public bool canAttack = true;
    public bool canBeDamaged = true;
        
    public bool isFlinched;
    public bool canBeFlinched = true;
    
    public bool isConfused;
    
    public bool canBeInfatuated = true;
    public bool isInfatuated;
    
    public bool canEscape = true;
     
    public Slider playerHpSlider;
    [FormerlySerializedAs("hpSliderColor")] public RawImage hpSliderImage;
    public GameObject[] doubleBattleUI;
    public GameObject participantUI;
    
    public SemiInvulnerabilityData semiInvulnerabilityData = new();
    public bool isSemiInvulnerable;
    public PreviousMove previousMoveData;
    public TurnCoolDown currentCoolDown;
    public PokemonTypeData additionalTypeImmunity;
    public List<TypeImmunityNegation> immunityNegations = new();
    public MoveLockData currentMoveLock;
    
    public Slider playerExpSlider;
    public GameObject[] singleBattleUI;
    
    public List<PokemonStateHandler> expReceivers;
    [SerializeField]private bool handlingFaintEvent;
    
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
        pokemonState = new PokemonStateHandler(_container);
        
        currentCoolDown = new TurnCoolDown(this, _moveUsageHandler);
        currentMoveLock = new MoveLockData { moveLocked = false,moveToLock = null};
        _defaultImagePosition = pokemonImage.rectTransform.anchoredPosition;
        _uiRect = participantUI.GetComponent<RectTransform>();
        _defaultUIPosition = _uiRect.anchoredPosition; 
        statusAnimationHandler.participant = this;
    }
    
    public void RemovePokemon()
    {
        Pokemon = null;
        pokemonState.pokemon = null;
    }
    public void ReceivePokemon(Pokemon pokemon)
    {
        pokemonState.pokemon = pokemon;
        Pokemon = pokemonState.pokemon;
    }
    public IEnumerator SetupEnemyAi(TrainerData trainerData,BattleParticipant partner = null)
    {
        pokemonTrainerAI = new EnemyAiHandler(_container,this);
        
        yield return pokemonTrainerAI.SetupTrainerForBattle(trainerData);
        if (partner is null)
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
        foreach (var ev in Pokemon.effortValues)
        {
            _pokemonOperationsHandler.CalculateEvForStat(ev.stat,ev.eVAmount,enemy.Pokemon);
        }
    }
    
    public void AddToExpList(PokemonStateHandler state)
    {
        if(!expReceivers.Contains(state))
        {
            expReceivers.Add(state);
        }
    }
    private IEnumerator DistributeExp()
    {
        var expFromEnemy = Pokemon.CalculateExperience();
        
        // Remove fainted or invalid Pokémon
        expReceivers.RemoveAll(p => pokemonState.pokemon.hp <= 0);
        
        //only player pokemon receive exp
        var filter =  new List<PokemonStateHandler>();
        foreach (var receiver in expReceivers)
        {
            if (_pokemonPartyHandler.Party.Contains(receiver.pokemon))
            {
                filter.Add(receiver);
            }
        }
        expReceivers.Clear();
        expReceivers.AddRange(filter);
        
        if (expReceivers.Count < 1) yield break;

        // Separate holders and participants
        var expShareHolders = new List<PokemonStateHandler>();
        foreach (var receiver in expReceivers)
        {
            if (!receiver.pokemon.hasItem) continue;
            var expHeldItem = receiver.pokemon.heldItem.GetDynamicModule<ExpModifierInfo>();
            if (expHeldItem is not null)
            {
                var hasExpShare = expHeldItem.modifier == ExpModifier.ExpShare;
                if (hasExpShare)
                {
                    expShareHolders.Add(receiver);
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
        else
        {
            expShareTotal = 0;
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
        if (Pokemon.hp > 0) return;
        
        if (_battleHandler.HasFaintedParticipant(this)) return;
        
        Pokemon.statusEffect = StatusEffect.None;
        _battleHandler.AddFaintedParticipant(this);
        Pokemon.DetermineFriendshipLevelChange(false, FriendshipModifier.Fainted);
    }
    public IEnumerator HandleFaintLogic()
    {
        yield return _dialogueHandler.AwaitAllDialogue();
        if (!isPlayer)
        {
            yield return DistributeExp();
            
            foreach (var enemy in currentEnemies)
                if(enemy.isActive)
                    GiveEVs(enemy);

            if (!_battleHandler.isTrainerBattle)
            {
                yield return _wildPokemonHandler.EndWildBattle(BattleEndState.PlayerWon);
                EndFaintEvent();
            }
            else
            {
                ResetParticipantState();
                yield return pokemonTrainerAI.CheckIfLoss();
                EndFaintEvent();
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
                _battleHandler.CountValidParticipants();
                EndFaintEvent();
            }
        }
        yield return null;
    }

    public void SetupSwitchOut(PartyUsage usage = PartyUsage.SwapOut)
    {
        _pokemonPartyHandler.selectedMemberIndex = (int)participantKey;
        _dialogueHandler.EndDialogue();
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
        
        ResetImagePosition();
        ResetUiPosition();
        pokemonImage.color = Color.white;
        
        _turnBasedCombatHandler.OnNewTurn -= statusHandler.StunCheck;
        
        _turnBasedCombatHandler.UnsubscribeFromMoveExecution(statusHandler.CheckTrapDuration);
        _turnBasedCombatHandler.UnsubscribeFromMoveExecution(statusHandler.ConfusionCheck);
        _turnBasedCombatHandler.UnsubscribeFromMoveExecution(statusHandler.NotifyHealing);
        
        pokemonState.OnHealthChanged -= CheckIfFainted;
        pokemonState.ResetMoveData();
    }

    public void ResetParticipantState()
    {
        statData.LoadActualStats();
        statData.ResetBattleState(Pokemon);
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
            Pokemon.pokemonDisplayName = Pokemon.pokemonName;
            pokemonState.OnEvolutionSuccessful -= AddToEvolutionQueue;
            pokemonState.OnNewLevel -= statData.SaveActualStats;
            pokemonState.OnLevelUp -= ResetParticipantStateAfterLevelUp;
        }
        pokemonState.OnHealthChanged -= CheckIfFainted;
        if (Pokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            Pokemon.statusEffect = StatusEffect.Poison;
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
        return _battleHandler.GetTeam(participantKey)
            .statChangeEffects.Any(s => s.changeability == protection);
    }
    
    private void UpdateUI()
    {
        PokemonOperations.UpdateHealthPhase(pokemonState,hpSliderImage); 
        pokemonNameText.text = rawName;
        pokemonLevelText.text = "Lv: " + Pokemon.currentLevel;
        if (isPlayer && !_battleHandler.isDoubleBattle)
        {
            pokemonHealthText.text = Pokemon.hp + "/" + Pokemon.maxHp;
            playerExpSlider.value = Pokemon.currentExpAmount;
            playerExpSlider.maxValue = Pokemon.nextLevelExpAmount;
            playerExpSlider.minValue = Pokemon.currentLevelExpAmount;
        }
        playerHpSlider.value = Pokemon.hp;
        playerHpSlider.maxValue = Pokemon.maxHp;
        if(Pokemon.hp<=0) Pokemon.hp = 0;
    }
    public void RefreshStatusEffectImage()
    {
        if (Pokemon.statusEffect == StatusEffect.None)
            statusImage.gameObject.SetActive(false);
        else
        {
            statusImage.gameObject.SetActive(true);
            statusImage.sprite = Resources.Load<Sprite>(
                DirectoryHandler.GetDirectory(AssetDirectory.Status) 
             + Pokemon.statusEffect.ToString().ToLower());
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
        playerHpSlider.minValue = 0;
        isActive = true;
        pokemonImage.sprite = isPlayer?Pokemon.backPicture : Pokemon.frontPicture;
        rawName = isPlayer? Pokemon.pokemonDisplayName :  Pokemon.pokemonDisplayName.Replace("Foe ", "");
        ActivateGenderImage();
        if (Pokemon.statusEffect == StatusEffect.BadlyPoison)
        {
            Pokemon.statusEffect = StatusEffect.Poison;
        }
        
        _moveUsageHandler.ApplyStatusToVictim(this, Pokemon.statusEffect);
        
        if (initialCall)
        {
            _turnBasedCombatHandler.SubToMoveExecution(statusHandler.CheckTrapDuration);
            
            _turnBasedCombatHandler.SubToMoveExecution(statusHandler.ConfusionCheck);
            _turnBasedCombatHandler.OnNewTurn += statusHandler.StunCheck;
            _turnBasedCombatHandler.SubToMoveExecution(statusHandler.NotifyHealing);
        }
        
        ActivateUI(doubleBattleUI, _battleHandler.isDoubleBattle);
        ActivateUI(singleBattleUI, !_battleHandler.isDoubleBattle);
        
        //these are reset each time participant is reset
        pokemonState.OnHealthChanged += CheckIfFainted;
        if (isPlayer)
        {
            pokemonState.OnEvolutionSuccessful += AddToEvolutionQueue;
            pokemonState.OnLevelUp +=  ResetParticipantStateAfterLevelUp;
            pokemonState.OnNewLevel += statData.SaveActualStats;
        }
    }

    private void ActivateGenderImage()
    {
        pokemonGenderImage.gameObject.SetActive(true);
        if(Pokemon.hasGender)
        {
            pokemonGenderImage.sprite = Resources.Load<Sprite>(
                DirectoryHandler.GetDirectory(AssetDirectory.UI)
                + Pokemon.gender.ToString().ToLower());
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
