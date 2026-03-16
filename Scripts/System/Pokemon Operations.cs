using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;
public enum FriendshipModifier{Fainted,LevelUp,Vitamin,Berry} 
public enum StatusEffect{None,Paralysis,Burn,Poison,BadlyPoison,Freeze,Sleep,FullHeal}
public enum Gender{None,Male,Female}
public enum Types
{
    Normal, Fire, Water, Electric, Grass, Ice,
    Fighting, Poison, Ground, Flying, Psychic,
    Bug, Rock, Ghost, Dragon, Dark, Steel
}
public enum Stat{None,Attack,Defense,SpecialAttack,SpecialDefense,Speed,Hp,Crit,Accuracy,Evasion,Multi}
public enum ExpGroup{Erratic,Fast,MediumFast,MediumSlow,Slow,Fluctuating}

public class PokemonOperations : MonoBehaviour,IInjectable
{
    public bool LearningNewMove;
    public bool SelectingMoveReplacement;
    public Pokemon currentPokemon { get; private set; }
    public Move NewMoveAsset;
    public static Action<bool> OnEvChange;
    public static PokemonOperations Instance;
    public event Action<Pokemon,bool> OnPokeballUsed;
    private Wild_pkm _wildPokemonHandler;
    private Dialogue_handler _dialogueHandler;
    private Item_handler _itemHandler;
    private Pokemon_party _playerParty;
    private InputStateHandler _inputStateHandler;
    private Game_Load _gameHandler;
    private BattleVisuals _battleVisuals;
    private Pokemon_Details _pokemonDetailsHandler;

    
    public void Inject(Container container)
    {
        _wildPokemonHandler = container.Resolve<Wild_pkm>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _itemHandler = container.Resolve<Item_handler>();
        _playerParty = container.Resolve<Pokemon_party>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _battleVisuals = container.Resolve<BattleVisuals>();
        _gameHandler=container.Resolve<Game_Load>();
        _pokemonDetailsHandler=container.Resolve<Pokemon_Details>();
        gameObject.SetActive(true);
    }   
    private void Awake()
     {
         if (Instance != null && Instance != this)
         {
             Destroy(gameObject);
             return;
         }
         Instance = this;
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
    private static void AssignPokemonAbility(Pokemon pokemon)
    { 
        pokemon.ability = null;
        var abilityEnum = (pokemon.abilities.Length > 1)? 
             pokemon.abilities[pokemon.personalityValue % 2] : pokemon.abilities[0];
        pokemon.abilityName = NameDB.GetAbility(abilityEnum);
        pokemon.ability = Resources.Load<Ability>(
            Save_manager.GetDirectory(AssetDirectory.Abilities)
            + pokemon.abilityName.ToLower());
    }
    public static bool ContainsType(Types[]typesList ,Type typesToCheck)
    {
        foreach (var type in typesList)
            if (type.ToString() == typesToCheck.typeName)
                return true;
        return false;
    }
    private static void AssignPokemonNature(Pokemon pokemon)
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
                Save_manager.GetDirectory(AssetDirectory.Natures)
                + nature);
            if (assignedNature.requiredNatureValue != natureValue) continue;
            pokemon.nature = assignedNature;
            break;
        }
    }
    public static int CalculateExpForNextLevel(int currentLevel, ExpGroup expGroup)
    {
        if (currentLevel == 0) return 0;
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

    public static ref float GetEvStatRef(Stat stat, Pokemon pkm)
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
    public static void CalculateEvForStat(Stat stat,float evAmount,Pokemon pkm)
    {
        ref float evRef = ref GetEvStatRef(stat, pkm);
        evRef = CheckEvLimit(evRef,evAmount,pkm);
    }
    static float CheckEvLimit(float currentEv,float amount,Pokemon pokemon)
    {
        var totalEv = pokemon.hpEv + pokemon.attackEv + pokemon.defenseEv +
                        pokemon.specialAttackEv + pokemon.specialDefenseEv + pokemon.speedEv;

        // Prevent over-adding if already at cap
        if (amount > 0 && (currentEv >= 252 || totalEv >= 510))
        {
            OnEvChange?.Invoke(false);
            return currentEv;
        }

        // Calculate clamped new EV
        float maxAssignable = Math.Min(252 - currentEv, 510 - totalEv);
        float clampedAmount = Math.Clamp(amount, -currentEv, maxAssignable);
        bool changed = clampedAmount != 0;

        OnEvChange?.Invoke(changed);
        
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
        if (LearningNewMove)
        {
            if (maxNumMoves)
            {
                yield return new WaitUntil(() => SelectingMoveReplacement);
                yield return new WaitUntil(() => !SelectingMoveReplacement);
            }
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        yield return new WaitUntil(() => !LearningNewMove);
    }
    public void CheckForNewMove(Pokemon pokemon)
    {
        LearningNewMove = true;
        currentPokemon = pokemon;
        if (currentPokemon.currentLevel > currentPokemon.learnSet[^1].requiredLevel)
        {//No more moves to learn
            LearningNewMove = false;
            return;
        }
        StartCoroutine(HandleMoveLearning());
    }
    public IEnumerator WaitForNewMoveCheck(Pokemon pokemon)
    {
        LearningNewMove = true;
        currentPokemon = pokemon;
        if (currentPokemon.currentLevel > currentPokemon.learnSet[^1].requiredLevel)
        {//No more moves to learn
            LearningNewMove = false;
            yield break;
        }
        yield return HandleMoveLearning();
    }
    private IEnumerator HandleMoveLearning()
    {
        var isPartyPokemon = _playerParty.party.Contains(currentPokemon);
        foreach (var move in currentPokemon.learnSet)
        {
            if (currentPokemon.currentLevel < move.requiredLevel)
                break;
            if (currentPokemon.currentLevel > move.requiredLevel)
                continue;
            LearningNewMove = true;
            var moveName = move.GetName().ToLower();
            yield return LearnMove(moveName, isPartyPokemon);
        }
        if(!SelectingMoveReplacement)
            LearningNewMove = false;
    }
    private IEnumerator LearnMove(string moveName,bool isPartyPokemon = true, bool isLevelUpMove = true)
    {
        _itemHandler.usingItem = false;
        var assetPath = Save_manager.GetDirectory(AssetDirectory.Moves) + moveName;
        
        var moveFromAsset = Resources.Load<Move>(assetPath);
        
        var newMove = Obj_Instance.CreateMove(moveFromAsset);
        
        if (currentPokemon.moveSet.Any(move=> move.moveName == newMove.moveName))
        {
            if (isPartyPokemon && !isLevelUpMove)
            {
                _dialogueHandler.DisplayBattleInfo(
                    $"{currentPokemon.pokemonName} already knows {moveName}", true);
            }
            yield return new WaitForSeconds(2f);
            yield break;
        }
        
        if (currentPokemon.moveSet.Count == 4) 
        {
            if (isPartyPokemon)
            {
                SelectingMoveReplacement = true;
                _dialogueHandler.DisplayList(
                    $"{currentPokemon.pokemonName} is trying to learn {moveName} ,do you want it to learn" +
                    $" {moveName}?", "", new[] { InteractionOptions.LearnMove
                        , InteractionOptions.SkipMove},
                    new[] { "Yes", "No" });
                
                NewMoveAsset = moveFromAsset;
                yield return new WaitUntil(()=>!LearningNewMove);
                yield return new WaitForSeconds(2f);
            }
            else
            {//wild pokemon get generated with somewhat random moveset choices
                currentPokemon.moveSet[Utility.RandomRange(0, 4)]
                    = Obj_Instance.CreateMove(moveFromAsset);
            }
        }
        else
        {
            if (isPartyPokemon)
            {
                _dialogueHandler.DisplayBattleInfo(
                    $"{currentPokemon.pokemonName} learned {moveName}",true);
                yield return new WaitForSeconds(2f);
            }
            currentPokemon.moveSet.Add(newMove);
        }
    }
    public IEnumerator LearnTmOrHm(AdditionalInfoModule infoModule, Pokemon pokemon)
    {
        currentPokemon = pokemon;
        switch (infoModule)
        {
            case TM tm:
                if (currentPokemon.learnableTms.Contains(tm.TmName))
                    yield return LearnMove(tm.move.moveName,isLevelUpMove:false);
                else
                    _dialogueHandler.DisplayDetails(pokemon.pokemonName+" cant learn that!");
                break;
            case HM hm:
                if (currentPokemon.learnableHms.Contains(hm.HmName))
                    yield return LearnMove(hm.move.moveName,isLevelUpMove:false);      
                else
                    _dialogueHandler.DisplayDetails(pokemon.pokemonName+" cant learn that!");
                break;
        }
    }
    public void LearnSelectedMove(int moveIndex)
    {
        _pokemonDetailsHandler.OnMoveSelected -= LearnSelectedMove;
        _pokemonDetailsHandler.learningMove = false;
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
        _dialogueHandler.DisplayBattleInfo(currentPokemon.pokemonName + " forgot " 
            + currentPokemon.moveSet[moveIndex].moveName 
            + " and learned " + NewMoveAsset.moveName,false);
        currentPokemon.moveSet[moveIndex] = Obj_Instance.CreateMove(NewMoveAsset);
        SelectingMoveReplacement = false;
        LearningNewMove = false;
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
        var wildPokemon = _wildPokemonHandler.participant.pokemon;//pokemon only caught in wild
        yield return StartCoroutine(_battleVisuals.DisplayPokemonThrow());
        var ballRate = float.Parse(pokeball.itemEffect);
        var bracket1 = (3 * wildPokemon.maxHp - 2 * wildPokemon.hp) / (3 * wildPokemon.maxHp);
        var catchValue = math.trunc(bracket1 * wildPokemon.catchRate * ballRate * 
                                      BattleOperations.GetCatchRateBonusFromStatus(wildPokemon.statusEffect));

        if (BattleOperations.IsImmediateCatch(catchValue))
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
            _dialogueHandler.DisplayBattleInfo("Well done "+wildPokemon.pokemonName+" has been caught");
            var rawName = wildPokemon.pokemonName.Replace("Foe ", "");
            wildPokemon.pokemonName = rawName;
            wildPokemon.ChangeFriendshipLevel(70);
            wildPokemon.pokeballName = pokeball.itemName;
            _playerParty.AddMember(wildPokemon,pokeball.itemName);
            yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
            _wildPokemonHandler.participant.EndWildBattle();
        }else
        {
            yield return StartCoroutine(_battleVisuals.DisplayPokeballEscape());
            _dialogueHandler.DisplayBattleInfo(wildPokemon.pokemonName+" escaped the pokeball");
            yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
        }
        OnPokeballUsed?.Invoke(wildPokemon,isCaught);
    }
}







