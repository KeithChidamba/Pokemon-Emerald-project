using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Battle_handler : MonoBehaviour
{
    public GameObject battleUI;
    public GameObject movesUI;
    public GameObject optionsUI;
    public GameObject[] battleOptions;
    public Battle_Participant[] battleParticipants = { null, null, null, null };
    public List<Battle_Participant> faintQueue = new();
    public Text movePowerPointsText;
    public Text moveTypeText;
    public Text[] availableMovesText;
    public bool isTrainerBattle = false;
    public bool isDoubleBattle = false;
    public int participantCount = 0;
    public bool battleOver = false;
    public bool battleWon = false;
    public GameObject overWorld;
    public bool runningAway;
    private bool battleTerminated;
    private int _currentMoveIndex = 0;
    public int currentEnemyIndex = 0;
    public float participantPositionOffset = 100;
    private List<Vector2> _defaultPokemonImagePositions = new ();
    public BattleType currentBattleType;
    public Pokemon lastOpponent;
    private Battle_Participant _currentParticipant;
    public List<EvolutionInBattleData> evolutionQueue;
    public GameObject optionSelector;
    public GameObject moveSelector;
    public bool usedTurnForItem;
    public bool usedTurnForSwap;
    public static Battle_handler Instance;
    public event Action OnBattleEnd;
    public event Action<bool> OnBattleResult;
    public event Action OnSwitchIn;
    public event Action<Battle_Participant> OnSwitchOut;
    private Action _checkParticipantsEachTurn;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (!Options_manager.Instance.playerInBattle) return;
        if (Turn_Based_Combat.Instance.currentTurnIndex > 1) return;
        
        _currentParticipant = GetCurrentParticipant();
        //if single battle, auto aim at enemy
        if (!isDoubleBattle && Turn_Based_Combat.Instance.currentTurnIndex == 0)
        {
            currentEnemyIndex = 2;
        }
    }

    public Battle_Participant GetCurrentParticipant()
    {
        return battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex];
    }
    private void EnableBattleMessage(InputState currentState)
    {
        if (currentState.stateName == InputStateName.PokemonBattleOptions)
        {
            Dialogue_handler.Instance.DisplaySpecific("What will you do?",DialogType.BattleDisplayMessage);
        }
    }
    private void AllowEnemySelection()
    {
        if (!isDoubleBattle)
        {
            PlayerExecuteMove();
            return;
        }
        if (_currentParticipant.pokemon.moveSet[_currentMoveIndex].isSelfTargeted 
            || _currentParticipant.pokemon.moveSet[_currentMoveIndex].isMultiTarget)
        {
            currentEnemyIndex = battleParticipants.ToList().FindIndex(a => a.isActive & !a.isPlayer);
            PlayerExecuteMove();
            return;
        }
        var enemySelectables = new List<SelectableUI>();
        for (var i = 2; i < battleParticipants.Length; i++)
        {
            enemySelectables.Add( new (battleParticipants[i].pokemonImage.gameObject,PlayerExecuteMove,true));
        }
        InputStateHandler.Instance.ChangeInputState(new (InputStateName.PokemonBattleEnemySelection
            ,new[] { InputStateGroup.PokemonBattle },
            stateDirection:InputDirection.Horizontal, selectableUis:enemySelectables, selecting:true));
    }
    private void PlayerExecuteMove()
    {//selecting enemy only happens in double battle
        
        battleParticipants[currentEnemyIndex].pokemonImage.color = Color.HSVToRGB(0,0,100);
        UseMove(_currentParticipant.pokemon.moveSet[_currentMoveIndex], _currentParticipant); 
    }
    private void ResetAi()
    {
        if(!isTrainerBattle)return;
        for(var i = 2; i < 4;i++)
            if (battleParticipants[i].pokemon != null & !battleParticipants[i].isPlayer)
            {
                battleParticipants[i].pokemonTrainerAI.CanAttack();
            }
    }

    public void SelectEnemy(int change)
    {
        battleParticipants[currentEnemyIndex].pokemonImage.color = Color.HSVToRGB(0,0,100);
        
        var partnerIndex = GetCurrentParticipant().GetPartnerIndex(); 
        var expectedAttackables = new [] {partnerIndex,2,3}; //can attack partner and enemies
         
        var validAttackables = expectedAttackables.ToList().Where(a => battleParticipants[a].isActive).ToList();
        
        var attackables = validAttackables.ToArray();
        
        var currentPos = Array.IndexOf(attackables,currentEnemyIndex);        
        var choiceIndex = Mathf.Clamp(currentPos+change,0,attackables.Length-1);//index of attackables
        
        currentEnemyIndex = attackables[choiceIndex];
        battleParticipants[currentEnemyIndex].pokemonImage.color = Color.HSVToRGB(17,96,54);
        
    }
    public void SetupOptionsInput(InputState currentState)
    {
        if (currentState.stateName != InputStateName.PokemonBattleOptions) return;
        InputStateHandler.Instance.OnStateRemoved -= SetupOptionsInput;
        SetupOptionsInput();
    }
    private void SetupOptionsInput()
    {
        var battleOptionSelectables = new List<SelectableUI>
        {
            new(battleOptions[0], LoadMoveInputAndText, true),
            new(battleOptions[1], Game_ui_manager.Instance.ViewBag, true),
            new(battleOptions[2], Game_ui_manager.Instance.ViewPokemonParty, true),
            new(battleOptions[3], () => StartCoroutine(RunAway()), true)
        };
        
        InputStateHandler.Instance.ChangeInputState(new (InputStateName.PokemonBattleOptions
            , new[] { InputStateGroup.PokemonBattle }, true,
            optionsUI, InputDirection.OmniDirection, battleOptionSelectables,
            optionSelector,true,true
            ,onExit:Turn_Based_Combat.Instance.RemoveTurn, updateExit:ConditionsForExit));
    }
    private bool ConditionsForExit()
    {
        return isDoubleBattle && Turn_Based_Combat.Instance.currentTurnIndex > 0
                              && !usedTurnForItem && !usedTurnForSwap
                              && !battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex - 1]
                                  .isSemiInvulnerable
                              //if partner is cooling down, cant remove turn
                              && !battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex - 1]
                                  .currentCoolDown.isCoolingDown;
    }
    private void AllowPlayerInput()
    {
        if (Turn_Based_Combat.Instance.currentTurnIndex > 1) return;
        var currentParticipant = GetCurrentParticipant();
        if (currentParticipant.isSemiInvulnerable) return;
        if (currentParticipant.currentCoolDown.isCoolingDown) return;
        InputStateHandler.Instance.ResetRelevantUi(new[]
        {
            InputStateName.PokemonBattleEnemySelection,
            InputStateName.PlaceHolder,InputStateName.DialoguePlaceHolder
        });
    }
    private IEnumerator SetValidParticipants()
    {
        foreach (var participant in battleParticipants)
        {
            if (participant.pokemon != null)
                if (participant.pokemon.hp > 0)
                {
                    yield return SetParticipant(participant, participant.pokemon, initialCall: true);
                }
        }
    }
    private IEnumerator SetupBattleSequence(Encounter_Area areaOfBattle)
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
        BattleIntro.Instance.SetPlatformSprite(areaOfBattle);
        //load visuals based on area
        overWorld.SetActive(false);
        battleUI.SetActive(true);
        Options_manager.Instance.playerInBattle = true;
        
        if(isTrainerBattle)
            yield return StartCoroutine(BattleIntro.Instance.PlayTrainerIntroSequence(areaOfBattle));
        else
            yield return StartCoroutine(BattleIntro.Instance.PlayWildIntroSequence(areaOfBattle));
        
        //Setup battle events
        _checkParticipantsEachTurn = ()=> CheckParticipantStates();
        Turn_Based_Combat.Instance.OnNewTurn += _checkParticipantsEachTurn;
        Game_Load.Instance.playerData.playerPosition = Player_movement.Instance.playerObject.transform.position;
        InputStateHandler.Instance.OnStateChanged += EnableBattleMessage;
        
        SetupOptionsInput();
        
        Turn_Based_Combat.Instance.ChangeTurn(-1, 0);
        
        Turn_Based_Combat.Instance.OnTurnsCompleted += ResetPlayersTurnUsage;
        Turn_Based_Combat.Instance.OnNewTurn += ResetAi;
        Turn_Based_Combat.Instance.OnNewTurn += AllowPlayerInput;
        
    }
    private void ResetPlayersTurnUsage()
    {
        usedTurnForItem = false;
        usedTurnForSwap = false;
    }
    public void SetBattleType(List<string> trainerNames)
    {
        overworld_actions.Instance.doingAction = true;
        Pokemon_party.Instance.SortByFainted();
        var copyOfTrainerData = Resources.Load<TrainerData>(
            Save_manager.GetDirectory(AssetDirectory.TrainerData)
            + $"{trainerNames[0]}/{trainerNames[0]}");
         currentBattleType = copyOfTrainerData.battleType;
        switch (copyOfTrainerData.battleType)
        {
            case BattleType.Single:
                StartCoroutine(StartSingleBattle(copyOfTrainerData));
                break;
            case BattleType.SingleDouble:
                StartCoroutine(StartSingleDoubleBattle(copyOfTrainerData));
                break;
        }
    }

    private IEnumerator DisplayTrainerMessage(string message)
    {
        Dialogue_handler.Instance.DisplayDetails(message,false);
        yield return new WaitUntil(()=>Dialogue_handler.Instance.dialogueFinished);
        Dialogue_handler.Instance.EndDialogue();
    }
    public IEnumerator StartWildBattle(Pokemon enemy) //only ever be for wild battles
    {
        Pokemon_party.Instance.SortByFainted();
        battleOver = false;
        isTrainerBattle = false;
        isDoubleBattle = false;
        var player = battleParticipants[0];
        var wildPokemon = battleParticipants[2];
        //set initial pokemon and enemy for player
        player.pokemon = Pokemon_party.Instance.party[0];
        player.currentEnemies.Add(wildPokemon);
        //setup wild pokemon enemy AI 
        wildPokemon.pokemon = enemy;
        wildPokemon.currentEnemies.Add(player);
        Wild_pkm.Instance.participant = wildPokemon;
        wildPokemon.AddToExpList(player.pokemon);
        Wild_pkm.Instance.inBattle = true;
        Wild_pkm.Instance.ranAway = false;
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(Encounter_handler.Instance.currentArea));
        Encounter_handler.Instance.currentArea = null;
    }
    private IEnumerator StartSingleBattle(TrainerData trainerData) //single trainer battle
    {
        yield return DisplayTrainerMessage(trainerData.battleIntroMessage);
        battleOver = false;
        isTrainerBattle = true;
        isDoubleBattle = false; 
        var player = battleParticipants[0];
        var enemy = battleParticipants[2];
        //set initial pokemon and enemy for player
        player.pokemon = Pokemon_party.Instance.party[0];
        player.currentEnemies.Add(enemy);
        //setup enemy AI
        enemy.currentEnemies.Add(player);
        enemy.pokemonTrainerAI = enemy.GetComponent<Enemy_trainer>();
        enemy.pokemonTrainerAI.SetupTrainerForBattle(trainerData);
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemy.AddToExpList(player.pokemon);
        enemy.pokemonTrainerAI.inBattle = true;
        //setup battle
        yield return SetValidParticipants();
        StartCoroutine(SetupBattleSequence(enemy.pokemonTrainerAI.trainerData.TrainerLocation));
    }

    private IEnumerator StartSingleDoubleBattle(TrainerData trainerData) //1v1 double battle
    {
        yield return DisplayTrainerMessage(trainerData.battleIntroMessage);
        battleOver = false;
        isTrainerBattle = true;
        isDoubleBattle = true; 
        var alivePartyPokemon = Pokemon_party.Instance.GetLivingPokemon();
        var player = battleParticipants[0];
        var playerPartner = battleParticipants[1];
        var enemy = battleParticipants[2];
        var enemyPartner = battleParticipants[3];
        //set initial pokemon for player
        player.pokemon = alivePartyPokemon[0];
        playerPartner.pokemon = alivePartyPokemon[1];
        //setup trainer ai for enemy participants
        for (int i = 2; i < 4; i++)
        {
            var currentEnemy = battleParticipants[i];
            currentEnemy.pokemonTrainerAI = currentEnemy.GetComponent<Enemy_trainer>();
            currentEnemy.pokemonTrainerAI.SetupTrainerForBattle(trainerData);
        }
        //copy over team data to enemy partner
        enemyPartner.pokemonTrainerAI.trainerData = enemy.pokemonTrainerAI.trainerData;
        enemyPartner.pokemonTrainerAI.trainerParty = enemy.pokemonTrainerAI.trainerParty;
        //set initial pokemon for enemies
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemyPartner.pokemon = enemyPartner.pokemonTrainerAI.trainerParty[1];
        //set enemies of all participants
        for (int i = 0; i < 2; i++) 
        {
            var currentEnemy = battleParticipants[i + 2];
            var currentPlayerParticipant = battleParticipants[i];

            player.currentEnemies.Add(currentEnemy);
            playerPartner.currentEnemies.Add(currentEnemy);
                
            enemy.currentEnemies.Add(currentPlayerParticipant);
            enemyPartner.currentEnemies.Add(currentPlayerParticipant);
        }
        //add player's pokemon to get exp from all current enemies
        foreach (var enemyParticipant in player.currentEnemies)//player and partner have same enemies
        {
            enemyParticipant.AddToExpList(player.pokemon);
            enemyParticipant.AddToExpList(playerPartner.pokemon);
        }
        //setup battle
        yield return SetValidParticipants();
        enemy.pokemonTrainerAI.inBattle = true;
        enemyPartner.pokemonTrainerAI.inBattle = true;
        StartCoroutine(SetupBattleSequence(enemy.pokemonTrainerAI.trainerData.TrainerLocation));
    }

    public IEnumerator SetParticipant(Battle_Participant participant,Pokemon newPokemon,bool initialCall=false)
    {
        OnSwitchOut?.Invoke(participant);
        participant.isEnemy = Array.IndexOf(battleParticipants, participant) > 1 ;
        if(!initialCall)
        {
            participant.pokemon = newPokemon;
            if (participant.isPlayer)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName
                                                            +" sent out "+participant.pokemon.pokemonName);
                
                //add enemies to exp list of new player pokemon
                foreach (var enemyParticipant in participant.currentEnemies)
                    enemyParticipant.AddToExpList(participant.pokemon);
            }
            else
            {
                Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemonTrainerAI.trainerData.TrainerName
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
                if (participant.pokemon!=null)
                {
                    if (participant.pokemon.hp>0)
                    {
                        validList.Add(participant);
                    }
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
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < _currentParticipant.pokemon.moveSet.Count; i++)
        {
            availableMovesText[i].text = _currentParticipant.pokemon.moveSet[i].moveName;
            moveSelectables.Add( new (availableMovesText[i].gameObject,AllowEnemySelection,true));
        }
        
        InputStateHandler.Instance.ChangeInputState(new (InputStateName.PokemonBattleMoveSelection
            ,new[] { InputStateGroup.PokemonBattle },true,
            movesUI, InputDirection.OmniDirection, moveSelectables,
            moveSelector,true,true,ResetMoveUsability,ResetMoveUsability));
        
        for (var i = _currentParticipant.pokemon.moveSet.Count; i < 4; i++)//only show available moves
            availableMovesText[i].text = "";
    }
    public void UseMove(Move move,Battle_Participant user)
    {
        if(move.powerpoints==0)return;
        move.powerpoints--;

        Turn currentTurn = new Turn(TurnUsage.Attack,move, Array.IndexOf(battleParticipants,user)
            ,currentEnemyIndex
            , user.pokemon.pokemonID
            ,battleParticipants[currentEnemyIndex].pokemon.pokemonID);
        Turn_Based_Combat.Instance.SaveTurn(currentTurn);
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
        var currentMove = _currentParticipant.pokemon.moveSet[_currentMoveIndex];
        movePowerPointsText.text = "PP: " + currentMove.powerpoints+ "/" + currentMove.maxPowerpoints;
        movePowerPointsText.color = (currentMove.powerpoints == 0)? Color.red : Color.black;
        moveTypeText.text = currentMove.type.typeName;
    }
    int MoneyModifier()
    {
        var playerParticipants = battleParticipants.ToList()
            .Where(p => p.isActive & p.isPlayer).ToList();
        foreach(var participant in playerParticipants)
            if (participant.pokemon.hasItem)
                if(participant.pokemon.heldItem.itemName == "Amulet Coin")
                    return 2;
        //in future can add another condition for abilities that increase/give money
        return 1;
    }
    public void StartFaintEvent()
    {
        StartCoroutine(FaintSequence());
    } 
    private IEnumerator FaintSequence()
    {
        while (faintQueue.Count > 0) 
        {
            Turn_Based_Combat.Instance.faintEventDelay = true;
            Dialogue_handler.Instance.DisplayBattleInfo(faintQueue[0].pokemon.pokemonName + " fainted!");
            var pkmImageRect = faintQueue[0].pokemonImage.rectTransform;
            var target = new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y-pkmImageRect.rect.height);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            
            StartCoroutine(BattleVisuals.Instance.SlideRect(pkmImageRect, pkmImageRect.anchoredPosition, target, 300f));
            
            var participantUIRect = faintQueue[0].participantUI.GetComponent<RectTransform>(); 
            var targetForUI = new Vector2(participantUIRect.anchoredPosition.x, participantUIRect.anchoredPosition.y-400f);
            yield return StartCoroutine(BattleVisuals.Instance.SlideRect(participantUIRect,participantUIRect.anchoredPosition, targetForUI, 900f));
            
            if (faintQueue[0].isEnemy)
            {
                yield return new WaitForSeconds(0.05f);
                faintQueue[0].participantUI.SetActive(false);
                yield return new WaitForSeconds(0.25f);
                faintQueue[0].pokemonImage.color = new Color(0, 0, 0, 0);
            }
            
            StartCoroutine(faintQueue[0].HandleFaintLogic());
            yield return new WaitUntil(() => !Turn_Based_Combat.Instance.faintEventDelay);
            if(!battleOver && faintQueue[0].isActive)
            {
                faintQueue[0].participantUI.SetActive(true);
                pkmImageRect.anchoredPosition =
                    new Vector2(pkmImageRect.anchoredPosition.x, pkmImageRect.anchoredPosition.y + pkmImageRect.rect.height);
                if (faintQueue[0].isEnemy) faintQueue[0].pokemonImage.color = Color.white;
            }
            participantUIRect.anchoredPosition = new Vector2(participantUIRect.anchoredPosition.x,
                participantUIRect.anchoredPosition.y + 400f);
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
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
    }
    private IEnumerator ProcessBattleEnd()
    {
        var playerWhiteOut = false;
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        if (battleTerminated)
        {
            Dialogue_handler.Instance.DisplayBattleInfo("the battle ended");
            yield return new WaitForSeconds(1f);
            battleTerminated = false;
        }
        else if (runningAway)
        {
            runningAway = false;
            Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " ran away");
        }
        else
        {
            
            if (battleWon)
            {
                foreach (var evolution in evolutionQueue)
                {
                    if (evolution.participantToEvolve.isActive)
                    {
                        var pokemon = evolution.participantToEvolve.pokemon;
                        Dialogue_handler.Instance.DisplayBattleInfo("What? "+pokemon.pokemonName+" is evolving!");
                        var previousName = pokemon.pokemonName;
                        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                        pokemon.Evolve(pokemon.evolutions[evolution.evolutionIndex]);
                        Dialogue_handler.Instance.DisplayBattleInfo(previousName+" evolved into "+pokemon.pokemonName);
                        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                    }
                }
                evolutionQueue.Clear();
                
                if (isTrainerBattle)
                {
                    var anyEnemy = battleParticipants[0].currentEnemies[0];
                    var baseMoneyPayout = anyEnemy.pokemonTrainerAI.trainerData.BaseMoneyPayout;
                    
                    Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " defeated "
                        +anyEnemy.pokemonTrainerAI.trainerData.TrainerName);
                    
                    yield return  BattleIntro.Instance.ShowEnemiesAfterBattle();
                    yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                    Dialogue_handler.Instance.DisplayBattleInfo(anyEnemy.pokemonTrainerAI.trainerData.battleLossMessage);
                    
                    var moneyGained = baseMoneyPayout * lastOpponent.currentLevel * MoneyModifier();
                    Game_Load.Instance.playerData.playerMoney += moneyGained;
                    Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " recieved P" + moneyGained);
                }
            }
            else
            {
                if (isTrainerBattle)
                {//last participant is always not null in this situation
                    var anyEnemy = battleParticipants[0].currentEnemies[0];
                    var baseMoneyPayout = anyEnemy.pokemonTrainerAI.trainerData.BaseMoneyPayout;
                    var playerLastParticipant = battleParticipants.ToList()
                        .Where(p => p.isActive & p.isPlayer).ToList()[0];
                    lastOpponent = playerLastParticipant.currentEnemies
                        .Where(p=>p.isActive).ToList()[0].pokemon;
                    Game_Load.Instance.playerData.playerMoney -= baseMoneyPayout 
                                                                   * Game_Load.Instance.playerData.numBadges 
                                                                   * lastOpponent.currentLevel;
                }
                if (!Wild_pkm.Instance.ranAway)
                {
                    var partyPokemon = Pokemon_party.Instance.party.ToList();
                    partyPokemon.RemoveAll(p => p == null);
                    Game_Load.Instance.playerData.playerMoney -= 100 * partyPokemon.OrderByDescending(p=>p.currentLevel)
                        .First().currentLevel;//highest leveled pokemon in party
                    Dialogue_handler.Instance.DisplayBattleInfo("All your pokemon have fainted");
                    playerWhiteOut = true;
                }
            }
        }
        yield return new WaitUntil(() => Dialogue_handler.Instance.dialogueFinished);
        Dialogue_handler.Instance.EndDialogue();
        yield return BattleIntro.Instance.BlackFade();
        yield return ResetUiAfterBattle(playerWhiteOut);
        //battle triggered from fishing
        if(overworld_actions.Instance.fishing) overworld_actions.Instance.ResetFishingAction();
    }

    public void EndBattle(bool hasWon,bool battleCancelled=false)
    {
        if (battleOver) return;
        battleWon = hasWon;
        battleOver = true;
        battleTerminated = battleCancelled;
        StartCoroutine(ProcessBattleEnd());
    }
    private IEnumerator ResetUiAfterBattle(bool playerWhiteOut)
    {
        OnBattleEnd?.Invoke();
        Turn_Based_Combat.Instance.OnNewTurn -= _checkParticipantsEachTurn;;
        Turn_Based_Combat.Instance.OnNewTurn -= ResetAi;
        Turn_Based_Combat.Instance.OnNewTurn -= AllowPlayerInput;
        Turn_Based_Combat.Instance.OnTurnsCompleted -= ResetPlayersTurnUsage;
        InputStateHandler.Instance.OnStateChanged -= EnableBattleMessage;
        Dialogue_handler.Instance.EndDialogue();
        usedTurnForItem = false;
        usedTurnForSwap = false;
        Options_manager.Instance.playerInBattle = false;
        overworld_actions.Instance.doingAction = false;
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
                if (battleParticipants[i].pokemonTrainerAI != null)
                    battleParticipants[i].pokemonTrainerAI.inBattle = false;
                battleParticipants[i].pokemon = null;
            }
        }
        OnBattleResult?.Invoke(battleWon);
        _defaultPokemonImagePositions.Clear();
        Encounter_handler.Instance.ResetTrigger();
        overWorld.SetActive(true);
        var location = (playerWhiteOut)? AreaName.PokeCenter : Game_Load.Instance.playerData.location;
        if(playerWhiteOut) Options_manager.Instance.HealPartyPokemon();
        Area_manager.Instance.SwitchToArea(location);
        Dialogue_handler.Instance.canExitDialogue = true;
        InputStateHandler.Instance.ResetGroupUi(InputStateGroup.PokemonBattle);
        battleWon = false;
        battleOver = false;
        yield return new WaitForSeconds(1f);
    }
    private IEnumerator RunAway() {
        runningAway = true;
        if(!isTrainerBattle & !_currentParticipant.canEscape)
            Dialogue_handler.Instance.DisplayBattleInfo(_currentParticipant.pokemon.pokemonName +" is trapped");
        else
        {
            if (isTrainerBattle)
            {
                Dialogue_handler.Instance.DisplayBattleInfo("Can't run away from trainer battle");
                yield return new WaitForSeconds(1.5f);
                runningAway = false;
                Turn_Based_Combat.Instance.NextTurn();
            }
            else
            { 
                int random = Utility.RandomRange(1,11);
                if (battleParticipants[0].pokemon.currentLevel < 
                    battleParticipants[0].currentEnemies[0].pokemon.currentLevel)//lower chance if weaker
                    random--;
                if (random > 5) //initially 50/50 chance to run
                {
                    Wild_pkm.Instance.inBattle = false;
                    EndBattle(false);
                }
                else
                {
                    Dialogue_handler.Instance.DisplayBattleInfo("Can't run away");
                    yield return new WaitForSeconds(1.5f);
                    runningAway = false;
                    Turn_Based_Combat.Instance.NextTurn();
                }
            }
        } 
    }
}
