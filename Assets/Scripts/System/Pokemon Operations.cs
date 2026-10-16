using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
public enum FriendshipModifier{Fainted,LevelUp,Vitamin,Berry} 
public enum StatusEffect{None,Paralysis,Burn,Poison,BadlyPoison,Freeze,Sleep}
public enum Gender{None,Male,Female}
public enum PokemonType
{
    Normal, Fire, Water, Electric, Grass, Ice,
    Fighting, Poison, Ground, Flying, Psychic,
    Bug, Rock, Ghost, Dragon, Dark, Steel,Typeless
}
public enum Stat{None,Attack,Defense,SpecialAttack,SpecialDefense,Speed,Hp,Crit,Accuracy,Evasion,Multi}
public enum ExpGroup{Erratic,Fast,MediumFast,MediumSlow,Slow,Fluctuating}

public class PokemonOperations : MonoBehaviour,IInjectable
{
    [SerializeField] private GameObject evolutionUi;
    [SerializeField] private Image evolutionImage;
    [SerializeField] private Image evolutionImageMask;
    [SerializeField] private Image evolutionBackground;
    private bool _learningNewMove;
    private static ushort _trainerID;
    private static ushort _secretID;
    public event Action<Stat,bool> OnEvChange;
    public event Action<Pokemon,bool> OnPokeballUsed;
    /// <summary>
    /// [Item usage]Used to notify when a move has been learned or skipped
    /// </summary>
    public event Action<Pokemon, bool> OnMoveLearnOperationComplete;
    
    private BattleHandler _battleHandler;
    private WildPokemonAiHandler _wildPokemonHandler;
    private PokemonPartyHandler _playerParty;
    private InputStateHandler _inputStateHandler;
    private GameLoadingHandler _gameHandler;
    private BattleVisuals _battleVisuals;
    private PokemonDetailsHandler _pokemonDetailsHandler;
    private DialogueHandler _dialogueHandler;
    private GameUiHandler _gameUiHandler;
    private GameLoadingHandler _gameLoadingHandler;
    private BattleOperations _battleOperations;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private static PokemonOperations _instance;
    private ServiceContainer _container;
    public void Inject(ServiceContainer container)
    {
        _container = container;
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _wildPokemonHandler = container.Resolve<WildPokemonAiHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _playerParty = container.Resolve<PokemonPartyHandler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _battleVisuals = container.Resolve<BattleVisuals>();
        _gameHandler = container.Resolve<GameLoadingHandler>();
        _pokemonDetailsHandler=container.Resolve<PokemonDetailsHandler>();
        _gameUiHandler = container.Resolve<GameUiHandler>();
        _gameLoadingHandler = container.Resolve<GameLoadingHandler>();
        _battleOperations = container.Resolve<BattleOperations>();
        _instance = container.Resolve<PokemonOperations>();
        gameObject.SetActive(true);
    }   

    public void OnInject()
    {
       _gameLoadingHandler.OnGameStarted += SetIds;
    }

    private void SetIds()
    {
        _trainerID = _gameHandler.playerData.trainerID;
        _secretID = _gameHandler.playerData.secretID;
    }
    public static bool ContainsType(PokemonType[]typesList ,PokemonTypeData typesToCheck)
    {
        foreach (var type in typesList)
            if (type == typesToCheck.typeEnum)
                return true;
        return false;
    }
    public static void LoadMovesFromSource(List<MoveSaveData> movesetTemplate, Pokemon pokemon)
    {
        pokemon.moveSet.Clear();
        foreach (var move in movesetTemplate)
        {
            var moveCopy = InstanceFactory.CreateMove(Resources.Load<Move>(
                DirectoryHandler.GetDirectory(AssetDirectory.Moves) + move.moveName));
            
            moveCopy.powerpoints = move.powerPoints;
            moveCopy.maxPowerpoints = move.maxPowerpoints;
            pokemon.moveSet.Add(moveCopy);
        }
    }
    public static int CalculateExpForLevel(int desiredLevel, ExpGroup expGroup)
    {
        if (desiredLevel == 0)
        {
            Debug.LogError("cant level up to 0");
            return 0;
        }
        return CalculateExpForNextLevel(desiredLevel - 1,expGroup)+1;
    }
    public static int CalculateExpForNextLevel(int currentLevel, ExpGroup expGroup)
    {
        var cube = Utility.Cube(currentLevel);
        var square = Utility.Square(currentLevel);
        switch (expGroup)
        {
            case ExpGroup.Erratic: 
                return CalculateErratic(currentLevel);
            case ExpGroup.Fast: 
                return (int)math.trunc((4 * cube ) / 5f );
            case ExpGroup.MediumFast: 
                return cube;
            case ExpGroup.MediumSlow:
                return (int)math.trunc( ( (6 * cube ) / 5f) - (15 * square ) + (100 * currentLevel) - 140 );
            case ExpGroup.Slow:
                return (int)math.trunc((5 * cube ) / 4f);
            case ExpGroup.Fluctuating:
                return CalculateFluctuating(currentLevel);
            default:
                Debug.Log("pokemon has no exp group");
                break;
        }
        return 0;
    }
    private static int CalculateErratic(int level)
    {
        var cube = Utility.Cube(level);
        if (0 < level & level <= 50)
            return (int)math.trunc( (cube * (100 - level)) / 50f );
        if (50 < level & level <= 68)
            return (int)math.trunc( ( cube * (150 - level)) / 100f );
        if (68 < level & level <= 98)
            return (int)math.trunc( ( cube * (1911 - (10*level) ) ) / 1500f );
        if (98 < level & level <= 100)
            return (int)math.trunc( ( cube * (160 - level)) / 100f );
        return 0;
    }
    private static int CalculateFluctuating(int level)
    {
        var cube = Utility.Cube(level);
        if (0 < level & level <= 15)
            return (int)math.trunc( cube *  (24 + math.floor((level+1) / 3f) / 50f) );
        if (15 < level & level <= 36)
            return (int)math.trunc( cube *  ( (14 + level) / 50f) );
        if (36 < level & level <= 100)
            return (int)math.trunc( cube * ( (32 + math.floor(level/2f) ) / 50f) );
        return 0;
    }

    public ref float GetEvStatRef(Stat stat, Pokemon pkm)
    {
        switch (stat)
        {
            case Stat.Hp: return ref pkm.hpEv;
            case Stat.Attack: return ref pkm.attackEv;
            case Stat.Defense: return ref pkm.defenseEv;
            case Stat.SpecialAttack: return ref pkm.specialAttackEv;
            case Stat.SpecialDefense: return ref pkm.specialDefenseEv;
            case Stat.Speed: return ref pkm.speedEv;
            default:
                throw new ArgumentException($"Invalid stat parsed: {stat}");
        }
    }
    public void CalculateEvForStat(Stat stat,float evAmount,Pokemon pkm)
    {
        ref float evRef = ref GetEvStatRef(stat, pkm);
        evRef = CheckEvLimit(evRef,evAmount,pkm,stat);
    }
    private float CheckEvLimit(float currentEv,float amount,Pokemon pokemon,Stat statToChange)
    {
        var totalEv = pokemon.hpEv + pokemon.attackEv + pokemon.defenseEv +
                        pokemon.specialAttackEv + pokemon.specialDefenseEv + pokemon.speedEv;

        // Prevent over-adding if already at cap
        if (amount > 0 && (currentEv >= 252 || totalEv >= 510))
        {
            OnEvChange?.Invoke(statToChange,false);
            return currentEv;
        }

        // Calculate clamped new EV
        float maxAssignable = Math.Min(252 - currentEv, 510 - totalEv);
        float clampedAmount = Math.Clamp(amount, -currentEv, maxAssignable);
        bool changed = clampedAmount != 0;

        OnEvChange?.Invoke(statToChange,changed);
        
        return currentEv + clampedAmount;
    }
    public IEnumerator LearnTmOrHm(MoveLearningMachineInfo moveInfo, Pokemon currentPokemon)
    {
        yield return LearnMove(moveInfo.move, currentPokemon,true,false);
    }
    public static IEnumerator WaitForNewMoveCheck(Pokemon currentPokemon, bool isPartyPokemon)
    {
        if (currentPokemon.currentLevel > currentPokemon.learnSet[^1].requiredLevel)
        {//No more moves to learn
            yield break;
        }
        //Handle move learning
        foreach (var move in currentPokemon.learnSet)
        {
            if (currentPokemon.currentLevel < move.requiredLevel)
                break;
            if (currentPokemon.currentLevel > move.requiredLevel)
                continue;
            
            var moveName = move.GetName().ToLower();
            var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
            var moveFromAsset = Resources.Load<Move>(assetPath);
            if (isPartyPokemon)
            {
                yield return LearnMove(moveFromAsset,currentPokemon,true,true);
            }
            else
            {
                WildPokemonLearnMove(moveFromAsset,currentPokemon);
            }
            yield return _instance._dialogueHandler.AwaitAllDialogue();
        }
    }
    private static void WildPokemonLearnMove(Move moveFromAsset,Pokemon currentPokemon)
    {
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        if (currentPokemon.moveSet.Any(move => move.moveName == newMove.moveName))
        {
            return;
        }
        var maxMoveCount = 4;
        if (currentPokemon.moveSet.Count == maxMoveCount) 
        {
            //wild pokemon get generated with somewhat random moveset choices
            currentPokemon.moveSet[Utility.RandomRange(0, maxMoveCount)]
                = InstanceFactory.CreateMove(moveFromAsset);
        }
        else currentPokemon.moveSet.Add(newMove);
    }
    private static IEnumerator LearnMove(Move moveFromAsset,Pokemon currentPokemon, bool isPartyPokemon,bool removeDetailsUi)
    {
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        
        if (currentPokemon.moveSet.Any(move=> move.moveName == newMove.moveName))
        {
            //this will only ever happen because of the player
            //Tm move already teaching an already taught move
            if (isPartyPokemon)
            {
                _instance._dialogueHandler.DisplayDetails(
                    $"{currentPokemon.pokemonDisplayName} already knows {newMove.moveName}");
            }

            _instance.OnMoveLearnOperationComplete?.Invoke(currentPokemon,false);
            yield break;
        }

        var maxMoveCount = 4;
        if (currentPokemon.moveSet.Count == maxMoveCount) 
        {
            if (isPartyPokemon)
            {
                _instance._learningNewMove = true;
                
                _instance._dialogueHandler.DisplayCustomOptions(
                    $"{currentPokemon.pokemonDisplayName} is trying to learn {newMove.moveName} ,do you want it to learn" +
                    $" {newMove.moveName}?", new[] { "Yes", "No" }
                    ,new Action[]
                    {
                        () => _instance.LearnMove(currentPokemon,moveFromAsset,removeDetailsUi)
                        , () => _instance.SkipMove(currentPokemon,moveFromAsset)
                    });
                
                //Await move learning events
                yield return new WaitUntil(()=>!_instance._learningNewMove);
            }
            else
            {
                //wild pokemon get generated with somewhat random moveset choices
                currentPokemon.moveSet[Utility.RandomRange(0, maxMoveCount)]
                    = InstanceFactory.CreateMove(moveFromAsset);
            }
        }
        else
        {
            if (isPartyPokemon)
            {
                _instance._dialogueHandler.DisplayBattleInfo(
                    $"{currentPokemon.pokemonDisplayName} learned {newMove.moveName}");
                _instance.OnMoveLearnOperationComplete?.Invoke(currentPokemon,true);
            }
            currentPokemon.moveSet.Add(newMove);
        }
        
    }
    private void LearnMove(Pokemon currentPokemon,Move newMoveAsset,bool removeDetailsUi = true)
    {        
        _pokemonDetailsHandler.SetUsage(PokemonDetailsUsage.LearnMoves);
        _pokemonDetailsHandler.OnMoveSelected += LearnSelectedMoveOperation;
        _gameUiHandler.ViewPokemonDetails(currentPokemon);
        _dialogueHandler.EndDialogue();
        _inputStateHandler.OnStateRemoved += SkipMoveCallBack;
        return;
        void LearnSelectedMoveOperation(int moveIndex)
        {
            _pokemonDetailsHandler.OnMoveSelected -= LearnSelectedMoveOperation;
            _inputStateHandler.OnStateRemoved -= SkipMoveCallBack;
            
            if (_battleHandler.BattleInProgress)
            {
                _inputStateHandler.OnStateRemoved += DisplayLearnText;
            }
            else
            {
                _dialogueHandler.DisplayDetails(
                    currentPokemon.pokemonDisplayName + " forgot " +
                    currentPokemon.moveSet[moveIndex].moveName 
                    + " and learned " + newMoveAsset.moveName);
                
                currentPokemon.moveSet[moveIndex] = InstanceFactory.CreateMove(newMoveAsset);
                OnMoveLearnOperationComplete?.Invoke(currentPokemon,true);
                _learningNewMove = false;
            }
            if (removeDetailsUi)
            {
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetailsMoveSelection);
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetails,true);
            }
            return;
            void DisplayLearnText(InputState state)
            {
                if (state.stateName != InputStateName.PokemonDetails) return;
                _inputStateHandler.OnStateRemoved -= DisplayLearnText;
                
                _dialogueHandler.DisplayBattleInfo(currentPokemon.pokemonDisplayName + " forgot " 
                    + currentPokemon.moveSet[moveIndex].moveName 
                    + " and learned " + newMoveAsset.moveName);
                
                currentPokemon.moveSet[moveIndex] = InstanceFactory.CreateMove(newMoveAsset);
                _learningNewMove = false;
            }
        }
        void SkipMoveCallBack(InputState state)
        {
            if (state.stateName != InputStateName.PokemonDetailsMoveSelection) return;
            _inputStateHandler.OnStateRemoved -= SkipMoveCallBack;
            _pokemonDetailsHandler.OnMoveSelected -= LearnSelectedMoveOperation;
            //if started learning but rejected it on move selection screen
            
            if (_battleHandler.BattleInProgress)
            {
                _inputStateHandler.OnStateRemoved += DisplaySkipText;
            }else
            {
                _dialogueHandler.DisplayDetails(currentPokemon.pokemonDisplayName +
                                                " did not learn "+newMoveAsset.moveName);
                OnMoveLearnOperationComplete?.Invoke(currentPokemon,false);   
                _learningNewMove = false;             
            }
            if (removeDetailsUi)
            {
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetailsMoveSelection);
                _inputStateHandler.ResetSpecificUi(InputStateName.PokemonDetails,true);
            }
            return;
            void DisplaySkipText(InputState newState)
            {
                if (newState.stateName != InputStateName.PokemonDetails) return;
                _inputStateHandler.OnStateRemoved -= DisplaySkipText;
                
                _dialogueHandler.DisplayBattleInfo(currentPokemon.pokemonDisplayName +
                                                   " did not learn "+newMoveAsset.moveName);
                _learningNewMove = false;
            }
        }
    }
    private void SkipMove(Pokemon currentPokemon,Move newMoveAsset)
    {
        _dialogueHandler.DeletePreviousOptions();
        _dialogueHandler.DisplayBattleInfo(currentPokemon.pokemonDisplayName +
                                           " did not learn "+newMoveAsset.moveName);
        OnMoveLearnOperationComplete?.Invoke(currentPokemon,false);
        _learningNewMove = false;
    }
    public static void UpdateHealthPhase(PokemonStateHandler state,RawImage hpSliderColor)
    {
        List<(float threshold, Color color, int phase)> healthPhases = new()
        {
            (0.2f, Color.red, 3),
            (0.5f, Color.yellow, 2),
            (1f, Color.green, 1),
        };

        var hpPercentage = state.pokemon.hp / state.pokemon.maxHp;
        var phase = healthPhases.FirstOrDefault(hp => hpPercentage <= hp.threshold);

        hpSliderColor.color = phase.color;
        state.healthPhase = phase.phase;
    }
    public static void SetPokemonTraits(Pokemon newPokemon)
    {
        //Generate Personality Value
        System.Random rand = new System.Random();
        // 16-bit random values
        int part1 = rand.Next(0, 65536); 
        int part2 = rand.Next(0, 65536); 
        newPokemon.personalityValue = (uint)math.abs((part1 << 16) | part2);
        
        //Generate Pokemon ID
        int combinedIDs = _trainerID;
        combinedIDs <<= 16;
        combinedIDs += _secretID;
        long pkmID = ((long)combinedIDs << 32) | newPokemon.personalityValue;
        newPokemon.pokemonID =  math.abs(pkmID);
       
        //Determine if Pokemon Shiny
        var pv = newPokemon.personalityValue; 
        ushort tid = _trainerID;
        ushort sid = _secretID;
        ushort pvLow = (ushort)(pv & 0xFFFF);
        ushort pvHigh = (ushort)(pv >> 16);
        int shinyValue = tid ^ sid ^ pvLow ^ pvHigh;
        newPokemon.isShiny =  shinyValue < 8;
       
        //Assign Pokemon Gender
        if (newPokemon.hasGender)
        {
            uint genderValue = 0;
            if(newPokemon.personalityValue>0)
                genderValue = newPokemon.personalityValue % 256;
            var genderThreshold = math.abs((int)math.trunc(((newPokemon.ratioFemale / 100) * 256)));
            newPokemon.gender = (genderValue < genderThreshold)? Gender.Male : Gender.Female;
        }else newPokemon.gender = Gender.None;
        
        //Assign Pokemon Ability
        newPokemon.ability = null;
        
        var abilityEnum = newPokemon.abilities.Length > 1?
            newPokemon.abilities[newPokemon.personalityValue % 2] : newPokemon.abilities[0];
        
        var abilityName = NameDB.GetAbility(abilityEnum);
        newPokemon.ability = Resources.Load<Ability>(
            DirectoryHandler.GetDirectory(AssetDirectory.Abilities)
            + abilityName.ToLower());
        
        //Assign Pokemon Nature
        uint natureValue = 0;
        if (newPokemon.personalityValue > 0)
        {
            natureValue = newPokemon.personalityValue % 25;
        }
        
        var natures = NameDB.PokemonNatures;
        foreach (var nature in natures)
        {
            var assignedNature = Resources.Load<Nature>(
                DirectoryHandler.GetDirectory(AssetDirectory.Natures)
                + nature);
            if (assignedNature.requiredNatureValue != natureValue) continue;
            newPokemon.nature = assignedNature;
            break;
        }
        
        //Generate Pokemon IVs
        newPokemon.hpIv =  Utility.RandomRange(0,32);
        newPokemon.attackIv = Utility.RandomRange(0,32);
        newPokemon.defenseIv =  Utility.RandomRange(0,32);
        newPokemon.specialAttackIv =  Utility.RandomRange(0,32);
        newPokemon.specialDefenseIv =  Utility.RandomRange(0,32);
        newPokemon.speedIv =  Utility.RandomRange(0,32);
    }
    public IEnumerator TryToCatchPokemon(Item pokeball)
    {
        _inputStateHandler.ResetGroupUi(InputStateGroup.Bag);
        
        _dialogueHandler.DisplayBattleInfo(" ");
        _inputStateHandler.AddPlaceHolderState();
        
        var isCaught = false;
        var wildPokemon = _wildPokemonHandler.participant.Pokemon;
        SoundManager.Play(SfxId.BallThrow);
        yield return StartCoroutine(_battleVisuals.DisplayPokemonThrow());
        var ballRate = pokeball.GetDynamicModule<EffectValueInfo>().effectValue;
        var bracket1 = (3 * wildPokemon.maxHp - 2 * wildPokemon.hp) / (3 * wildPokemon.maxHp);
        var catchValue = math.trunc(bracket1 * wildPokemon.catchRate * ballRate * 
                                    _battleOperations.GetCatchRateBonusFromStatus(wildPokemon.statusEffect));
        
        if (catchValue > 0)
        {
            if (_battleOperations.IsImmediateCatch(catchValue))
            {
                yield return StartCoroutine(_battleVisuals.DisplayPokemonCatch());
                isCaught = true;
            }
            else
            {
                float shakeProbability = 65536 / math.sqrt( math.sqrt(16711680/catchValue));
                for (int i = 0; i < 3; i++)
                {
                    yield return StartCoroutine(_battleVisuals.DisplayPokeballShake());
                    int rand = Utility.Random16Bit();
                    if (rand < (shakeProbability * (i + 1)))
                    {
                        yield return StartCoroutine(_battleVisuals.DisplayPokemonCatch());
                        isCaught = true;
                        break;
                    }
                }
            }
        }
        
        if (isCaught)
        {
            _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
            SoundManager.Play(UiId.Success);
            _dialogueHandler.DisplayBattleInfo("Well done "+wildPokemon.pokemonDisplayName+" has been caught");
            SoundManager.PlayMusic(MusicId.VictoryWild);
            _wildPokemonHandler.participant.DeactivateUI();
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonBattle);
            var rawName = wildPokemon.pokemonDisplayName.Replace("Foe ", "");
            wildPokemon.pokemonDisplayName = rawName;

            wildPokemon.captureInformation.levelCaptured = wildPokemon.currentLevel;
            wildPokemon.captureInformation.areaName = AreaManager.GetAreaName(_gameLoadingHandler.playerData.location);
            yield return _dialogueHandler.AwaitAllDialogue();
            
            var nickNameOperationComplete = false;
            SetupPokemonNaming(wildPokemon, (result) => nickNameOperationComplete = true);
            yield return new WaitUntil(()=> nickNameOperationComplete);
            
            wildPokemon.ChangeFriendshipLevel(70);
            wildPokemon.pokeballName = pokeball.itemName;
            _playerParty.AddMemberAfterCatch(wildPokemon,pokeball.itemName);
            yield return _dialogueHandler.AwaitAllDialogue();
            yield return _wildPokemonHandler.EndWildBattle(BattleEndState.PokemonWasCaught);

        }else
        {
            yield return StartCoroutine(_battleVisuals.DisplayPokeballEscape());
            _dialogueHandler.DisplayBattleInfo(wildPokemon.pokemonDisplayName+" escaped the pokeball");
            yield return _dialogueHandler.AwaitAllDialogue();
            
            //Prevent player action until wild pokemon turn
            _turnBasedCombatHandler.OnTurnsCompleted += RemoveAfterPokeballFailure;
            void RemoveAfterPokeballFailure()
            {
                _turnBasedCombatHandler.OnTurnsCompleted -= RemoveAfterPokeballFailure;
                _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
            }
        }
        OnPokeballUsed?.Invoke(wildPokemon,isCaught);
    }

    public void SetupPokemonNaming(Pokemon pokemon, Action<bool> callBack)
    {
        _dialogueHandler.DisplayCustomOptions(
            $"Give a nickname to {pokemon.pokemonDisplayName}?", new[] { "Yes", "No" }
            ,new Action[]
            {
                SetupNickNameView, () =>  callBack?.Invoke(false)
            });
        return;    
        void SetupNickNameView()
        {
            void SetPokemonNickName(string nickName)
            {
                pokemon.nickName = nickName;
                callBack?.Invoke(true);
            }
            var maxPokemonNameLength = 12;
            _gameUiHandler.ViewTypingInterface(SetPokemonNickName,
                maxPokemonNameLength,
                new TypingInterfaceGraphicData(
                    false,
                    new() {pokemon.partyFrame1},
                    $"{pokemon.pokemonDisplayName}'s nickname?",
                    new Vector2(80,80),
                    pokemon.gender));
        }
    }
    /// <summary>
    /// for single pokemon creation only
    /// </summary>
    public void CreateSpecificPokemon(Action<Pokemon> creationCallBack,Pokemon template,int desiredLevel,int evolutionStage)
    {
        StartCoroutine(HandlePokemonCreation(creationCallBack,template,desiredLevel,evolutionStage));
    }
/// <summary>
/// for bulk async creation in a coroutine
/// </summary>
    public IEnumerator HandlePokemonCreation(Action<Pokemon> creationCallBack,Pokemon template,int desiredLevel,int evolutionStage)
    {
        var newPokemon = InstanceFactory.CreatePokemon(template); 
        SetPokemonTraits(newPokemon);
        if (evolutionStage > 0)
        {
            if (evolutionStage > newPokemon.evolutions.Count)
                Debug.LogError("Evolution number in encounter data is out of range of available evolutions");
            else
            {//quiet evolution
                newPokemon.Evolve(newPokemon.evolutions[evolutionStage - 1]);
            }
        }
        newPokemon.pokemonDisplayName = newPokemon.pokemonName;
        
        var expForRequiredLevel = CalculateExpForLevel(desiredLevel, newPokemon.expGroup);
        var newState = new PokemonStateHandler(_container)
        {
            pokemon = newPokemon,
            canEvolve = false//prevent evolution from artificial exp
        };
        yield return newState.ReceiveExperienceOutsideBattle(expForRequiredLevel,false);
        
        newState.canEvolve = true;
        
        newPokemon.hp = newPokemon.maxHp;
     
        creationCallBack.Invoke(newPokemon);
    }
    public IEnumerator HandlePokemonEvolution(Pokemon pokemon, int evolutionIndex)
    {
        _inputStateHandler.AddPlaceHolderState();
        yield return _gameUiHandler.FadeInBlackScreen();
        _gameUiHandler.RemoveColorScreen();
        
        evolutionImage.sprite = pokemon.frontPicture;
        
        evolutionUi.SetActive(true);
        
        _dialogueHandler.DisplayTextOnly("What? "+pokemon.pokemonDisplayName+" is evolving!",false);
        StartCoroutine(Utility.PokemonIntroAnimation(evolutionImage, pokemon));
        
        if (_battleHandler.BattleInProgress)
        {
            yield return _dialogueHandler.AwaitAllDialogue();
        }
        else
        {
            yield return _dialogueHandler.WaitForDialogueCompletion();
        }
        yield return Utility.FadeImage(evolutionBackground,Color.white,new Color(255,255,255,0));
        
        evolutionImageMask.gameObject.SetActive(true);
        yield return Utility.FadeImage(evolutionImageMask,Color.white);
        
        var evoSprite = pokemon.evolutions[evolutionIndex].frontPicture;
        yield return Utility.PokemonEvolutionAnimation(evolutionImage, pokemon.frontPicture, evoSprite);

        var previousName = pokemon.pokemonDisplayName;
        
        pokemon.Evolve(pokemon.evolutions[evolutionIndex]);
        
        StartCoroutine(Utility.FadeImage(evolutionImageMask,Color.white,new Color(255,255,255,0)));
        yield return _gameUiHandler.FadeInColorScreen(Color.white);
        evolutionImageMask.gameObject.SetActive(false);
        _gameUiHandler.RemoveColorScreen();
        
        StartCoroutine(Utility.PokemonIntroAnimation(evolutionImage, pokemon));
        if (_battleHandler.BattleInProgress)
        {
            _dialogueHandler.DisplayBattleInfo($"Congratulations! Your {previousName} evolved into {pokemon.pokemonName}");
        }
        else
        {
            _dialogueHandler.DisplayTextOnly($"Congratulations! Your {previousName} evolved into {pokemon.pokemonName}",
                false);
        }
        
        if (_battleHandler.BattleInProgress)
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            if(player.isActive)player.pokemonImage.sprite = pokemon.backPicture;
            var partner = _battleHandler.GetParticipant(BattleParticipantKey.PlayerPartner);
            if(partner.isActive)partner.pokemonImage.sprite = pokemon.backPicture;
        }
        
        if (_battleHandler.BattleInProgress)
        {
            yield return _dialogueHandler.AwaitAllDialogue();
        }
        else
        {
            yield return _dialogueHandler.WaitForDialogueCompletion();
            yield return new WaitForSecondsRealtime(1f);
        }
        
        _inputStateHandler.ResetSpecificUi(InputStateName.PlaceHolder);
        yield return _gameUiHandler.FadeInBlackScreen();
        _dialogueHandler.EndDialogue();
        evolutionUi.SetActive(false);
        evolutionImageMask.gameObject.SetActive(false);
        _gameUiHandler.RemoveColorScreen();
    }

    public static void ResolvePokemonNickName(Pokemon newPokemon)
    {
        var invalidNickname = string.IsNullOrEmpty(newPokemon.nickName) ||
                              string.IsNullOrWhiteSpace(newPokemon.nickName);
        newPokemon.nickName = invalidNickname? newPokemon.pokemonName : newPokemon.nickName;
    }
    
    public static void UpdatePokemonDisplayName(Pokemon newPokemon,string previousName)
    {
        var noNickname = newPokemon.pokemonDisplayName == previousName;
        if (noNickname)
        {
            newPokemon.pokemonDisplayName = newPokemon.pokemonName;
            newPokemon.nickName = newPokemon.pokemonName;
        }
    }
    

}







