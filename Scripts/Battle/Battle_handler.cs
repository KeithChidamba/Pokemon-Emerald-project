using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum BattleParticipantKey
{
    Player = 0,PlayerPartner = 1,Enemy = 2,EnemyPartner = 3
}
public enum BattleEndState
{
    PlayerRanAway,PokemonRanAway,PlayerLost,PlayerWon,BattleTerminated,None
}
public enum PlayerTurnUsage
{
    UseItem,SwitchPokemonIn,Fight,None
}
public class Battle_handler : MonoBehaviour, IInjectable
{
    public GameObject battleUI;
    public GameObject movesUI;
    public GameObject optionsUI;
    public GameObject[] battleOptions; 
    public GameObject optionSelector;
    public GameObject moveSelector;
    public GameObject overWorld;
    public Text movePowerPointsText;
    public Text moveTypeText;
    public Text[] availableMovesText;

    private Dictionary<BattleParticipantKey, Battle_Participant> battleParticipants = new();
    [SerializeField]private List<Battle_Participant> currentParticipants = new();
    public IReadOnlyList<Battle_Participant> GetParticipants => currentParticipants;
    [SerializeField]private Battle_Participant[] battleParticipantInstances;
    
    [SerializeField]private List<Battle_Participant> faintQueue = new();
    [SerializeField]private bool handlingFaintEvent;
    
    public bool BattleOver { get; private set; }
    public bool BattleInProgress{ get; private set; }
    
    public bool isTrainerBattle;
    public bool isDoubleBattle;
    public int validParticipantCount;
  
    public BattleEndState battleEndState;
    
    public float participantPositionOffset = 100;
    private List<Vector2> _defaultPokemonImagePositions = new();
    public BattleType currentBattleType;
    public enum BattlesStyle {Switch,Set };
    public BattlesStyle currentBattleStyle;
    public List<EvolutionInBattleData> evolutionQueue;
    private PlayerTurnUsage _previousTurnUsage;
    
    public event Action<Battle_Participant> OnParticipantFainted;
    public event Action OnBattleEnd;

    public event Action<bool> OnBattleResult;
    public event Action OnSwitchIn;
    public event Action<Battle_Participant> OnSwitchOut;
    public event Action<BattleParticipantKey> OnEnemySelected;
    private Action _checkParticipantsEachTurn;
    
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Pokemon_party _playerParty;
    private InputStateHandler _inputStateHandler;
    private Game_ui_manager _gameUIHandler;
    private BattleIntro _battleIntroHandler;
    private Game_Load _gameLoadingHandler;
    private Pokemon_party _pokemonPartyHandler;
    private overworld_actions _overworldActions;
    private PokemonOperations _pokemonOperations;
    private WildPokemonAiHandler _wildPokemonHandler;
    private Area_manager _areaHandler;
    private Player_movement _playerMovementHandler;
    
    
    public void Inject(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _playerParty = container.Resolve<Pokemon_party>();
        _wildPokemonHandler = container.Resolve<WildPokemonAiHandler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _areaHandler = container.Resolve<Area_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _overworldActions = container.Resolve<overworld_actions>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _pokemonOperations = container.Resolve<PokemonOperations>();
        gameObject.SetActive(true);
    }
    public void OnInject()
    {
        battleParticipants.Add(BattleParticipantKey.Player,battleParticipantInstances[0]);
        battleParticipants.Add(BattleParticipantKey.PlayerPartner,battleParticipantInstances[1]);
        battleParticipants.Add(BattleParticipantKey.Enemy,battleParticipantInstances[2]);
        battleParticipants.Add(BattleParticipantKey.EnemyPartner,battleParticipantInstances[3]);
        
        currentParticipants.AddRange(battleParticipantInstances);
        
        _turnBasedCombatHandler.OnNewTurn += ResetAi;
        _turnBasedCombatHandler.OnNewTurn += AllowPlayerInput;
        _checkParticipantsEachTurn = ()=> CheckParticipantStates();
        _turnBasedCombatHandler.OnNewTurn += _checkParticipantsEachTurn;
        _turnBasedCombatHandler.OnTurnsCompleted += ResetPlayersTurnUsage;
    }

    public IEnumerator AwaitBattleCompletion()
    {
        yield return new WaitUntil(() => !BattleInProgress);
    }
    public Battle_Participant GetParticipant(BattleParticipantKey participantKey)
    {
        return battleParticipants[participantKey];
    }
    public Battle_Participant GetCurrentParticipant()
    {
        return currentParticipants[_turnBasedCombatHandler.CurrentTurnIndex];
    }
    public void SetBattleStyle(int settingIndex)
    {
        currentBattleStyle = settingIndex switch
        {
            0 => BattlesStyle.Switch,
            1 => BattlesStyle.Set,
            _ => BattlesStyle.Switch
        };
    }

    public void EnableBattleMessage(InputState currentState)
    {
        if (currentState.stateName == InputStateName.PokemonBattleOptions)
        {
            _dialogueHandler.DisplaySpecific("What will you do?",DialogType.BattleDisplayMessage);
        }
    }
    private void AllowEnemySelection(int currentMoveIndex)
    {
        var currentPlayerParticipant = GetCurrentParticipant();
        if (!isDoubleBattle)
        {
            //if single battle, auto aim at enemy
            ExecutePlayersMove(currentMoveIndex,BattleParticipantKey.Enemy);
            return;
        }

        var currentMove = currentPlayerParticipant.pokemon.moveSet[currentMoveIndex];
        if (currentMove.isSelfTargeted || currentMove.isMultiTarget)
        {
            //then the enemy targeted doesn't matter so select any active one
            var currentPlayerEnemy = currentParticipants.First(a => a.isActive & !a.isPlayer);
            ExecutePlayersMove(currentMoveIndex,currentPlayerEnemy.participantKey);
            return;
        }

        BattleParticipantKey currentEnemyKey = BattleParticipantKey.Enemy;
        OnEnemySelected += ChangeSelectedEnemy;
        void ChangeSelectedEnemy(BattleParticipantKey newKey)
        {
            currentEnemyKey = newKey;
        }
        
        var enemySelectables = new List<SelectableUI>();
        for (var i = 2; i < currentParticipants.Count; i++)
        {
            enemySelectables.Add( new (currentParticipants[i].pokemonImage.gameObject, 
               () => ExecutePlayersMove(currentMoveIndex,currentEnemyKey),true));
        }
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonBattleEnemySelection
            ,InputStateGroup.PokemonBattle,
            stateDirection:InputDirection.Horizontal, selectableUis:enemySelectables, selecting:true
            ,onExit:ResetEnemySelection,onClose:ResetEnemySelection));
    }

    private void ResetEnemySelection()
    {
        OnEnemySelected = null;
        ResetEnemyColor();
    }
    private void ExecutePlayersMove(int currentMoveIndex, BattleParticipantKey enemyKey)
    {//selecting enemy only happens in double battle
        var currentPlayerParticipant = GetCurrentParticipant();
        GetParticipant(enemyKey).pokemonImage.color = Color.HSVToRGB(0,0,100);
        UseMove(currentPlayerParticipant.pokemon.moveSet[currentMoveIndex], currentPlayerParticipant,enemyKey); 
    }

    public void ResetEnemyColor()
    {
        foreach (var participant in currentParticipants)
        {
            participant.pokemonImage.color = Color.HSVToRGB(0,0,100);
        }
    }
    public void SelectEnemy(int indexChange,BattleParticipantKey previousKey = BattleParticipantKey.Enemy)
    {
        var partner= GetCurrentParticipant().GetPartner(); 
        //can attack partner and enemies
        var expectedTargets = new [] {partner.participantKey
            ,BattleParticipantKey.Enemy
            ,BattleParticipantKey.EnemyPartner};
        
        var validTargets = expectedTargets.Where(key => battleParticipants[key].isActive).ToArray();
        
        var currentTargetIndex = Array.IndexOf(validTargets,previousKey);      
        
        var choiceIndex = Mathf.Clamp(currentTargetIndex + indexChange,0,validTargets.Length-1);
        
        var currentEnemyIndex = validTargets[choiceIndex];
        battleParticipants[currentEnemyIndex].pokemonImage.color = Color.HSVToRGB(17,96,54);
        
        OnEnemySelected?.Invoke(currentEnemyIndex);
    }
    public void SetupOptionsAfterTurnReset(InputState currentState)
    {
        if (currentState.stateName != InputStateName.PokemonBattleOptions) return;
        _inputStateHandler.OnStateRemoved -= SetupOptionsAfterTurnReset;
        SetupOptionsInput();
    }
    private void SetupOptionsInput()
    {
        var battleOptionSelectables = new List<SelectableUI>
        {
            new(battleOptions[0],
                LoadMoveInputAndText,
                true),
            new(battleOptions[1], 
                ()=> RemoveBattleTextAndInvoke(_gameUIHandler.ValidateBagView),
                true),
            new(battleOptions[2], 
                () => RemoveBattleTextAndInvoke(()=>_gameUIHandler.ViewPokemonParty(PartyUsage.General)),
                true),
            new(battleOptions[3], 
                () => StartCoroutine(RunAway()),
                true)
        };
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonBattleOptions
            , InputStateGroup.PokemonBattle, false,
            optionsUI, InputDirection.Grid, battleOptionSelectables,
            optionSelector,true,true
            ,onExit:_turnBasedCombatHandler.RemoveTurn, updateExit:ConditionsForExit),true);
        
        _inputStateHandler.OnStateLoaded += HideDuringDialogue;
        _inputStateHandler.OnStateRemoved += CleanEvents;
        return;
        void CleanEvents(InputState oldState)
        {
            if (oldState.stateName == InputStateName.PokemonBattleOptions)
            {
                _inputStateHandler.OnStateLoaded -= HideDuringDialogue;
                _inputStateHandler.OnStateRemoved -= CleanEvents;
            }
        }
        void HideDuringDialogue(InputState newState)
        {
            if (newState.stateName == InputStateName.BattleDialoguePlaceHolder)
            {
                optionsUI.SetActive(false);
            }
        }
        void RemoveBattleTextAndInvoke(Action callBack)
        {
            _dialogueHandler.DisplaySpecific(string.Empty,DialogType.BattleDisplayMessage);
            callBack.Invoke();
        }
    }
    private bool ConditionsForExit()
    {
        //Check if player can reset their move selection
        return isDoubleBattle && _turnBasedCombatHandler.CurrentTurnIndex > 0
                              //if irreversible turn usage occured, cant remove turn
                              && _previousTurnUsage == PlayerTurnUsage.Fight
                              //if partner is semi-invulnerable, cant remove turn
                              && !currentParticipants[_turnBasedCombatHandler.CurrentTurnIndex - 1]
                                  .isSemiInvulnerable
                              //if partner is cooling down, cant remove turn
                              && !currentParticipants[_turnBasedCombatHandler.CurrentTurnIndex - 1]
                                  .currentCoolDown.isCoolingDown;
    }
    public void SetPlayerTurnUsage(PlayerTurnUsage usage)
    {
        _previousTurnUsage = usage;
    }
    private void ResetPlayersTurnUsage()
    {
        SetPlayerTurnUsage(PlayerTurnUsage.None);
    }
    private void AllowPlayerInput()
    {
        if (_turnBasedCombatHandler.CurrentTurnIndex > 1) return;
        var currentParticipant = GetCurrentParticipant();
        if (currentParticipant.isSemiInvulnerable) return;
        if (currentParticipant.currentCoolDown.isCoolingDown) return;
        _inputStateHandler.ResetRelevantUi(new[]
        {
            InputStateName.PokemonBattleEnemySelection, InputStateName.PlaceHolder
        });
    }
    private IEnumerator SetValidParticipants()
    {
        foreach (var participant in currentParticipants)
        {
            if(!participant.activeForBattle)continue;
            
            if (participant.pokemon != null)
                if (participant.pokemon.hp > 0)
                {
                    yield return SetupParticipant(participant, participant.pokemon, initialCall: true);
                }
        }
    }
    private IEnumerator SetupBattleSequence(Biome biome)
    {
        foreach (var participant in currentParticipants)
        {
            var pos = participant.pokemonImage.rectTransform.anchoredPosition;
            _defaultPokemonImagePositions.Add(pos);
            var horizontalShift = participant.isPlayer
                ? pos.x - (isDoubleBattle ? 0 : participantPositionOffset)
                : pos.x + (isDoubleBattle ? 0 : participantPositionOffset);
            participant.pokemonImage.rectTransform.anchoredPosition = new Vector2(horizontalShift, pos.y);
        }

        _battleIntroHandler.currentBiome = biome;
        _battleIntroHandler.SetPlatformSprite();
        //load visuals based on area
        overWorld.SetActive(false);
        battleUI.SetActive(true);
        BattleInProgress = true;
        _gameUIHandler.RemoveBlackScreen();
        
        if(isTrainerBattle)
            yield return StartCoroutine(_battleIntroHandler.PlayTrainerIntroSequence());
        else
            yield return StartCoroutine(_battleIntroHandler.PlayWildIntroSequence());
        
        _gameLoadingHandler.playerData.playerPosition = _playerMovementHandler.GetPlayerPosition();
        _inputStateHandler.OnStateChanged += EnableBattleMessage;
        
        SetupOptionsInput();
        _inputStateHandler.ResetRelevantUi(InputStateName.PlaceHolder);

        _turnBasedCombatHandler.StartFreshTurn();
    }
    private void ResetAi()
    {
        if(!isTrainerBattle)return;
        var currentParticipant = GetCurrentParticipant();
        if(!currentParticipant.isActive) return;
        if(currentParticipant.isPlayer)return;
        currentParticipant.pokemonTrainerAI.MakeBattleDecision();
    }
    
    public IEnumerator SetBattleTypeAndStart(TrainerData data)
    {
        _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.Battle);
        _pokemonPartyHandler.SortByFainted();
        currentBattleType = data.battleType;
        switch (data.battleType)
        {
            case BattleType.Single:
                yield return StartCoroutine(StartSingleBattle(data));
                break;
            case BattleType.SingleDouble:
                yield return StartCoroutine(StartSingleDoubleBattle(data));
                break;
        }
    }
    private IEnumerator DisplayTrainerMessage(string message)
    {
        _inputStateHandler.AddPlaceHolderState();
        _dialogueHandler.DisplayDetails(message,false);
        yield return _dialogueHandler.WaitForDialogueCompletion();
        yield return new WaitForSecondsRealtime(1f);
        _dialogueHandler.EndDialogue();
    }
    public IEnumerator StartWildBattle(Pokemon enemy,Biome biome)
    {
        StartCoroutine(_gameUIHandler.FadeInBlackScreen());
        _pokemonPartyHandler.SortByFainted();
        
        isTrainerBattle = false;
        isDoubleBattle = false;
        var player = GetParticipant(BattleParticipantKey.Player);
        var wildPokemon = GetParticipant(BattleParticipantKey.Enemy);
        player.activeForBattle = true;
        wildPokemon.activeForBattle = true;
        
        //set initial pokemon and enemy for player
        player.pokemon = _pokemonPartyHandler.Party[0];
        player.currentEnemies.Add(wildPokemon);      
        //setup wild pokemon enemy AI 
        wildPokemon.pokemon = enemy;
        wildPokemon.currentEnemies.Add(player);
        _wildPokemonHandler.participant = wildPokemon;
        wildPokemon.AddToExpList(player.pokemon);
        _wildPokemonHandler.SetBattleState();
        //setup battle
        yield return SetValidParticipants();
        yield return new WaitForSecondsRealtime(0.55f);
        StartCoroutine(SetupBattleSequence(biome));
    }
    private IEnumerator StartSingleBattle(TrainerData trainerData) //single trainer battle
    {
        yield return DisplayTrainerMessage(trainerData.battleIntroMessage);
        isTrainerBattle = true;
        isDoubleBattle = false; 
        var player = GetParticipant(BattleParticipantKey.Player);
        var enemy = GetParticipant(BattleParticipantKey.Enemy);
        player.activeForBattle = true;
        enemy.activeForBattle = true;
        
        //set initial pokemon and enemy for player
        player.pokemon = _pokemonPartyHandler.Party[0];
        player.currentEnemies.Add(enemy);
        //setup enemy AI
        enemy.currentEnemies.Add(player);

        StartCoroutine(_gameUIHandler.FadeInBlackScreen());
        yield return enemy.SetupEnemyAi(trainerData);
        
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemy.AddToExpList(player.pokemon);
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(enemy.pokemonTrainerAI.trainerData.TrainerLocationData.biome));
    }

    private IEnumerator StartSingleDoubleBattle(TrainerData trainerData) //1v1 double battle
    {
        yield return DisplayTrainerMessage(trainerData.battleIntroMessage);
        isTrainerBattle = true;
        isDoubleBattle = true; 
        var alivePartyPokemon = _pokemonPartyHandler.GetLivingPokemon();
        var playerPartnerAvailable = alivePartyPokemon.Count > 1;
        
        var player =GetParticipant(BattleParticipantKey.Player);
        player.activeForBattle = true;
        var playerPartner = GetParticipant(BattleParticipantKey.PlayerPartner);
        playerPartner.activeForBattle = playerPartnerAvailable;
        var enemy = GetParticipant(BattleParticipantKey.Enemy);
        enemy.activeForBattle = true;
        var enemyPartner = GetParticipant(BattleParticipantKey.EnemyPartner);
        enemyPartner.activeForBattle = true;
        
        //set initial pokemon for player
        player.pokemon = alivePartyPokemon[0];
        if(playerPartner.activeForBattle) playerPartner.pokemon = alivePartyPokemon[1];
        
        //setup trainer ai for enemy participants
        StartCoroutine(_gameUIHandler.FadeInBlackScreen());
        yield return enemy.SetupEnemyAi(trainerData,enemyPartner);
        
        //set initial pokemon for enemies
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemyPartner.pokemon = enemyPartner.pokemonTrainerAI.trainerParty[1];
        
        //set enemies of all participants
        SetupEnemies(GetParticipant(BattleParticipantKey.Enemy),
            GetParticipant(BattleParticipantKey.Player));
        SetupEnemies(GetParticipant(BattleParticipantKey.EnemyPartner),
            GetParticipant(BattleParticipantKey.PlayerPartner));
        
        //add player's pokemon to get exp from all current enemies
        foreach (var enemyParticipant in player.currentEnemies)//player and partner have same enemies
        {
            enemyParticipant.AddToExpList(player.pokemon);
            if(playerPartner.activeForBattle) enemyParticipant.AddToExpList(playerPartner.pokemon);
        }
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(enemy.pokemonTrainerAI.trainerData.TrainerLocationData.biome));

        void SetupEnemies(Battle_Participant currentEnemy,Battle_Participant currentPlayerParticipant)
        {
            player.currentEnemies.Add(currentEnemy);
            if(playerPartner.activeForBattle) playerPartner.currentEnemies.Add(currentEnemy);
            
            if(!currentPlayerParticipant.activeForBattle) return;
            
            enemy.currentEnemies.Add(currentPlayerParticipant);
            enemyPartner.currentEnemies.Add(currentPlayerParticipant);
        }
    }

    public IEnumerator SetupParticipant(Battle_Participant participant,Pokemon newPokemon,bool initialCall=false)
    {
        OnSwitchOut?.Invoke(participant);
        
        participant.isPlayer = participant.participantKey is BattleParticipantKey.Player
            or BattleParticipantKey.PlayerPartner;
        
        if (participant.isPlayer)
        {
            newPokemon.pokemonDisplayName = newPokemon.nickName;
        }
        else
        {
            newPokemon.pokemonDisplayName = "Foe " + newPokemon.pokemonName;
        }
        
        if(!initialCall)
        {
            participant.pokemon = newPokemon;
            if (participant.isPlayer)
            {
                _dialogueHandler.DisplayBattleInfo(_gameLoadingHandler.playerData.playerName
                                                            +" sent out "+newPokemon.pokemonDisplayName);
                
                //add enemies to exp list of new player pokemon
                foreach (var enemyParticipant in participant.currentEnemies)
                    enemyParticipant.AddToExpList(newPokemon);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo(participant.pokemonTrainerAI.trainerData.TrainerName
                                                            +" sent out "+newPokemon.pokemonDisplayName);
                
                //add player participants to get exp from switched in enemy
                foreach (var playerParticipant in participant.currentEnemies)
                    participant.AddToExpList(playerParticipant.pokemon);
            }
        }
        //setup participant for battle
        participant.statData.SaveActualStats();
        participant.ActivateParticipant();
        participant.abilityHandler.SetAbilityMethod();
        CheckParticipantStates(initialCall);
        OnSwitchIn?.Invoke();
        yield return null;
    }

    public List<Battle_Participant> GetValidParticipants()
    {
        var validList = new List<Battle_Participant>();
        foreach (var participant in currentParticipants)
        {
            if (participant.isActive)
            {
                if (participant.pokemon is{ hp:>0})
                {
                    validList.Add(participant);
                }
            }
        }
        return validList;
    }
    public void CheckParticipantStates(bool initialCall=false)
    {
        validParticipantCount = 0;
        foreach (var participant in currentParticipants)
        {
            if (participant.pokemon == null) continue;
            validParticipantCount++;
            //if revived during double battle for example
            if(initialCall)continue;
            if (participant.pokemon.hp > 0 & !participant.isActive)
                participant.ActivateParticipant();
        }
    }
    public void RefreshStatusEffectUI()
    {
        var validParticipants = GetValidParticipants();
        validParticipants.ForEach(p=>p.RefreshStatusEffectImage());
    }
    void LoadMoveInputAndText()
    { 
        var currentPlayerParticipant = GetCurrentParticipant();
        bool emptyMoves = true;
        foreach (var move in currentPlayerParticipant.pokemon.moveSet)
        {
            if(move.powerpoints>0)
            {
                emptyMoves = false;
                break;
            }
        }
        if (emptyMoves)
        {
            _turnBasedCombatHandler.SaveStruggleTurn(currentPlayerParticipant);
            return;
        }
        
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < currentPlayerParticipant.pokemon.moveSet.Count; i++)
        {
            availableMovesText[i].text = currentPlayerParticipant.pokemon.moveSet[i].moveName;
            var moveIndex = i;
            moveSelectables.Add( new (availableMovesText[i].gameObject,() => AllowEnemySelection(moveIndex),true));
        }
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonBattleMoveSelection
            ,InputStateGroup.PokemonBattle,false,
            movesUI, InputDirection.Grid, moveSelectables,
            moveSelector,true,true,ResetMoveUsability,ResetMoveUsability));
        
        for (var i = currentPlayerParticipant.pokemon.moveSet.Count; i < 4; i++)//only show available moves
            availableMovesText[i].text = "";
        void ResetMoveUsability()
        {
            movesUI.SetActive(false);
        }
    }
    public void UseMove(Move move,Battle_Participant user, BattleParticipantKey enemyKey)
    {
        if(move.powerpoints==0)return;
        move.powerpoints--;

        Turn currentTurn = new Turn(TurnUsage.Attack,move, user.participantKey
            ,enemyKey
            , user.pokemon.pokemonID
            ,GetParticipant(enemyKey).pokemon.pokemonID);
        _turnBasedCombatHandler.SaveTurn(currentTurn);
    }

    public void SelectMove(int moveIndex)
    {
        var currentPlayerParticipant = GetCurrentParticipant();
        var currentMove = currentPlayerParticipant.pokemon.moveSet[moveIndex];
        movePowerPointsText.text = "PP: " + currentMove.powerpoints+ "/" + currentMove.maxPowerpoints;
        movePowerPointsText.color = currentMove.powerpoints == 0? Color.red : Color.black;
        moveTypeText.text = currentMove.type.GetTypeName;
    }
    private float PrizeMoneyModifier()
    {
        var playerParticipants = currentParticipants
            .Where(p => p.isActive & p.isPlayer).ToList();
        foreach(var participant in playerParticipants)
        {
            if (!participant.pokemon.hasItem)continue;
            var heldItem = participant.pokemon.heldItem;
            if (heldItem.itemType == ItemType.GainMoney)
            {
                var moneyBonus = heldItem.GetDynamicModule<ItemEffectInfo>().effectValue;
                return moneyBonus;
            }
        }
        return 1f;
    }
    public void AddFaintedParticipant(Battle_Participant participant)
    {
        faintQueue.Add(participant);
    }
    public void StartFaintEvent()
    {
        if(!handlingFaintEvent)
        {
            StartCoroutine(FaintSequence());
        }
    }
    public IEnumerator AwaitFaintQueue()
    {
        yield return new WaitUntil(() => faintQueue.Count == 0);
    }
    private IEnumerator FaintSequence()
    {
        handlingFaintEvent = true;
        while (faintQueue.Count > 0)
        {
            var faintedParticipant = faintQueue[0];
            faintedParticipant.BeginFaintEvent();
            _dialogueHandler.DisplayBattleInfo(faintedParticipant.pokemon.pokemonDisplayName + " fainted!");
            var pkmImageRect = faintedParticipant.pokemonImage.rectTransform;
            var rectHeight = pkmImageRect.rect.height;
            var target = new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y-rectHeight);
            yield return _dialogueHandler.AwaitAllDialogue();
            
            StartCoroutine(BattleVisuals.SlideRect(pkmImageRect, pkmImageRect.anchoredPosition, target, 300f));
            
            var participantUIRect = faintedParticipant.participantUI.GetComponent<RectTransform>(); 
            var targetForUI = new Vector2(participantUIRect.anchoredPosition.x, participantUIRect.anchoredPosition.y-400f);
            yield return StartCoroutine(BattleVisuals.SlideRect(participantUIRect,participantUIRect.anchoredPosition, targetForUI, 900f));
            
            if (!faintedParticipant.isPlayer)
            {
                yield return new WaitForSeconds(0.05f);
                faintedParticipant.participantUI.SetActive(false);
                yield return new WaitForSeconds(0.25f);
                faintedParticipant.pokemonImage.color = new Color(0, 0, 0, 0);
            }
            
            yield return faintedParticipant.HandleFaintLogic();
            
            OnParticipantFainted?.Invoke(faintedParticipant);
            
            if (BattleInProgress)
            {   
                participantUIRect.anchoredPosition = new Vector2(participantUIRect.anchoredPosition.x,
                    participantUIRect.anchoredPosition.y + 400f);
                
                if(faintedParticipant.isActive)
                {

                    faintedParticipant.participantUI.SetActive(true);
                    pkmImageRect.anchoredPosition =
                        new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y + rectHeight);
              
                    if (!faintedParticipant.isPlayer)
                    {
                        faintedParticipant.pokemonImage.color = Color.white;
                        yield return _battleIntroHandler.PokemonIntroAnimation(faintedParticipant);
                    }
                }
            }
            
            faintQueue.RemoveAt(0);
            yield return null;
        }
        handlingFaintEvent = false;
    }
    public void StartExpEvent(Pokemon pokemon)
    {
        StartCoroutine(ExpGainSequence(pokemon));
    } 
    private IEnumerator ExpGainSequence(Pokemon pokemonGainExp)
    {
        bool awaitingEventCompletion = true; 
        Action<Pokemon> awaitEvent = (pokemon)=> awaitingEventCompletion = false;
        pokemonGainExp.OnExpGainComplete += awaitEvent;
        yield return new WaitUntil(() => !awaitingEventCompletion);
        pokemonGainExp.OnExpGainComplete -= awaitEvent;
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    
    public void EndBattle(BattleEndState state,Pokemon lastDefeatedOpponent)
    {
        if (BattleOver) return;
        battleEndState = state;
        BattleOver = true;
        StartCoroutine(ProcessBattleEnd());
        return;
        IEnumerator ProcessBattleEnd()
        {
            yield return _dialogueHandler.AwaitAllDialogue();
            _inputStateHandler.OnStateChanged -= EnableBattleMessage;
            _inputStateHandler.AddPlaceHolderState();
            var playerData = _gameLoadingHandler.playerData;
            var playerName = playerData.playerName;
            string wildPokemonName = "";
            if (!isTrainerBattle)
            {
                wildPokemonName = _wildPokemonHandler.participant.pokemon.pokemonDisplayName;
            }
            
            switch (battleEndState)
            {
                case BattleEndState.BattleTerminated:
                    _dialogueHandler.DisplayBattleInfo("the battle ended");
                    break;
                case BattleEndState.PlayerRanAway:
                    _dialogueHandler.DisplayBattleInfo(playerName + " ran away");
                    break;
                case BattleEndState.PlayerWon:
                    yield return HandleEvolutions();
                    if (isTrainerBattle)
                    {
                        var anyEnemy = GetParticipant(BattleParticipantKey.Player)
                            .currentEnemies[0].pokemonTrainerAI.trainerData;
                        
                        var baseMoneyPayout = anyEnemy.BaseMoneyPayout;
                        
                        _dialogueHandler.DisplayBattleInfo(playerName + " defeated " + anyEnemy.TrainerName);
                        
                        yield return _battleIntroHandler.ShowEnemiesAfterBattle();
                        _dialogueHandler.DisplayBattleInfo(anyEnemy.battleLossMessage);
                        var moneyGained = baseMoneyPayout * lastDefeatedOpponent.currentLevel * PrizeMoneyModifier();
                        playerData.playerMoney += (int)math.floor(moneyGained);
                        _dialogueHandler.DisplayBattleInfo(playerName + " received P" + moneyGained);
                    }
                    else
                    {
                        _dialogueHandler.DisplayBattleInfo(playerName + " defeated " + wildPokemonName);
                    }
                    break;
                case BattleEndState.PlayerLost:
                    if (isTrainerBattle)
                    {
                        //last participant is always not null in this situation
                        var anyEnemy =  GetParticipant(BattleParticipantKey.Player).currentEnemies[0];
                        
                        var baseMoneyPayout = anyEnemy.pokemonTrainerAI.trainerData.BaseMoneyPayout;
                        
                        var playersLastParticipant = currentParticipants.First(p => p.isActive & p.isPlayer);
                        
                        var victoriousOpponent = playersLastParticipant.currentEnemies
                            .First(p=>p.isActive).pokemon;
                        playerData.playerMoney -= baseMoneyPayout * playerData.numBadges * victoriousOpponent.currentLevel;
                    }
                    else
                    {
                        var partyPokemon = _pokemonPartyHandler.Party
                            .Where(pokemon => pokemon != null).ToList();
                        
                        var highestLevelOfParty = partyPokemon
                            .OrderByDescending(p => p.currentLevel)
                            .First().currentLevel;

                        playerData.playerMoney -= 100 * highestLevelOfParty;
                    }
                    _dialogueHandler.DisplayBattleInfo("All your pokemon have fainted");
                    break;
                case BattleEndState.PokemonRanAway:
                    _dialogueHandler.DisplayBattleInfo(wildPokemonName+" ran away");
                    break;
            }
            
            yield return _dialogueHandler.AwaitAllDialogue();
            _dialogueHandler.EndDialogue();
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonBattle);
            yield return _battleIntroHandler.BlackFade();
            yield return ResetUiAfterBattle();
           
            if(_overworldActions.fishing)
            {
                _overworldActions.EndFishing();
            }
            else
            {
                _playerMovementHandler.AllowPlayerMovement(MovementRestrictor.Battle,0.15f);
            }

            IEnumerator HandleEvolutions()
            {
                foreach (var evolution in evolutionQueue)
                {
                    if (evolution.participantToEvolve.isActive)
                    {
                        var pokemon = evolution.participantToEvolve.pokemon;
                        yield return _pokemonOperations.HandlePokemonEvolution(pokemon,evolution.evolutionIndex);
                    }
                }
                evolutionQueue.Clear();
            }
        }
    }
    private IEnumerator ResetUiAfterBattle()
    {
        BattleInProgress = false;
        OnBattleEnd?.Invoke();
        _dialogueHandler.EndDialogue();
        _inputStateHandler.ResetRelevantUi(InputStateName.PlaceHolder);
        
        SetPlayerTurnUsage(PlayerTurnUsage.None);
        
        battleUI.SetActive(false);
        optionsUI.SetActive(false);
        
        for (var i=0;i<currentParticipants.Count;i++)
        {
            currentParticipants[i].pokemonImage.rectTransform.anchoredPosition = _defaultPokemonImagePositions[i];
            currentParticipants[i].pokemonImage.color = Color.white;
            
            if(currentParticipants[i].pokemon!=null)
            {
                currentParticipants[i].ResetParticipantState();
                currentParticipants[i].DeactivateUI();
                currentParticipants[i].pokemon = null;
                currentParticipants[i].pokemonTrainerAI = null;
            }
            currentParticipants[i].activeForBattle = false;
        }
        _battleIntroHandler.ResetParticipantIntroImages();
        OnBattleResult?.Invoke(battleEndState == BattleEndState.PlayerWon);
        _defaultPokemonImagePositions.Clear();
        overWorld.SetActive(true);
        if(battleEndState == BattleEndState.PlayerLost)
        {
            _playerParty.HealPartyPokemon();
            _areaHandler.TeleportToArea(AreaName.PokeCenter);
        }
        else
        {
            _areaHandler.SwitchToArea(_gameLoadingHandler.playerData.location);
        }
        
        BattleOver = false;
        battleEndState = BattleEndState.None;
        yield return new WaitForSeconds(1f);
    }
    private IEnumerator RunAway() 
    {
        var currentPlayerParticipant = GetCurrentParticipant();
        if(!currentPlayerParticipant.canEscape)
        {
            _dialogueHandler.DisplayBattleInfo(currentPlayerParticipant.pokemon.pokemonDisplayName + " is trapped");
        }
        else
        {
            if (isTrainerBattle)
            {
                _dialogueHandler.DisplayBattleInfo("Can't run away from trainer battle");
                yield return FailEscape();
            }
            else
            { 
                int random = Utility.RandomRange(1,11);
                var playerLevel =  GetParticipant(BattleParticipantKey.Player).pokemon.currentLevel;
                var enemyLevel =  GetParticipant(BattleParticipantKey.Player).currentEnemies[0].pokemon.currentLevel;
                if (playerLevel < enemyLevel)
                {
                    //lower chance if weaker
                    random--;
                }
                if (random > 5) //50/50 chance to run
                {
                    EndBattle(BattleEndState.PlayerRanAway,null);
                }
                else
                {
                    _dialogueHandler.DisplayBattleInfo("Can't run away");
                    yield return FailEscape();
                }
            }
        }
        IEnumerator FailEscape()
        {
            yield return _dialogueHandler.AwaitAllDialogue();
            _turnBasedCombatHandler.NextTurn();
        }
        
    }
}
