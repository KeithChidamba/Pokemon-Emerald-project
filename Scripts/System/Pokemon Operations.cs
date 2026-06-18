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
public enum StatusEffect{None,Paralysis,Burn,Poison,BadlyPoison,Freeze,Sleep,FullHeal}
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
    private bool _learningNewMove;
    private bool _selectingMoveReplacement;
    private Pokemon _currentPokemon;
    private Move _newMoveAsset;
    public event Action<Stat,bool> OnEvChange;
    public event Action<Pokemon,bool> OnPokeballUsed;
    
    private WildPokemonAiHandler _wildPokemonHandler;
    private Item_handler _itemHandler;
    private Pokemon_party _playerParty;
    private InputStateHandler _inputStateHandler;
    private Game_Load _gameHandler;
    private BattleVisuals _battleVisuals;
    private Pokemon_Details _pokemonDetailsHandler;
    private Dialogue_handler _dialogueHandler;
    private Game_ui_manager _gameUIManager;
    private Game_Load _gameLoadingHandler;
    private BattleOperations _battleOperations;
    
    public void Inject(ServiceContainer container)
    {
        _wildPokemonHandler = container.Resolve<WildPokemonAiHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _itemHandler = container.Resolve<Item_handler>();
        _playerParty = container.Resolve<Pokemon_party>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _battleVisuals = container.Resolve<BattleVisuals>();
        _gameHandler=container.Resolve<Game_Load>();
        _pokemonDetailsHandler=container.Resolve<Pokemon_Details>();
        _gameUIManager = container.Resolve<Game_ui_manager>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _battleOperations = container.Resolve<BattleOperations>();
        
        gameObject.SetActive(true);
    }   

    public void OnInject()
    {
        
    }
    private long GeneratePokemonID(Pokemon pokemon)//pokemon's unique ID
    {
        int combinedIDs = _gameHandler.playerData.trainerID;
        combinedIDs <<= 16;
        combinedIDs += _gameHandler.playerData.secretID;
        long pkmID = (((long)combinedIDs)<<32) | pokemon.personalityValue;
        return math.abs(pkmID);
    }
    private uint GeneratePersonalityValue()
    {
        System.Random rand = new System.Random();
        int part1 = rand.Next(0, 65536); // 16-bit random value
        int part2 = rand.Next(0, 65536); // 16-bit random value
        return (uint)math.abs((part1 << 16) | part2);
    }
    private void AssignPokemonAbility(Pokemon pokemon)
    { 
        pokemon.ability = null;
        var abilityEnum = (pokemon.abilities.Length > 1)? 
             pokemon.abilities[pokemon.personalityValue % 2] : pokemon.abilities[0];
        pokemon.abilityName = NameDB.GetAbility(abilityEnum);
        pokemon.ability = Resources.Load<Ability>(
            DirectoryHandler.GetDirectory(AssetDirectory.Abilities)
            + pokemon.abilityName.ToLower());
    }
    public bool ContainsType(PokemonType[]typesList ,Type typesToCheck)
    {
        foreach (var type in typesList)
            if (type == typesToCheck.typeEnum)
                return true;
        return false;
    }
    private void AssignPokemonNature(Pokemon pokemon)
    {
        uint natureValue = 0;
        if (pokemon.personalityValue > 0)
            natureValue = pokemon.personalityValue % 25;
        string[] natures =
        {
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        };
        foreach (var nature in natures)
        {
            var assignedNature = Resources.Load<Nature>(
                DirectoryHandler.GetDirectory(AssetDirectory.Natures)
                + nature);
            if (assignedNature.requiredNatureValue != natureValue) continue;
            pokemon.nature = assignedNature;
            break;
        }
    }
    public int CalculateExpForLevel(int desiredLevel, ExpGroup expGroup)
    {
        if (desiredLevel == 0)
        {
            Debug.LogError("cant level up to 0");
            return 0;
        }
        return CalculateExpForNextLevel(desiredLevel - 1,expGroup)+1;
    }
    public int CalculateExpForNextLevel(int currentLevel, ExpGroup expGroup)
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
    private static void GeneratePokemonIVs(Pokemon pokemon)
    {
        pokemon.hpIv =  Utility.RandomRange(0,32);
        pokemon.attackIv = Utility.RandomRange(0,32);
        pokemon.defenseIv =  Utility.RandomRange(0,32);
        pokemon.specialAttackIv =  Utility.RandomRange(0,32);
        pokemon.specialDefenseIv =  Utility.RandomRange(0,32);
        pokemon.speedIv =  Utility.RandomRange(0,32);
    }

    public IEnumerator AwaitMoveOperation(bool maxNumMoves)
    {
        if (_learningNewMove)
        {
            if (maxNumMoves)
            {
                yield return new WaitUntil(() => _selectingMoveReplacement);
                yield return new WaitUntil(() => !_selectingMoveReplacement);
            }
            yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        }
        yield return new WaitUntil(() => !_learningNewMove);
    }
    public IEnumerator WaitForNewMoveCheck(Pokemon pokemon)
    {
        _learningNewMove = true;
        _currentPokemon = pokemon;
        if (_currentPokemon.currentLevel > _currentPokemon.learnSet[^1].requiredLevel)
        {//No more moves to learn
            _learningNewMove = false;
            yield break;
        }
        yield return HandleMoveLearning();
    }
    private IEnumerator HandleMoveLearning()
    {
        var isPartyPokemon = _playerParty.party.Contains(_currentPokemon);
        foreach (var move in _currentPokemon.learnSet)
        {
            if (_currentPokemon.currentLevel < move.requiredLevel)
                break;
            if (_currentPokemon.currentLevel > move.requiredLevel)
                continue;
            _learningNewMove = true;
            var moveName = move.GetName().ToLower();
            yield return LearnMove(moveName, isPartyPokemon);
        }
        if(!_selectingMoveReplacement)
            _learningNewMove = false;
    }
    private IEnumerator LearnMove(string moveName,bool isPartyPokemon = true, bool isLevelUpMove = true)
    {
        var assetPath = DirectoryHandler.GetDirectory(AssetDirectory.Moves) + moveName;
        
        var moveFromAsset = Resources.Load<Move>(assetPath);
        
        var newMove = InstanceFactory.CreateMove(moveFromAsset);
        
        if (_currentPokemon.moveSet.Any(move=> move.moveName == newMove.moveName))
        {
            if (isPartyPokemon && !isLevelUpMove)
            {
                _dialogueHandler.DisplayBattleInfo(
                    $"{_currentPokemon.pokemonDisplayName} already knows {moveName}", true);
            }
            yield return new WaitForSecondsRealtime(2f);
            yield break;
        }
        
        if (_currentPokemon.moveSet.Count == 4) 
        {
            if (isPartyPokemon)
            {
                _selectingMoveReplacement = true;
                
                _dialogueHandler.DisplayCustomOptions(
                    $"{_currentPokemon.pokemonDisplayName} is trying to learn {moveName} ,do you want it to learn" +
                    $" {moveName}?", new[] { "Yes", "No" }
                    ,new Action[] { LearnMove, SkipMove });
                
                _newMoveAsset = moveFromAsset;
                yield return new WaitUntil(()=>!_learningNewMove);
                yield return new WaitForSecondsRealtime(2f);
            }
            else
            {//wild pokemon get generated with somewhat random moveset choices
                _currentPokemon.moveSet[Utility.RandomRange(0, 4)]
                    = InstanceFactory.CreateMove(moveFromAsset);
            }
        }
        else
        {
            if (isPartyPokemon)
            {
                _dialogueHandler.DisplayBattleInfo(
                    $"{_currentPokemon.pokemonDisplayName} learned {moveName}",true);
                yield return new WaitForSecondsRealtime(2f);
            }
            _currentPokemon.moveSet.Add(newMove);
        }
    }
    private void LearnMove()
    {        
        _pokemonDetailsHandler.learningMove = true;
        _pokemonDetailsHandler.OnMoveSelected += LearnSelectedMove;
        _dialogueHandler.DisplayBattleInfo("Which move will you replace?",false);
        _gameUIManager.ViewPartyPokemonDetails(_currentPokemon);
    }
    public void SkipMove()
    {
        _dialogueHandler.DeletePreviousOptions();
        _pokemonDetailsHandler.OnMoveSelected = null;
        _selectingMoveReplacement = false;
        _learningNewMove = false;
        _pokemonDetailsHandler.learningMove = false;
        _dialogueHandler.DisplayBattleInfo(_currentPokemon.pokemonDisplayName +
                                           " did not learn "+_newMoveAsset.moveName,false);
    }
    public IEnumerator LearnTmOrHm(Item item, Pokemon pokemon)
    {
        _currentPokemon = pokemon;
        foreach (var infoModule in item.additionalInfoModules)
        {
            switch (infoModule)
            {
                case TM tm:
                {
                    if (_currentPokemon.learnableTms.Contains(tm.TmName))
                        yield return LearnMove(tm.move.moveName, isLevelUpMove: false);
                    else
                        _dialogueHandler.DisplayDetails(pokemon.pokemonDisplayName + " cant learn that!");
                } break;
                case HM hm:
                {
                    if (_currentPokemon.learnableHms.Contains(hm.HmName))
                        yield return LearnMove(hm.move.moveName, isLevelUpMove: false);
                    else
                        _dialogueHandler.DisplayDetails(pokemon.pokemonDisplayName + " cant learn that!");
                } break;
            }
        }
    }
    private void LearnSelectedMove(int moveIndex)
    {
        _pokemonDetailsHandler.OnMoveSelected -= LearnSelectedMove;
        _pokemonDetailsHandler.learningMove = false;
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
        _dialogueHandler.DisplayBattleInfo(_currentPokemon.pokemonDisplayName + " forgot " 
            + _currentPokemon.moveSet[moveIndex].moveName 
            + " and learned " + _newMoveAsset.moveName,false);
        _currentPokemon.moveSet[moveIndex] = InstanceFactory.CreateMove(_newMoveAsset);
        _selectingMoveReplacement = false;
        _learningNewMove = false;
    }
    public static void UpdateHealthPhase(Pokemon pokemon,RawImage hpSliderColor)
    {
        List<(float threshold, Color color, int phase)> healthPhases = new()
        {
            (0.2f, Color.red, 3),
            (0.5f, Color.yellow, 2),
            (1f, Color.green, 1),
        };

        var hpPercentage = pokemon.hp / pokemon.maxHp;
        var phase = healthPhases.FirstOrDefault(hp => hpPercentage <= hp.threshold);

        hpSliderColor.color = phase.color;
        pokemon.healthPhase = phase.phase;
    }
    private static void AssignPokemonGender(Pokemon pokemon)
    {
        uint genderValue = 0;
        if(pokemon.personalityValue>0)
            genderValue = pokemon.personalityValue % 256;
        var genderThreshold = math.abs((int)math.trunc(((pokemon.ratioFemale / 100) * 256)));
        pokemon.gender = (genderValue < genderThreshold)? Gender.Male : Gender.Female;
    }

    private bool IsShiny(uint pv)
    {
        ushort tid = _gameHandler.playerData.trainerID;
        ushort sid = _gameHandler.playerData.secretID;
        
        ushort pvLow = (ushort)(pv & 0xFFFF);
        ushort pvHigh = (ushort)(pv >> 16);

        int shinyValue = tid ^ sid ^ pvLow ^ pvHigh;

        return shinyValue < 8;
    }
    public void SetPokemonTraits(Pokemon newPokemon)
    {
        newPokemon.personalityValue = GeneratePersonalityValue();
        newPokemon.pokemonID = GeneratePokemonID(newPokemon);
        newPokemon.isShiny = IsShiny(newPokemon.personalityValue); 
        if (newPokemon.hasGender)
            AssignPokemonGender(newPokemon);
        else
            newPokemon.gender = Gender.None;
        
        AssignPokemonAbility(newPokemon);
        AssignPokemonNature(newPokemon);
        GeneratePokemonIVs(newPokemon);
    }
    public IEnumerator TryToCatchPokemon(Item pokeball)
    {
        _inputStateHandler.ResetGroupUi(InputStateGroup.Bag);
        var isCaught = false;
        var wildPokemon = _wildPokemonHandler.participant.pokemon;
        yield return StartCoroutine(_battleVisuals.DisplayPokemonThrow());
        var ballRate = pokeball.GetDynamicModule<ItemEffectInfo>().effectValue;
        var bracket1 = (3 * wildPokemon.maxHp - 2 * wildPokemon.hp) / (3 * wildPokemon.maxHp);
        var catchValue = math.trunc(bracket1 * wildPokemon.catchRate * ballRate * 
                                    _battleOperations.GetCatchRateBonusFromStatus(wildPokemon.statusEffect));

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
        if (isCaught)
        {
            _dialogueHandler.DisplayBattleInfo("Well done "+wildPokemon.pokemonDisplayName+" has been caught");
            
            _wildPokemonHandler.participant.DeactivateUI();
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonBattle);
            var rawName = wildPokemon.pokemonDisplayName.Replace("Foe ", "");
            wildPokemon.pokemonDisplayName = rawName;

            wildPokemon.captureInformation.levelCaptured = wildPokemon.currentLevel;
            wildPokemon.captureInformation.areaName = Utility.GetAreaName(_gameLoadingHandler.playerData.location);
            yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
            
            var nickNameOperationComplete = false;
            SetupPokemonNaming(wildPokemon, (result) => nickNameOperationComplete = true);
            yield return new WaitUntil(()=> nickNameOperationComplete);
            
            wildPokemon.ChangeFriendshipLevel(70);
            wildPokemon.pokeballName = pokeball.itemName;
            _playerParty.AddMember(wildPokemon,pokeball.itemName);
            yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
            yield return _wildPokemonHandler.EndWildBattle();

        }else
        {
            yield return StartCoroutine(_battleVisuals.DisplayPokeballEscape());
            _dialogueHandler.DisplayBattleInfo(wildPokemon.pokemonDisplayName+" escaped the pokeball");
            yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
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
            
        void SetupNickNameView()
        {
            void SetPokemonNickName(string nickName)
            {
                pokemon.nickName = nickName;
                callBack?.Invoke(true);
            }
            var maxPokemonNameLength = 12;
            _gameUIManager.ViewTypingInterface(SetPokemonNickName,
                maxPokemonNameLength,
                new TypingInterfaceGraphicData(
                    false,
                    new() {pokemon.partyFrame1},
                    $"{pokemon.pokemonDisplayName}'s nickname?",
                    new Vector2(80,80),
                    pokemon.gender));
        }
    }

    public void CreateSpecificPokemon(Action<Pokemon> creationCallBack,Pokemon template,int desiredLevel,int evolutionStage)
    {
        StartCoroutine(HandlePokemonCreation(creationCallBack,template,desiredLevel,evolutionStage));
    }
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
        newPokemon.canEvolve = false;//prevent evolution from exp

        yield return newPokemon.ReceiveExperienceOutsideBattle(expForRequiredLevel,false);
        newPokemon.hp = newPokemon.maxHp;
        creationCallBack.Invoke(newPokemon);
    }
    public IEnumerator HandlePokemonEvolution(Pokemon pokemon, int evolutionIndex)
    {
        _dialogueHandler.DisplayBattleInfo("What? "+pokemon.pokemonDisplayName+" is evolving!");
        var previousName = pokemon.pokemonDisplayName;
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
        pokemon.Evolve(pokemon.evolutions[evolutionIndex]);
        _dialogueHandler.DisplayBattleInfo(previousName+" evolved into "+pokemon.pokemonDisplayName);
        yield return new WaitUntil(() => !_dialogueHandler.messagesLoading);
    }
}







