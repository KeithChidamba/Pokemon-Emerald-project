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
    public List<LevelUpEvent> levelUpQueue = new();
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
                UseMove(_currentParticipant.pokemon.move_set[_currentMoveIndex], _currentParticipant);
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

    void SetupBattle()
    {
        Game_Load.Instance.playerData.playerPosition = Player_movement.instance.transform.position;
        levelUpQueue.Clear();
        Options_manager.Instance.playerInBattle = true;
        overworld_actions.Instance.doingAction = true;
        battleUI.SetActive(true);
        overWorld.SetActive(false);
        Turn_Based_Combat.Instance.ChangeTurn(-1, 0);
    }

    void SetValidParticipants()
    {
        foreach (var participant in battleParticipants)
            if (participant.pokemon != null)
                SetParticipant(participant);
    }
    void LoadAreaBackground(Encounter_Area area)
    {
        foreach (GameObject background in backgrounds)
            background.SetActive(background.name == area.biomeName.ToLower());
    }

    public void StartWildBattle(Pokemon enemy) //only ever be for wild battles
    {
        battleOver = false;
        isTrainerBattle = false;
        isDoubleBattle = false;
        LoadAreaBackground(Encounter_handler.Instance.currentArea);
        battleParticipants[0].pokemon = Pokemon_party.Instance.party[0];
        battleParticipants[0].currentEnemies.Add(battleParticipants[2]);
        battleParticipants[2].pokemon = enemy;
        battleParticipants[2].currentEnemies.Add(battleParticipants[0]);
        Wild_pkm.Instance.participant = battleParticipants[2];
        Wild_pkm.Instance.currentEnemyPokemon = battleParticipants[0];
        battleParticipants[2].AddToExpList(battleParticipants[0].pokemon);
        SetValidParticipants();
        Wild_pkm.Instance.inBattle = true;
        SetupBattle();
        Encounter_handler.Instance.currentArea = null;
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

    private void StartSingleBattle(string trainerName) //single trainer battle
    {
        battleOver = false;
        isTrainerBattle = true;
        isDoubleBattle = false;
        battleParticipants[0].pokemon = Pokemon_party.Instance.party[0];
        battleParticipants[0].currentEnemies.Add(battleParticipants[2]);
        battleParticipants[2].currentEnemies.Add(battleParticipants[0]);
        battleParticipants[2].pokemonTrainerAI = battleParticipants[2].GetComponent<Enemy_trainer>();
        battleParticipants[2].pokemonTrainerAI.SetupTrainerForBattle(trainerName, false);
        battleParticipants[2].pokemon = battleParticipants[2].pokemonTrainerAI.trainerParty[0];
        LoadAreaBackground(battleParticipants[2].pokemonTrainerAI.trainerData.TrainerLocation);
        battleParticipants[2].AddToExpList(battleParticipants[0].pokemon);
        SetValidParticipants();
        battleParticipants[2].pokemonTrainerAI.inBattle = true;
        SetupBattle();
    }

    private void StartSingleDoubleBattle(string trainerName) //1v1 trainer double battle
    {
        battleOver = false;
        isTrainerBattle = true;
        isDoubleBattle = true;
        var alivePokemon = Pokemon_party.Instance.GetLivingPokemon();
        battleParticipants[0].pokemon = alivePokemon[0];
        if (Pokemon_party.Instance.numMembers > 1)
            battleParticipants[1].pokemon = alivePokemon[1];
        battleParticipants[2].pokemonTrainerAI = battleParticipants[2].GetComponent<Enemy_trainer>();
        battleParticipants[3].pokemonTrainerAI = battleParticipants[3].GetComponent<Enemy_trainer>();
        battleParticipants[2].pokemonTrainerAI.SetupTrainerForBattle(trainerName, false);
        battleParticipants[3].pokemonTrainerAI.SetupTrainerForBattle(trainerName, true);
        battleParticipants[3].pokemonTrainerAI.trainerData = battleParticipants[2].pokemonTrainerAI.trainerData;
        battleParticipants[3].pokemonTrainerAI.trainerParty = battleParticipants[2].pokemonTrainerAI.trainerParty;
        battleParticipants[2].pokemon = battleParticipants[2].pokemonTrainerAI.trainerParty[0];
        battleParticipants[3].pokemon = battleParticipants[3].pokemonTrainerAI.trainerParty[1];
        for (int i = 0; i < 2; i++) //double battle always has 2 enemies enter
        {
            if (battleParticipants[i + 2].pokemon != null)
            {
                battleParticipants[0].currentEnemies.Add(battleParticipants[i + 2]);
                if (Pokemon_party.Instance.numMembers > 1)
                    battleParticipants[1].currentEnemies.Add(battleParticipants[i + 2]);
            }

            if (battleParticipants[i].pokemon != null)
            {
                battleParticipants[2].currentEnemies.Add(battleParticipants[i]);
                battleParticipants[3].currentEnemies.Add(battleParticipants[i]);
            }
        }
        for (int i = 0; i < 2; i++)
            if (battleParticipants[i].pokemon != null)
                foreach (var enemy in battleParticipants[i].currentEnemies)
                    enemy.AddToExpList(battleParticipants[i].pokemon);
        SetValidParticipants();
        battleParticipants[2].pokemonTrainerAI.inBattle = true;
        battleParticipants[3].pokemonTrainerAI.inBattle = true;
        LoadAreaBackground(battleParticipants[2].pokemonTrainerAI.trainerData.TrainerLocation);
        SetupBattle();
    }
public void SetParticipant(Battle_Participant participant)
    {
        if (participant.isPlayer)
        { //for switch-ins
            if (Pokemon_party.Instance.swappingIn || Pokemon_party.Instance.swapOutNext)
            {
                var alivePokemon=Pokemon_party.Instance.GetLivingPokemon();
                participant.pokemon = alivePokemon[Pokemon_party.Instance.selectedMemberIndex - 1];
                foreach (Battle_Participant enemyParticipant  in participant.currentEnemies)
                    enemyParticipant.AddToExpList(participant.pokemon);
            }
        }
        else
        {//add player participants to get exp from switched in enemy
            foreach (Battle_Participant playerParticipant  in participant.currentEnemies)
                participant.AddToExpList(playerParticipant.pokemon);
        }
        participant.statData.SaveActualStats();
        participant.ActivateParticipant();
        participant.abilityHandler.SetAbilityMethod();
        CountParticipants();
    }
    public void CountParticipants()
    {
         participantCount = battleParticipants.Count(p => p.pokemon != null);
    }
    public void RefreshParticipantUI()
    {
        foreach(Battle_Participant p in battleParticipants)
            if (p.pokemon != null)
                p.RefreshStatusEffectImage();
    }
    void LoadTextDataForMoves()
    {
        int j = 0;
        foreach(Move m in _currentParticipant.pokemon.move_set)
            if (m != null)
            {
                availableMovesText[j].text = _currentParticipant.pokemon.move_set[j].Move_name;
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
        if(move.Powerpoints==0)return;
        move.Powerpoints--;
        doingMove = true;
        choosingMove = false;
        movesUI.SetActive(false);
        optionsUI.SetActive(false);
        Turn currentTurn = new Turn(move, Array.IndexOf(battleParticipants,user)
            ,currentEnemyIndex
            , user.pokemon.Pokemon_ID.ToString()
            ,battleParticipants[currentEnemyIndex].pokemon.Pokemon_ID.ToString());
        Turn_Based_Combat.Instance.SaveMove(currentTurn);
    }
    public void ResetMoveUsability()
    {
        selectedMove = false; 
        moveButtons[_currentMoveIndex].GetComponent<Button>().interactable = true;
        _currentMoveIndex = 0;
    }
    public void Select_Move(int moveNum)
    {
        ResetMoveUsability();
        _currentMoveIndex = moveNum-1;
        Move currentMove = _currentParticipant.pokemon.move_set[_currentMoveIndex];
        movePowerPointsText.text = "PP: " + currentMove.Powerpoints+ "/" + currentMove.max_Powerpoints;
        movePowerPointsText.color = (currentMove.Powerpoints == 0)? Color.red : Color.black;
        moveTypeText.text = currentMove.type.Type_name;
        selectedMove = true;
        moveButtons[_currentMoveIndex].GetComponent<Button>().interactable = false;
    }
    int MoneyModifier()
    {
        var alivePokemon = Pokemon_party.Instance.GetLivingPokemon(); 
        foreach(var currentPokemon in alivePokemon)//exp share split, assuming there's only ever 1 exp share in the game
            if (currentPokemon.HasItem)
                if(currentPokemon.HeldItem.itemName == "Amulet Coin")
                    return 2;
        return 1;
    }
    IEnumerator DelayBattleEnd()
    {
        bool playerWhiteOut = false;
        yield return new WaitUntil(() => levelUpQueue.Count==0);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        if (runningAway)
            Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " ran away");
        else
        {
            int baseMoneyPayout=0;
            if(isTrainerBattle)
                baseMoneyPayout=battleParticipants[0].currentEnemies[0].pokemonTrainerAI.trainerData.BaseMoneyPayout;
            if (battleWon)
            {
                Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " won the battle");
                if (isTrainerBattle)
                {
                    int moneyGained = baseMoneyPayout * lastOpponent.Current_level * MoneyModifier();
                    Game_Load.Instance.playerData.playerMoney += moneyGained;
                    Dialogue_handler.Instance.DisplayBattleInfo(Game_Load.Instance.playerData.playerName + " recieved P" + moneyGained);
                }
            }
            else
            {
                if (isTrainerBattle)
                {
                    var playerLastParticipant = battleParticipants.ToList()
                        .Where(p => p.isActive & p.isPlayer).ToList()[0];//last participant is always not null in this case
                    lastOpponent = playerLastParticipant.currentEnemies.Where(p=>p.isActive).ToList()[0].pokemon;
                    Game_Load.Instance.playerData.playerMoney -= baseMoneyPayout 
                                                                   * Game_Load.Instance.playerData.numBadges 
                                                                   * lastOpponent.Current_level;
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
        if(overworld_actions.Instance.fishing)
            overworld_actions.Instance.ResetFishingAction();
    }
    public void LevelUpEvent(Pokemon pkm)
    {
        LevelUpEvent levelEvent=new LevelUpEvent(pkm);
        levelUpQueue.Add(levelEvent);
        StartCoroutine(LevelUp_Sequence(levelEvent));
    } 
    IEnumerator LevelUp_Sequence(LevelUpEvent pkmLevelUp)
    {
        yield return new WaitUntil(() => !Turn_Based_Combat.Instance.levelEventDelay);
        Turn_Based_Combat.Instance.levelEventDelay = true;
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        Dialogue_handler.Instance.DisplayBattleInfo("Wow!");
        yield return new WaitForSeconds(0.5f);
        Dialogue_handler.Instance.DisplayBattleInfo(pkmLevelUp.pokemon.Pokemon_name+" leveled up!");
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        pkmLevelUp.Execute();
        yield return new WaitForSeconds(0.5f);
        if (PokemonOperations.LearningNewMove)
        {
            if (pkmLevelUp.pokemon.move_set.Count > 3)
            {
                yield return new WaitUntil(() => Options_manager.Instance.selectedNewMoveOption);
                yield return new WaitForSeconds(0.5f);
                if (Pokemon_Details.Instance.learningMove)
                    yield return new WaitUntil(() => !Pokemon_Details.Instance.learningMove);
                else
                    Turn_Based_Combat.Instance.levelEventDelay = false;
            }
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        else
        //in case if leveled up and didn't learn move
            levelUpQueue.Remove(pkmLevelUp);
        Turn_Based_Combat.Instance.levelEventDelay = false;
        if(battleOver & levelUpQueue.Count==0)
            End_Battle(battleWon);
    }
    public void End_Battle(bool Haswon)
    {
        battleWon = Haswon;
        battleOver = true;
        StartCoroutine(DelayBattleEnd());
    }
    void ResetUiAfterBattle(bool playerWhiteOut)
    {
        OnBattleEnd?.Invoke();
        Dialogue_handler.Instance.EndDialogue();
        Options_manager.Instance.playerInBattle = false;
        overworld_actions.Instance.doingAction = false;
        battleUI.SetActive(false);
        optionsUI.SetActive(false);
        lastOpponent = null;
        foreach (Battle_Participant p in battleParticipants)
            if(p.pokemon!=null)
            {
                p.statData.LoadActualStats();
                p.statData.ResetBattleState(p.pokemon,true);
                p.pokemon = null;
                p.previousMove = "";
                p.DeactivateUI();
                if (p.pokemonTrainerAI != null)
                    p.pokemonTrainerAI.inBattle = false;
            }
        Encounter_handler.Instance.ResetTrigger();
        overWorld.SetActive(true);
        var location = (playerWhiteOut)? "Poke Center" : Game_Load.Instance.playerData.location;
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
            Dialogue_handler.Instance.DisplayBattleInfo(_currentParticipant.pokemon.Pokemon_name +" is trapped");
        else
        {
            if (isTrainerBattle )
                Dialogue_handler.Instance.DisplayBattleInfo("Can't run away from trainer battle");
            else
            { 
                int random = Utility.RandomRange(1,11);
                if (battleParticipants[0].pokemon.Current_level < 
                    battleParticipants[0].currentEnemies[0].pokemon.Current_level)//lower chance if weaker
                    random--;
                if (random > 5) //initially 50/50 chance to run
                {
                    Wild_pkm.Instance.inBattle = false;
                    End_Battle(false);
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
