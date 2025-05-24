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
    public Battle_Participant[] battleParticipants = { null, null, null, null };
    public List<Pokemon> levelUpQueue = new();
    public Text movePowerPointsText;
    public Text moveTypeText;
    public Text[] availableMovesText;
    public GameObject[] moveButtons;
    public bool isTrainerBattle = false;
    public bool isDoubleBattle = false;
    public int participantCount = 0;
    public bool displayingInfo = false;
    public bool battleOver = false;
    public bool battleWon = false;
    public GameObject overWorld;
    public List<GameObject> backgrounds;
    public bool viewingOptions = false;
    public bool choosingMove = false;
    public bool runningAway = false;
    public bool selectedMove = false;
    private int _currentMoveIndex = 0;
    public int currentEnemyIndex = 0;
    public bool doingMove = false;
    public Pokemon lastOpponent;
    private Battle_Participant _currentParticipant;
    public static Battle_handler Instance;
    public event Action OnBattleEnd;

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
        HandlePlayerInput();
    }

    private void HandlePlayerInput()
    {
        _currentParticipant = battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex];
        if (selectedMove && (Input.GetKeyDown(KeyCode.F)))
        {
            if (_currentParticipant.enemySelected)
            {
                UseMove(_currentParticipant.pokemon.moveSet[_currentMoveIndex], _currentParticipant);
                choosingMove = false;
            }
            else
                Dialogue_handler.Instance.DisplayInfo("Click on who you will attack", "Details");
        }

        if (choosingMove && (Input.GetKeyDown(KeyCode.Escape))) //exit move selection
        {
            ViewOptions();
            choosingMove = false;
            ResetMoveUsability();
        }

        if (displayingInfo || battleOver)
        {
            optionsUI.SetActive(false);
            viewingOptions = false;
        }

        if (overworld_actions.Instance.usingUI)
        {
           // Dialogue_handler.Instance.EndDialogue();
            Wild_pkm.Instance.canAttack = false;
            optionsUI.SetActive(false);
            viewingOptions = false;
        }
        else
        {
            if (!choosingMove && !runningAway && !doingMove && !displayingInfo && !doingMove && !battleOver)
                viewingOptions = true;
            else
                viewingOptions = false;
            if (viewingOptions)
            {
                ResetAi();
                Dialogue_handler.Instance.DisplayInfo("What will you do?", "Details");
                optionsUI.SetActive(true);
            }
        }
        AutoAim();
    }

    void ResetAi()
    {
        //improve eventually to work as proper strategy AI
        if (!isTrainerBattle)
            Wild_pkm.Instance.Invoke(nameof(Wild_pkm.Instance.CanAttack), 1f);
        else
            foreach (var participant in battleParticipants)
                if (participant.pokemon != null & !participant.isPlayer)
                    participant.pokemonTrainerAI.Invoke(nameof(participant.pokemonTrainerAI.CanAttack), 1f);
    }

    void AutoAim()
    {
        if (!isDoubleBattle && Turn_Based_Combat.Instance.currentTurnIndex == 0) //if single battle, auto aim at enemy
        {
            currentEnemyIndex = 2;
            _currentParticipant.enemySelected = true;
        }
    }

    public void ViewOptions()
    {
        if (!Options_manager.Instance.playerInBattle) return;
        movesUI.SetActive(false);
        optionsUI.SetActive(true);
    }

    public void SelectEnemy(int choice)
    {
        if (Turn_Based_Combat.Instance.currentTurnIndex > 1) return; //not player's turn
        if (isDoubleBattle & choice < 2)
        {
            //cant attack own pokemon
            _currentParticipant.enemySelected = false;
            return;
        }
        _currentParticipant.enemySelected = true;
        currentEnemyIndex = choice;
    }

    private void SetupBattle()
    {
        Turn_Based_Combat.Instance.OnNewTurn += CheckParticipantStates;
        Game_Load.Instance.playerData.playerPosition = Player_movement.Instance.transform.position;
        levelUpQueue.Clear();
        Options_manager.Instance.playerInBattle = true;
        overworld_actions.Instance.doingAction = true;
        battleUI.SetActive(true);
        overWorld.SetActive(false);
        Turn_Based_Combat.Instance.ChangeTurn(-1, 0);
    }

    private void SetValidParticipants()
    {
        foreach (var participant in battleParticipants)
            if (participant.pokemon != null)
                SetParticipant(participant);
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
public void SetParticipant(Battle_Participant participant)
    {
        participant.isEnemy = Array.IndexOf(battleParticipants, participant) > 1 ;
        if (participant.isPlayer)
        { //for switch-ins
            if (Pokemon_party.Instance.swappingIn || Pokemon_party.Instance.swapOutNext)
            { 
                var alivePokemon=Pokemon_party.Instance.GetLivingPokemon();
                participant.pokemon = alivePokemon[Pokemon_party.Instance.selectedMemberIndex - 1];
                foreach (var enemyParticipant  in participant.currentEnemies)
                    enemyParticipant.AddToExpList(participant.pokemon);
            }
        } 
        else
        {//add player participants to get exp from switched in enemy
            foreach (var playerParticipant  in participant.currentEnemies)
                participant.AddToExpList(playerParticipant.pokemon);
            
            participant.pokemon.pokemonName = (participant.isEnemy )? 
                "Foe " + participant.pokemon.pokemonName : participant.pokemon.pokemonName;
        }
        //setup participant for battle
        participant.statData.SaveActualStats();
        participant.ActivateParticipant();
        participant.abilityHandler.SetAbilityMethod();
        CheckParticipantStates();
    }

    public void CheckParticipantStates()
    {
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
        foreach(var participant in battleParticipants)
            if (participant.pokemon != null)
                participant.RefreshStatusEffectImage();
    }
    void LoadTextDataForMoves()
    {
        var j = 0;
        foreach(var move in _currentParticipant.pokemon.moveSet)
            if (move != null)
            {
                availableMovesText[j].text = _currentParticipant.pokemon.moveSet[j].moveName;
                moveButtons[j].SetActive(true);
                j++;
            }
        for (int i = j; i < 4; i++)//only show available moves
        {
            availableMovesText[i].text = "";
            moveButtons[i].SetActive(false);
        }
    }
    public void UseMove(Move move,Battle_Participant user)
    {
        if(move.powerpoints==0)return;
        move.powerpoints--;
        doingMove = true;
        choosingMove = false;
        movesUI.SetActive(false);
        optionsUI.SetActive(false);
        Turn currentTurn = new Turn(move, Array.IndexOf(battleParticipants,user)
            ,currentEnemyIndex
            , user.pokemon.pokemonID.ToString()
            ,battleParticipants[currentEnemyIndex].pokemon.pokemonID.ToString());
        Turn_Based_Combat.Instance.SaveMove(currentTurn);
    }
    public void ResetMoveUsability()
    {
        selectedMove = false; 
        moveButtons[_currentMoveIndex].GetComponent<Button>().interactable = true;
        _currentMoveIndex = 0;
    }
    public void SelectMove(int moveNum)
    {
        ResetMoveUsability();
        _currentMoveIndex = moveNum-1;
        var currentMove = _currentParticipant.pokemon.moveSet[_currentMoveIndex];
        movePowerPointsText.text = "PP: " + currentMove.powerpoints+ "/" + currentMove.maxPowerpoints;
        movePowerPointsText.color = (currentMove.powerpoints == 0)? Color.red : Color.black;
        moveTypeText.text = currentMove.type.typeName;
        selectedMove = true;
        moveButtons[_currentMoveIndex].GetComponent<Button>().interactable = false;
    }
    int MoneyModifier()
    {
        var playerParticipants = battleParticipants.ToList()
            .Where(p => p.isActive & p.isPlayer).ToList();
        foreach(var participant in playerParticipants)
            if (participant.pokemon.hasItem)
                if(participant.pokemon.heldItem.itemName == "Amulet Coin")
                    return 2;
        return 1;
    }
    IEnumerator DelayBattleEnd()
    {
        var playerWhiteOut = false;
        yield return new WaitUntil(() => levelUpQueue.Count==0);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        if (runningAway)
            Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " ran away");
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
                    Dialogue_handler.Instance.DisplayBattleInfo("All your pokemon have fainted");
                    playerWhiteOut = true;
                }
            }
        }
        yield return new WaitForSeconds(2f);
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
        battleWon = hasWon;
        battleOver = true;
        StartCoroutine(DelayBattleEnd());
    }
    void ResetUiAfterBattle(bool playerWhiteOut)
    {
        OnBattleEnd?.Invoke();
        Turn_Based_Combat.Instance.OnNewTurn -= CheckParticipantStates;
        Dialogue_handler.Instance.EndDialogue();
        Options_manager.Instance.playerInBattle = false;
        overworld_actions.Instance.doingAction = false;
        battleUI.SetActive(false);
        optionsUI.SetActive(false);
        lastOpponent = null;
        foreach (var participant in battleParticipants)
            if(participant.pokemon!=null)
            {
                participant.statData.LoadActualStats();
                participant.statData.ResetBattleState(participant.pokemon,true);
                participant.pokemon = null;
                participant.previousMove = "";
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
        battleWon = false;
        battleOver = false;
    }
    public void RunAway()
    {
        runningAway = true;
        displayingInfo = true;
        if(!isTrainerBattle & !_currentParticipant.canEscape)
            Dialogue_handler.Instance.DisplayBattleInfo(_currentParticipant.pokemon.pokemonName +" is trapped");
        else
        {
            if (isTrainerBattle )
                Dialogue_handler.Instance.DisplayBattleInfo("Can't run away from trainer battle");
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
                    Turn_Based_Combat.Instance.Invoke(nameof(Turn_Based_Combat.Instance.NextTurn),0.9f);
                }
            }
        }
        Invoke(nameof(ResetRunLogic),1f);
    }
void ResetRunLogic()
{
    runningAway = false;
    displayingInfo = false;
}
    public void DisplayMovesUI()
    {
        choosingMove = true;
        viewingOptions = false;
        optionsUI.SetActive(false);
        movesUI.SetActive(true);
        LoadTextDataForMoves();
    }
}
