using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

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
    public Battle_Participant[] battleParticipants;
    public List<Battle_Participant> faintQueue = new();
    public Text movePowerPointsText;
    public Text moveTypeText;
    public Text[] availableMovesText;
    public bool battleInProgress;
    public bool isTrainerBattle;
    public bool isDoubleBattle;
    public int participantCount;
    public bool battleOver;
    public BattleEndState battleEndState;
    public GameObject overWorld;
    private int _currentMoveIndex;
    private int _currentPlayerEnemyIndex;
    public float participantPositionOffset = 100;
    private List<Vector2> _defaultPokemonImagePositions = new();
    public BattleType currentBattleType;
    public enum BattlesStyle {Switch,Set };
    public BattlesStyle currentBattleStyle;
    public Pokemon lastOpponent;
    private Battle_Participant _currentPlayerParticipant;
    public List<EvolutionInBattleData> evolutionQueue;
    public GameObject optionSelector;
    public GameObject moveSelector;
    private PlayerTurnUsage _previousTurnUsage;
    
    public event Action OnBattleEnd;
    public event Action<bool> OnBattleResult;
    public event Action OnSwitchIn;
    public event Action<Battle_Participant> OnSwitchOut;
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

    private WildPokemonAiHandler _wildPokemonHandler;
    private Area_manager  _areaHandler;
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
        gameObject.SetActive(true);
    }
    public void OnInject()
    {
        _turnBasedCombatHandler.OnNewTurn += ResetAi;
        _turnBasedCombatHandler.OnNewTurn += AllowPlayerInput;
        _checkParticipantsEachTurn = ()=> CheckParticipantStates();
        _turnBasedCombatHandler.OnNewTurn += _checkParticipantsEachTurn;
        _turnBasedCombatHandler.OnTurnsCompleted += ResetPlayersTurnUsage;
    }
    void Update()
    {
        if (!battleInProgress) return;
        if (_turnBasedCombatHandler.currentTurnIndex > 1) return;
        
        _currentPlayerParticipant = GetCurrentParticipant();
        //if single battle, auto aim at enemy
        if (!isDoubleBattle && _turnBasedCombatHandler.currentTurnIndex == 0)
        {
            _currentPlayerEnemyIndex = 2;
        }
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
    public Battle_Participant GetCurrentParticipant()
    {
        return battleParticipants[_turnBasedCombatHandler.currentTurnIndex];
    }
    public void EnableBattleMessage(InputState currentState)
    {
        if (currentState.stateName == InputStateName.PokemonBattleOptions)
        {
            _dialogueHandler.DisplaySpecific("What will you do?",DialogType.BattleDisplayMessage);
        }
    }
    private void AllowEnemySelection()
    {
        if (!isDoubleBattle)
        {
            PlayerExecuteMove();
            return;
        }
        if (_currentPlayerParticipant.pokemon.moveSet[_currentMoveIndex].isSelfTargeted 
            || _currentPlayerParticipant.pokemon.moveSet[_currentMoveIndex].isMultiTarget)
        {
            _currentPlayerEnemyIndex = battleParticipants.ToList().FindIndex(a => a.isActive & !a.isPlayer);
            PlayerExecuteMove();
            return;
        }
        var enemySelectables = new List<SelectableUI>();
        for (var i = 2; i < battleParticipants.Length; i++)
        {
            enemySelectables.Add( new (battleParticipants[i].pokemonImage.gameObject,PlayerExecuteMove,true));
        }
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonBattleEnemySelection
            ,InputStateGroup.PokemonBattle,
            stateDirection:InputDirection.Horizontal, selectableUis:enemySelectables, selecting:true));
    }
    private void PlayerExecuteMove()
    {//selecting enemy only happens in double battle
        
        battleParticipants[_currentPlayerEnemyIndex].pokemonImage.color = Color.HSVToRGB(0,0,100);
        UseMove(_currentPlayerParticipant.pokemon.moveSet[_currentMoveIndex], _currentPlayerParticipant,_currentPlayerEnemyIndex); 
    }

    public void ResetEnemyColor()
    {
       battleParticipants[_currentPlayerEnemyIndex]
            .pokemonImage.color = Color.HSVToRGB(0,0,100);//reset color if cancelled
    }
    public void SelectEnemy(int change)
    {
        battleParticipants[_currentPlayerEnemyIndex].pokemonImage.color = Color.HSVToRGB(0,0,100);
        
        var partnerIndex = GetCurrentParticipant().GetPartnerIndex(); 
        var expectedAttackables = new [] {partnerIndex,2,3}; //can attack partner and enemies
         
        var validAttackables = expectedAttackables.ToList().Where(a => battleParticipants[a].isActive).ToList();
        
        var attackables = validAttackables.ToArray();
        
        var currentPos = Array.IndexOf(attackables,_currentPlayerEnemyIndex);        
        var choiceIndex = Mathf.Clamp(currentPos+change,0,attackables.Length-1);//index of attackables
        
        _currentPlayerEnemyIndex = attackables[choiceIndex];
        battleParticipants[_currentPlayerEnemyIndex].pokemonImage.color = Color.HSVToRGB(17,96,54);
        
    }
    public void SetupOptionsInput(InputState currentState)
    {
        if (currentState.stateName != InputStateName.PokemonBattleOptions) return;
        _inputStateHandler.OnStateRemoved -= SetupOptionsInput;
        SetupOptionsInput();
    }
    private void SetupOptionsInput()
    {
        var battleOptionSelectables = new List<SelectableUI>
        {
            new(battleOptions[0], LoadMoveInputAndText, true),
            new(battleOptions[1], _gameUIHandler.ValidateBagView, true),
            new(battleOptions[2], _gameUIHandler.ViewPokemonParty, true),
            new(battleOptions[3], () => StartCoroutine(RunAway()), true)
        };
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonBattleOptions
            , InputStateGroup.PokemonBattle, true,
            optionsUI, InputDirection.Grid, battleOptionSelectables,
            optionSelector,true,true
            ,onExit:_turnBasedCombatHandler.RemoveTurn, updateExit:ConditionsForExit));
    }
    private bool ConditionsForExit()
    {
        //Check if player can reset their move selection
        return isDoubleBattle && _turnBasedCombatHandler.currentTurnIndex > 0
                              //if irreversible turn usage occured, cant remove turn
                              && _previousTurnUsage == PlayerTurnUsage.Fight
                              //if partner is semi-invulnerable, cant remove turn
                              && !battleParticipants[_turnBasedCombatHandler.currentTurnIndex - 1]
                                  .isSemiInvulnerable
                              //if partner is cooling down, cant remove turn
                              && !battleParticipants[_turnBasedCombatHandler.currentTurnIndex - 1]
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
        if (_turnBasedCombatHandler.currentTurnIndex > 1) return;
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
        foreach (var participant in battleParticipants)
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
        foreach (var participant in battleParticipants)
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
        battleInProgress = true;
        
        if(isTrainerBattle)
            yield return StartCoroutine(_battleIntroHandler.PlayTrainerIntroSequence());
        else
            yield return StartCoroutine(_battleIntroHandler.PlayWildIntroSequence());
        
        _gameLoadingHandler.playerData.playerPosition = _playerMovementHandler.GetPlayerPosition();
        _inputStateHandler.OnStateChanged += EnableBattleMessage;
        
        SetupOptionsInput();
    }
    private void ResetAi()
    {
        if(!isTrainerBattle)return;
        var currentParticipant = GetCurrentParticipant();
        if(!currentParticipant.isActive) return;
        if(currentParticipant.isPlayer)return;
        currentParticipant.pokemonTrainerAI.MakeBattleDecision();
    }
    
    public void SetBattleType(TrainerBattleInteractionInfo trainerInteraction)
    {
        _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.Battle);
        _pokemonPartyHandler.SortByFainted();
        
         currentBattleType = trainerInteraction.data.battleType;
        switch (trainerInteraction.data.battleType)
        {
            case BattleType.Single:
                StartCoroutine(StartSingleBattle(trainerInteraction.data));
                break;
            case BattleType.SingleDouble:
                StartCoroutine(StartSingleDoubleBattle(trainerInteraction.data));
                break;
        }
    }

    private IEnumerator DisplayTrainerMessage(string message)
    {
        _dialogueHandler.DisplayDetails(message,false);
        yield return new WaitUntil(()=>_dialogueHandler.dialogueFinished);
        _dialogueHandler.EndDialogue();
    }
    public IEnumerator StartWildBattle(Pokemon enemy,Biome biome)
    {
        _pokemonPartyHandler.SortByFainted();
        battleOver = false;
        isTrainerBattle = false;
        isDoubleBattle = false;
        var player = battleParticipants[0];
        var wildPokemon = battleParticipants[2];
        player.activeForBattle = true;
        wildPokemon.activeForBattle = true;
        
        //set initial pokemon and enemy for player
        player.pokemon = _pokemonPartyHandler.party[0];
        player.currentEnemies.Add(wildPokemon);      
        //setup wild pokemon enemy AI 
        wildPokemon.pokemon = enemy;
        wildPokemon.currentEnemies.Add(player);
        _wildPokemonHandler.participant = wildPokemon;
        wildPokemon.AddToExpList(player.pokemon);
        _wildPokemonHandler.SetBattleState();
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(biome));
    }
    private IEnumerator StartSingleBattle(TrainerData trainerData) //single trainer battle
    {
        yield return DisplayTrainerMessage(trainerData.battleIntroMessage);
        battleOver = false;
        isTrainerBattle = true;
        isDoubleBattle = false; 
        var player = battleParticipants[0];
        var enemy = battleParticipants[2];
        player.activeForBattle = true;
        enemy.activeForBattle = true;
        
        //set initial pokemon and enemy for player
        player.pokemon = _pokemonPartyHandler.party[0];
        player.currentEnemies.Add(enemy);
        //setup enemy AI
        enemy.currentEnemies.Add(player);
        enemy.SetupEnemyAi(trainerData);
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemy.AddToExpList(player.pokemon);
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(enemy.pokemonTrainerAI.trainerData.TrainerLocationData.biome));
    }

    private IEnumerator StartSingleDoubleBattle(TrainerData trainerData) //1v1 double battle
    {
        yield return DisplayTrainerMessage(trainerData.battleIntroMessage);
        battleOver = false;
        isTrainerBattle = true;
        isDoubleBattle = true; 
        var alivePartyPokemon = _pokemonPartyHandler.GetLivingPokemon();
        var playerPartnerAvailable = alivePartyPokemon.Count > 1;
        
        var player = battleParticipants[0];
        player.activeForBattle = true;
        var playerPartner = battleParticipants[1];
        playerPartner.activeForBattle = playerPartnerAvailable;
        var enemy = battleParticipants[2];
        enemy.activeForBattle = true;
        var enemyPartner = battleParticipants[3];
        enemyPartner.activeForBattle = true;
        
        //set initial pokemon for player
        player.pokemon = alivePartyPokemon[0];
        if(playerPartner.activeForBattle) playerPartner.pokemon = alivePartyPokemon[1];
        
        //setup trainer ai for enemy participants
        enemy.SetupEnemyAi(trainerData,enemyPartner);
        
        //set initial pokemon for enemies
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemyPartner.pokemon = enemyPartner.pokemonTrainerAI.trainerParty[1];
        //set enemies of all participants
        for (int i = 0; i < 2; i++) 
        {
            var currentEnemy = battleParticipants[i + 2];
            var currentPlayerParticipant = battleParticipants[i];

            player.currentEnemies.Add(currentEnemy);
            if(playerPartner.activeForBattle) playerPartner.currentEnemies.Add(currentEnemy);
            
            if(!currentPlayerParticipant.activeForBattle) continue;
            
            enemy.currentEnemies.Add(currentPlayerParticipant);
            enemyPartner.currentEnemies.Add(currentPlayerParticipant);
        }
        //add player's pokemon to get exp from all current enemies
        foreach (var enemyParticipant in player.currentEnemies)//player and partner have same enemies
        {
            enemyParticipant.AddToExpList(player.pokemon);
            if(playerPartner.activeForBattle) enemyParticipant.AddToExpList(playerPartner.pokemon);
        }
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(enemy.pokemonTrainerAI.trainerData.TrainerLocationData.biome));
    }

    public IEnumerator SetupParticipant(Battle_Participant participant,Pokemon newPokemon,bool initialCall=false)
    {
        OnSwitchOut?.Invoke(participant);
        participant.isEnemy = Array.IndexOf(battleParticipants, participant) > 1 ;
        if(!initialCall)
        {
            participant.pokemon = newPokemon;
            if (participant.isPlayer)
            {
                participant.pokemon.currentPokemonName = participant.pokemon.pokemonName;
                participant.pokemon.pokemonName = participant.pokemon.nickName;
                
                _dialogueHandler.DisplayBattleInfo(_gameLoadingHandler.playerData.playerName
                                                            +" sent out "+participant.pokemon.pokemonName);
                
                //add enemies to exp list of new player pokemon
                foreach (var enemyParticipant in participant.currentEnemies)
                    enemyParticipant.AddToExpList(participant.pokemon);
            }
            else
            {
                _dialogueHandler.DisplayBattleInfo(participant.pokemonTrainerAI.trainerData.TrainerName
                                                            +" sent out "+participant.pokemon.pokemonName);
                
                //add player participants to get exp from switched in enemy
                foreach (var playerParticipant in participant.currentEnemies)
                    participant.AddToExpList(playerParticipant.pokemon);
            }
        }

        if (participant.isEnemy)
        {
            participant.pokemon.pokemonName = "Foe " + participant.pokemon.pokemonName;
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
        foreach (var participant in battleParticipants)
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
        participantCount = 0;
        foreach (var participant in battleParticipants)
        {
            if (participant.pokemon == null) continue;
            participantCount++;
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
        bool emptyMoves = true;
        foreach (var move in _currentPlayerParticipant.pokemon.moveSet)
        {
            if(move.powerpoints>0)
            {
                emptyMoves = false;
                break;
            }
        }
        if (emptyMoves)
        {
            _turnBasedCombatHandler.SaveStruggleTurn(_currentPlayerParticipant);
            return;
        }
        
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < _currentPlayerParticipant.pokemon.moveSet.Count; i++)
        {
            availableMovesText[i].text = _currentPlayerParticipant.pokemon.moveSet[i].moveName;
            moveSelectables.Add( new (availableMovesText[i].gameObject,AllowEnemySelection,true));
        }
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonBattleMoveSelection
            ,InputStateGroup.PokemonBattle,true,
            movesUI, InputDirection.Grid, moveSelectables,
            moveSelector,true,true,ResetMoveUsability,ResetMoveUsability));
        
        for (var i = _currentPlayerParticipant.pokemon.moveSet.Count; i < 4; i++)//only show available moves
            availableMovesText[i].text = "";
    }
    public void UseMove(Move move,Battle_Participant user, int enemyIndex)
    {
        if(move.powerpoints==0)return;
        move.powerpoints--;

        Turn currentTurn = new Turn(TurnUsage.Attack,move, Array.IndexOf(battleParticipants,user)
            ,enemyIndex
            , user.pokemon.pokemonID
            ,battleParticipants[enemyIndex].pokemon.pokemonID);
        _turnBasedCombatHandler.SaveTurn(currentTurn);
    }
    private void ResetMoveUsability()
    {
        _currentMoveIndex = 0;
        movesUI.SetActive(false);
    }
    public void SelectMove(int moveIndex)
    {
        _currentMoveIndex = 0;
        _currentMoveIndex = moveIndex;
        var currentMove = _currentPlayerParticipant.pokemon.moveSet[_currentMoveIndex];
        movePowerPointsText.text = "PP: " + currentMove.powerpoints+ "/" + currentMove.maxPowerpoints;
        movePowerPointsText.color = (currentMove.powerpoints == 0)? Color.red : Color.black;
        moveTypeText.text = currentMove.type.GetTypeName;
    }
    private float PrizeMoneyModifier()
    {
        var playerParticipants = battleParticipants.ToList()
            .Where(p => p.isActive & p.isPlayer).ToList();
        foreach(var participant in playerParticipants)
        {
            if (!participant.pokemon.hasItem)continue;
            if (participant.pokemon.heldItem.itemType == ItemType.GainMoney)
            {
                return participant.pokemon.heldItem.itemEffectData;
            }
        }
        return 1f;
    }
    public void StartFaintEvent()
    {
        StartCoroutine(FaintSequence());
    } 
    private IEnumerator FaintSequence()
    {
        while (faintQueue.Count > 0)
        {
            var faintedParticipant = faintQueue[0];
            _turnBasedCombatHandler.faintEventDelay = true;
            _dialogueHandler.DisplayBattleInfo(faintedParticipant.pokemon.pokemonName + " fainted!");
            var pkmImageRect = faintedParticipant.pokemonImage.rectTransform;
            var rectHeight = pkmImageRect.rect.height;
            var target = new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y-rectHeight);
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
            
            StartCoroutine(BattleVisuals.SlideRect(pkmImageRect, pkmImageRect.anchoredPosition, target, 300f));
            
            var participantUIRect = faintedParticipant.participantUI.GetComponent<RectTransform>(); 
            var targetForUI = new Vector2(participantUIRect.anchoredPosition.x, participantUIRect.anchoredPosition.y-400f);
            yield return StartCoroutine(BattleVisuals.SlideRect(participantUIRect,participantUIRect.anchoredPosition, targetForUI, 900f));
            
            if (faintedParticipant.isEnemy)
            {
                yield return new WaitForSeconds(0.05f);
                faintedParticipant.participantUI.SetActive(false);
                yield return new WaitForSeconds(0.25f);
                faintedParticipant.pokemonImage.color = new Color(0, 0, 0, 0);
            }
            
            yield return faintedParticipant.HandleFaintLogic();
            
            if (!battleOver)
            {
                participantUIRect.anchoredPosition = new Vector2(participantUIRect.anchoredPosition.x,
                    participantUIRect.anchoredPosition.y + 400f);
                
                if(faintedParticipant.isActive)
                {

                    faintedParticipant.participantUI.SetActive(true);
                    pkmImageRect.anchoredPosition =
                        new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y + rectHeight);
              
                    if (faintedParticipant.isEnemy)
                    {
                        faintedParticipant.pokemonImage.color = Color.white;
                        yield return _battleIntroHandler.PokemonIntroAnimation(faintedParticipant);
                    }
                }
            }
            
            faintQueue.RemoveAt(0);
            yield return null;
        }
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
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
    }
    private IEnumerator ProcessBattleEnd()
    {
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        _inputStateHandler.OnStateChanged -= EnableBattleMessage;
        _inputStateHandler.AddPlaceHolderState();
        var playerData = _gameLoadingHandler.playerData;
        var playerName = playerData.playerName;
        string wildPokemonName = "";
        if (!isTrainerBattle)
        {
            wildPokemonName = _wildPokemonHandler.participant.pokemon.pokemonName;
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
                    var anyEnemy = battleParticipants[0].currentEnemies[0].pokemonTrainerAI.trainerData;
                    var baseMoneyPayout = anyEnemy.BaseMoneyPayout;
                    
                    _dialogueHandler.DisplayBattleInfo(playerName + " defeated " + anyEnemy.TrainerName);
                    
                    yield return _battleIntroHandler.ShowEnemiesAfterBattle();
                    _dialogueHandler.DisplayBattleInfo(anyEnemy.battleLossMessage);
                    var moneyGained = baseMoneyPayout * lastOpponent.currentLevel * PrizeMoneyModifier();
                    playerData.playerMoney += (int)math.floor(moneyGained);
                    _dialogueHandler.DisplayBattleInfo(playerName + " recieved P" + moneyGained);
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
                    var anyEnemy = battleParticipants[0].currentEnemies[0];
                    var baseMoneyPayout = anyEnemy.pokemonTrainerAI.trainerData.BaseMoneyPayout;
                    
                    var playersLastParticipant = battleParticipants.First(p => p.isActive & p.isPlayer);
                    
                    lastOpponent = playersLastParticipant.currentEnemies
                        .First(p=>p.isActive).pokemon;
                    playerData.playerMoney -= baseMoneyPayout * playerData.numBadges * lastOpponent.currentLevel;
                }
                else
                {
                    var partyPokemon = _pokemonPartyHandler.party
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
        
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
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
            _playerMovementHandler.AllowPlayerMovement(MovementRestrictor.Battle);
        }

        IEnumerator HandleEvolutions()
        {
            foreach (var evolution in evolutionQueue)
            {
                if (evolution.participantToEvolve.isActive)
                {
                    var pokemon = evolution.participantToEvolve.pokemon;
                    _dialogueHandler.DisplayBattleInfo("What? "+pokemon.pokemonName+" is evolving!");
                    var previousName = pokemon.pokemonName;
                    yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
                    pokemon.Evolve(pokemon.evolutions[evolution.evolutionIndex]);
                    _dialogueHandler.DisplayBattleInfo(previousName+" evolved into "+pokemon.pokemonName);
                    yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
                }
            }
            evolutionQueue.Clear();
        }
    }

    public void EndBattle(BattleEndState state)
    {
        if (battleOver) return;
        battleEndState = state;
        battleOver = true;
        StartCoroutine(ProcessBattleEnd());
    }
    private IEnumerator ResetUiAfterBattle()
    {
        battleInProgress = false;
        OnBattleEnd?.Invoke();
        _dialogueHandler.EndDialogue();
        _inputStateHandler.ResetRelevantUi(InputStateName.PlaceHolder,true);
        
        SetPlayerTurnUsage(PlayerTurnUsage.None);
        
        battleUI.SetActive(false);
        optionsUI.SetActive(false);
        lastOpponent = null;
        participantCount = 0;
        for (var i=0;i<battleParticipants.Length;i++)
        {
            battleParticipants[i].pokemonImage.rectTransform.anchoredPosition = _defaultPokemonImagePositions[i];
            battleParticipants[i].pokemonImage.color = Color.white;
            
            if(battleParticipants[i].pokemon!=null)
            {
                battleParticipants[i].ResetParticipantState();
                battleParticipants[i].DeactivateUI();
                battleParticipants[i].pokemon = null;
                battleParticipants[i].pokemonTrainerAI = null;
            }
            battleParticipants[i].activeForBattle = false;
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
       
        _dialogueHandler.canExitDialogue = true;
        battleOver = false;
        battleEndState = BattleEndState.None;
        yield return new WaitForSeconds(1f);
    }
    private IEnumerator RunAway() 
    {
        if(!_currentPlayerParticipant.canEscape)
        {
            _dialogueHandler.DisplayBattleInfo(_currentPlayerParticipant.pokemon.pokemonName + " is trapped");
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
                var playerLevel = battleParticipants[0].pokemon.currentLevel;
                var enemyLevel = battleParticipants[0].currentEnemies[0].pokemon.currentLevel;
                if (playerLevel < enemyLevel)
                {
                    //lower chance if weaker
                    random--;
                }
                if (random > 5) //50/50 chance to run
                {
                    EndBattle(BattleEndState.PlayerRanAway);
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
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
            _turnBasedCombatHandler.NextTurn();
        }
        
    }
}
