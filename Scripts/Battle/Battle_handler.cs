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
    public List<Pokemon> levelUpQueue = new();
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
    public List<GameObject> backgrounds;
    public bool runningAway; 
    private int _currentMoveIndex = 0;
    public int currentEnemyIndex = 0;

    public Pokemon lastOpponent;
    private Battle_Participant _currentParticipant;
    public GameObject optionSelector;
    public GameObject moveSelector;
    public bool usedTurnForItem;
    public bool usedTurnForSwap;
    public static Battle_handler Instance;
    public event Action OnBattleEnd;
    public event Action OnSwitchIn;
    public event Action<Battle_Participant> OnSwitchOut;
    
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
        AutoAim();
    }

    public Battle_Participant GetCurrentParticipant()
    {
        return battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex];
    }
    private void EnableBattleMessage(InputState currentState)
    {
        if (currentState.stateName == InputStateHandler.StateName.PokemonBattleOptions)
        {
            Dialogue_handler.Instance.DisplaySpecific("What will you do?",Dialogue_handler.DialogType.BattleDisplayMessage);
            optionsUI.SetActive(true);
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
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonBattleEnemySelection
            ,new[] { InputStateHandler.StateGroup.PokemonBattle },
            stateDirectional:InputStateHandler.Directional.Horizontal, selectableUis:enemySelectables, selecting:true));
    }
    private void PlayerExecuteMove()
    {//selecting enemy only happens in double battle
        
        battleParticipants[currentEnemyIndex].pokemonImage.color = Color.HSVToRGB(0,0,100);
        UseMove(_currentParticipant.pokemon.moveSet[_currentMoveIndex], _currentParticipant); 
    }
    private void ResetAi()
    {
        if (!isTrainerBattle)
            Wild_pkm.Instance.CanAttack();
        else
            for(var i = 2; i < 4;i++)
                if (battleParticipants[i].pokemon != null & !battleParticipants[i].isPlayer)
                {
                    battleParticipants[i].pokemonTrainerAI.CanAttack();
                }
    }

    void AutoAim()
    {
        if (!isDoubleBattle && Turn_Based_Combat.Instance.currentTurnIndex == 0) //if single battle, auto aim at enemy
        {
            currentEnemyIndex = 2;
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
    public void SetupOptionsInput()
    {
        InputStateHandler.Instance.OnStateRemovalComplete -= SetupOptionsInput;
        var battleOptionSelectables = new List<SelectableUI>
        {
            new(battleOptions[0], LoadMoveInputAndText, true),
            new(battleOptions[1], Game_ui_manager.Instance.ViewBag, true),
            new(battleOptions[2], Game_ui_manager.Instance.ViewPokemonParty, true),
            new(battleOptions[3], () => StartCoroutine(RunAway()), true)
        };
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonBattleOptions
            , new[] { InputStateHandler.StateGroup.PokemonBattle }, true,
            optionsUI, InputStateHandler.Directional.OmniDirection, battleOptionSelectables,
            optionSelector,true,true,canExit: false
            ,onExit:Turn_Based_Combat.Instance.RemoveTurn, updateExit:ConditionForExit));
    }

    bool ConditionForExit()
    {
        return isDoubleBattle && Turn_Based_Combat.Instance.currentTurnIndex > 0 && !usedTurnForItem && !usedTurnForSwap;
    }
    private void SetupBattle()
    {
        Turn_Based_Combat.Instance.OnNewTurn += CheckParticipantStates;
        Game_Load.Instance.playerData.playerPosition = Player_movement.Instance.playerObject.transform.position;
        InputStateHandler.Instance.OnStateChanged += EnableBattleMessage;
        SetupOptionsInput();
        levelUpQueue.Clear();
        Options_manager.Instance.playerInBattle = true;
        overworld_actions.Instance.doingAction = true;
        battleUI.SetActive(true);
        overWorld.SetActive(false);
        Turn_Based_Combat.Instance.ChangeTurn(-1, 0);
        
        Turn_Based_Combat.Instance.OnTurnsCompleted += ()=> usedTurnForItem = false;
        Turn_Based_Combat.Instance.OnTurnsCompleted += ()=> usedTurnForSwap = false;
        Turn_Based_Combat.Instance.OnNewTurn += ResetAi;
    }
    private void SetValidParticipants()
    {
        GetValidParticipants().ForEach(p=> SetParticipant(p,initialCall:true));
    }
    void LoadAreaBackground(Encounter_Area area)
    {
        foreach (var background in backgrounds)
            background.SetActive(background.name == area.biomeName.ToLower());
    }

    public void SetBattleType(List<string> trainerNames, string battleType)
    {
        switch (battleType)
        {
            case "single":
                StartSingleBattle(trainerNames[0]);
                break;
            case "single-double":
                StartSingleDoubleBattle(trainerNames[0]);
                break;
            case "double":
                //StartDoubleBattle(trainerNames);
                break;
        }
    }
    public void StartWildBattle(Pokemon enemy) //only ever be for wild battles
    {
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
        Wild_pkm.Instance.currentEnemyParticipant = player;
        wildPokemon.AddToExpList(player.pokemon);
        Wild_pkm.Instance.inBattle = true;
        //setup battle
        SetValidParticipants();
        LoadAreaBackground(Encounter_handler.Instance.currentArea);
        SetupBattle();
        Encounter_handler.Instance.currentArea = null;
    }
    private void StartSingleBattle(string trainerName) //single trainer battle
    {
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
        enemy.pokemonTrainerAI.SetupTrainerForBattle(trainerName, false);
        enemy.pokemon = enemy.pokemonTrainerAI.trainerParty[0];
        enemy.AddToExpList(player.pokemon);
        enemy.pokemonTrainerAI.inBattle = true;
        //setup battle
        LoadAreaBackground(enemy.pokemonTrainerAI.trainerData.TrainerLocation);
        SetValidParticipants();
        SetupBattle();
    }

    private void StartSingleDoubleBattle(string trainerName) //1v1 trainer double battle
    {
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
            currentEnemy.pokemonTrainerAI.SetupTrainerForBattle(trainerName, false);
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
        SetValidParticipants();
        enemy.pokemonTrainerAI.inBattle = true;
        enemyPartner.pokemonTrainerAI.inBattle = true;
        LoadAreaBackground(enemy.pokemonTrainerAI.trainerData.TrainerLocation);
        SetupBattle();
    }

    public void SetParticipant(Battle_Participant participant, bool initialCall = false)
    {
        OnSwitchOut?.Invoke(participant);
        participant.isEnemy = Array.IndexOf(battleParticipants, participant) > 1 ;
        if (participant.isPlayer)
        { //for switch-ins
            if (!initialCall)
            { 
                var alivePokemon= Pokemon_party.Instance.GetLivingPokemon();
                participant.pokemon = alivePokemon[Pokemon_party.Instance.selectedMemberIndex - 1];
                foreach (var enemyParticipant  in participant.currentEnemies)
                    enemyParticipant.AddToExpList(participant.pokemon);
            }
        } 
        else
        {//add player participants to get exp from switched in enemy
            foreach (var playerParticipant  in participant.currentEnemies)
                participant.AddToExpList(playerParticipant.pokemon);
            
            participant.pokemon.pokemonName = (participant.isEnemy)? 
                "Foe " + participant.pokemon.pokemonName : participant.pokemon.pokemonName;
        }
        //setup participant for battle
        participant.statData.SaveActualStats();
        participant.ActivateParticipant();
        participant.abilityHandler.SetAbilityMethod();
        CheckParticipantStates();
        OnSwitchIn?.Invoke();
    }

    public List<Battle_Participant> GetValidParticipants()
    {
        return battleParticipants.ToList().Where(p => !p.isActive && p.pokemon != null).ToList();
    }
    public void CheckParticipantStates()
    {
        participantCount = 0;
        foreach (var participant in battleParticipants)
        {
            if (participant.pokemon == null) continue;
            participantCount++;
            //if revived during double battle for example
            if (participant.pokemon.hp > 0 & !participant.isActive)
                participant.ActivateParticipant();
        }
    }
    public void RefreshParticipantUI()
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
        
        InputStateHandler.Instance.ChangeInputState(new InputState(InputStateHandler.StateName.PokemonBattleMoveSelection
            ,new[] { InputStateHandler.StateGroup.PokemonBattle },true,
            movesUI, InputStateHandler.Directional.OmniDirection, moveSelectables,
            moveSelector,true,true,ResetMoveUsability,ResetMoveUsability));
        
        for (var i = _currentParticipant.pokemon.moveSet.Count; i < 4; i++)//only show available moves
            availableMovesText[i].text = "";
    }
    public void UseMove(Move move,Battle_Participant user)
    {
        if(move.powerpoints==0)return;
        move.powerpoints--;

        Turn currentTurn = new Turn(move, Array.IndexOf(battleParticipants,user)
            ,currentEnemyIndex
            , user.pokemon.pokemonID
            ,battleParticipants[currentEnemyIndex].pokemon.pokemonID);
        Turn_Based_Combat.Instance.SaveMove(currentTurn);
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
    public void FaintEvent()
    {
        StartCoroutine(FaintSequence());
    } 
    private IEnumerator FaintSequence()
    {
        while (faintQueue.Count > 0)
        {
            Turn_Based_Combat.Instance.faintEventDelay = true;
            Dialogue_handler.Instance.DisplayBattleInfo(faintQueue[0].pokemon.pokemonName + " fainted!");
            StartCoroutine(faintQueue[0].HandleFaintLogic());
            yield return new WaitUntil(() => !Turn_Based_Combat.Instance.faintEventDelay);
            faintQueue.RemoveAt(0);
        }
    }
    IEnumerator DelayBattleEnd()
    {
        var playerWhiteOut = false;
        yield return new WaitUntil(() => levelUpQueue.Count==0);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        if (runningAway)
        {
            runningAway = false;
            Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " ran away");
        }
        else
        {
            var baseMoneyPayout = 0;
            if(isTrainerBattle)
                baseMoneyPayout = battleParticipants[0].currentEnemies[0].pokemonTrainerAI.trainerData.BaseMoneyPayout;
            if (battleWon)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " won the battle");
                if (isTrainerBattle)
                {
                    var moneyGained = baseMoneyPayout * lastOpponent.currentLevel * MoneyModifier();
                    Game_Load.Instance.playerData.playerMoney += moneyGained;
                    Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " recieved P" + moneyGained);
                }
            }
            else
            {
                if (isTrainerBattle)
                {//last participant is always not null in this situation
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
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        ResetUiAfterBattle(playerWhiteOut);
        if(overworld_actions.Instance.fishing)//battle triggered from fishing
            overworld_actions.Instance.ResetFishingAction();
    }
    public void LevelUpEvent(Pokemon pokemon)
    {
        levelUpQueue.Add(pokemon);
        StartCoroutine(LevelUpSequence(pokemon));
    } 
    private IEnumerator LevelUpSequence(Pokemon pokemonToLevelUp)
    {
        yield return new WaitUntil(() => !Turn_Based_Combat.Instance.levelEventDelay);
        Turn_Based_Combat.Instance.levelEventDelay = true;
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        Dialogue_handler.Instance.DisplayBattleInfo("Wow!");
        Dialogue_handler.Instance.DisplayBattleInfo(pokemonToLevelUp.pokemonName+" leveled up!");
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        PokemonOperations.CheckForNewMove(pokemonToLevelUp);
        if (PokemonOperations.LearningNewMove)
        {
            if (pokemonToLevelUp.moveSet.Count == 4)
            {
                yield return new WaitUntil(() => PokemonOperations.SelectingMoveReplacement);
                yield return new WaitUntil(() => !PokemonOperations.SelectingMoveReplacement);
            }
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        yield return new WaitUntil(() => !PokemonOperations.LearningNewMove);
        levelUpQueue.Remove(pokemonToLevelUp);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        Turn_Based_Combat.Instance.levelEventDelay = false;
        if(battleOver & levelUpQueue.Count==0)
            EndBattle(battleWon);
    }
    public void EndBattle(bool hasWon)
    {
        if (battleOver) return;
        battleWon = hasWon;
        battleOver = true;
        StartCoroutine(DelayBattleEnd());
    }
    void ResetUiAfterBattle(bool playerWhiteOut)
    {
        OnBattleEnd?.Invoke();
        Turn_Based_Combat.Instance.OnNewTurn -= CheckParticipantStates;
        Turn_Based_Combat.Instance.OnNewTurn -= ResetAi;
        Dialogue_handler.Instance.EndDialogue();
        usedTurnForItem = false;
        usedTurnForSwap = false;
        Options_manager.Instance.playerInBattle = false;
        overworld_actions.Instance.doingAction = false;
        battleUI.SetActive(false);
        optionsUI.SetActive(false);
        lastOpponent = null;
        participantCount = 0;
        foreach (var participant in battleParticipants)
            if(participant.pokemon!=null)
            {
                participant.Barrieirs.Clear();
                participant.statData.LoadActualStats();
                participant.statData.ResetBattleState(participant.pokemon);
                participant.pokemon = null;
                participant.additionalTypeImmunity = null;
                participant.PreviousMove = null;
                participant.DeactivateUI();
                if (participant.pokemonTrainerAI != null)
                    participant.pokemonTrainerAI.inBattle = false;
            }
        Encounter_handler.Instance.ResetTrigger();
        overWorld.SetActive(true);
        var location = (playerWhiteOut)? "Poke Center" : Game_Load.Instance.playerData.location;
        if(playerWhiteOut) Options_manager.Instance.HealPartyPokemon();
        Area_manager.Instance.SwitchToArea(location, 0f);
        Dialogue_handler.Instance.canExitDialogue = true;
        InputStateHandler.Instance.ResetGroupUi(InputStateHandler.StateGroup.PokemonBattle);
        battleWon = false;
        battleOver = false;
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
