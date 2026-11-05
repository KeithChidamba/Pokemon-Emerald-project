using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TournamentBracket
{
    public TournamentContestant firstContestant;
    public TournamentContestant secondContestant;
}

public enum BattleCompetitionDifficulty
{
    Easy,Hard,Crazy
}
public class BattleZoneHandler : MonoBehaviour,IInjectable
{
    public TMP_Text playerName;
    public List<TMP_Text> playerPartyNames = new();
    public List<Image> playerPartyImages = new ();
    public GameObject partyScreenUi;
    
    public TMP_Text playerNameVs;
    public List<Image> playerPartyImagesVs = new ();
    public TMP_Text enemyNameVs;
    public Image enemyImageVs;
    public List<Image> enemyPartyImagesVs = new ();
    public GameObject vsScreenUi;
    
    public BattleCompetitionDifficulty currentDifficulty;
    private AiFlags[] easyModeFlags =
    {
        AiFlags.CheckBadMove
    };
    private AiFlags[] hardModeFlags =
    {
        AiFlags.CheckBadMove,AiFlags.CheckViability,AiFlags.CheckPriority
    };
    private AiFlags[] crazyModeFlags =
    {
        AiFlags.CheckBadMove,AiFlags.CheckViability,AiFlags.CheckStatus,
        AiFlags.CheckSetup,AiFlags.CheckSwitching,AiFlags.CheckPriority
    };
    
    public BattleZoneContestantRegistry contestantRegistry;
    public TrainerData npcTrainer;
    public Interaction battleInteraction;
    public Vector3 npcIntroPosition;
    public Vector3 playerBattlePosition;
    private const int ContestantCount = 16;
    private const int PlayerPartyCount = 3;
    private int opponentLevelGap;
    private const int RequiredPokemonLevel = 65;
    private const float PartyScreenDuration = 4f;
    private const float VsScreenDuration = 3f;
    private const float VsFadeDuration = 0.5f;

    public List<TournamentBracket> leftBracket = new();
    public List<TournamentContestant> leftContestants = new();
    public List<TournamentBracket> rightBracket = new();
    public List<TournamentContestant> rightContestants = new();
    private TournamentContestant playerContestant;
    private List<Pokemon> playerSavedPokemon = new();
    private List<Item> playerSavedItems = new();
    private readonly List<GameObject> spawnedNpcInstances = new();
    private GameObject currentOpponentInstance;
    
    private Coroutine roundTransition;
    private Coroutine tournamentIntro;
    private int currentRound;
    
    private AreaManager _areaManager;
    private PokemonPartyHandler _partyHandler;
    private DialogueHandler _dialogueHandler;
    private PlayerMovementHandler _playerMovement;
    private GameLoadingHandler _gameLoader;
    private PokemonOperations _pokemonOperations;
    private BattleHandler _battleHandler;
    private GameUiHandler _gameUi;
    private PlayerBagHandler _bag;
    private SaveDataHandler _saveDataHandler;
    private ServiceContainer serviceContainer;
    
   public void Inject(ServiceContainer container) 
   { 
       serviceContainer = container;
       _partyHandler = container.Resolve<PokemonPartyHandler>();
       _pokemonOperations = container.Resolve<PokemonOperations>();
       _dialogueHandler = container.Resolve<DialogueHandler>();
       _areaManager = container.Resolve<AreaManager>();
       _battleHandler = container.Resolve<BattleHandler>();
       _gameLoader = container.Resolve<GameLoadingHandler>();
       _playerMovement = container.Resolve<PlayerMovementHandler>();
       _gameUi = container.Resolve<GameUiHandler>();
       _bag = container.Resolve<PlayerBagHandler>();
       _saveDataHandler = container.Resolve<SaveDataHandler>();
   }
   
   public void OnInject()
   {
      gameObject.SetActive(true);
      _battleHandler.OnBattleStarted += () => AlterExpGain(false);
      _battleHandler.OnBattleEnd += () => AlterExpGain(true);
   }
   
   private void AlterExpGain(bool canGain)
   {
       var participants = _battleHandler.GetParticipants;
       foreach (var participant in participants)
       {
           participant.pokemonState.canGainExp = canGain;
       }
   }

   public void SetDifficultyAndStart(BattleCompetitionDifficulty difficulty)
   {
       _dialogueHandler.EndDialogue();
       currentDifficulty = difficulty;
       npcTrainer.trainerAiFlags.Clear();
       switch (currentDifficulty)
       {
           case BattleCompetitionDifficulty.Easy:
               opponentLevelGap = 15;
               npcTrainer.trainerAiFlags = easyModeFlags.ToList();
               break;
           case BattleCompetitionDifficulty.Hard:
               opponentLevelGap = 8;
               npcTrainer.trainerAiFlags = hardModeFlags.ToList();
               break;
           case BattleCompetitionDifficulty.Crazy:
               opponentLevelGap = 0;
               npcTrainer.trainerAiFlags = crazyModeFlags.ToList();
               break;
       }
       StartCoroutine(BeginTournament());
   }
   private IEnumerator BeginTournament()
   {
       _gameUi.SetMenuAccessibility(false);
       _playerMovement.RestrictPlayerMovement(MovementRestrictor.OverworldAction);

       //save the real party and bag before anything is cleared for the tournament
       yield return _saveDataHandler.SaveAllData();

       _battleHandler.OnTrainerBattleResult += DetermineNextBracket;
       
       leftBracket.Clear();
       rightBracket.Clear();
       leftContestants.Clear();
       rightContestants.Clear();

       playerSavedPokemon.AddRange(_partyHandler.Party);
       _partyHandler.ClearPartyMembers();
       SavePlayerBag();
       currentRound = 1;

       var numBracketsPerColumn = ContestantCount / 4;
       var npcListCopy = new List<TournamentContestant>(contestantRegistry.contestants);
       
       playerContestant = new TournamentContestant
       {
           trainerName = _gameLoader.playerData.playerName,
           bracketIndex = 0
       };
       //give payer some pokemon from a preset pool
       var membersFromPool = contestantRegistry.playerPokemonPool;
       playerContestant.pokemonPartyData.Clear();
       for (int i = 0; i < PlayerPartyCount; i++)
       {
           var randomPartyIndex = Utility.RandomRange(0, membersFromPool.Count);
           
           while (playerContestant.pokemonPartyData.Contains(membersFromPool[randomPartyIndex]))
           {
               randomPartyIndex = Utility.RandomRange(0, membersFromPool.Count);
           }
           playerContestant.pokemonPartyData.Add(membersFromPool[randomPartyIndex]);
       }
       
       leftBracket.Add(new TournamentBracket
       {
           firstContestant = playerContestant,
           secondContestant = GetRandomContestant(0)
       });
  
       rightBracket.Add(new TournamentBracket
       {
           firstContestant = GetRandomContestant(0),
           secondContestant = GetRandomContestant(0)
       });
       
       for (int i = 1; i < numBracketsPerColumn; i++)
       {
           leftBracket.Add(new TournamentBracket
           {
               firstContestant = GetRandomContestant(i),
               secondContestant = GetRandomContestant(i)
           });
           rightBracket.Add(new TournamentBracket
           {
               firstContestant = GetRandomContestant(i),
               secondContestant = GetRandomContestant(i)
           });
       }
       leftContestants.AddRange(leftBracket.Select(b=>b.firstContestant));
       leftContestants.AddRange(leftBracket.Select(b=>b.secondContestant));
       
       rightContestants.AddRange(rightBracket.Select(b=>b.firstContestant));
       rightContestants.AddRange(rightBracket.Select(b=>b.secondContestant));
       
       TournamentContestant GetRandomContestant(int bracketIndex)
       {
           var random = Utility.RandomRange(0, npcListCopy.Count);
           var contestant = npcListCopy[random];
           npcListCopy.RemoveAt(random);
           contestant.bracketIndex = bracketIndex;
           return contestant;
       }
       
       tournamentIntro = StartCoroutine(TournamentIntroSequence());
   }

   private IEnumerator TournamentIntroSequence()
   {
       yield return _gameUi.FadeInBlackScreen();

       //move the player while the screen is black
       _playerMovement.SetPlayerPosition(playerBattlePosition);
       _playerMovement.FaceOppositeDirection(MovementDirection.Left);

       //wait for every pokemon to be fully created so the evolved forms are used
       yield return LoadPlayerPartyRoutine();

       yield return ShowPlayerPartyScreen();

       GiveRoundHelpItems();
       yield return ShowVsScreen(leftBracket.First());
       
       SoundManager.PlayMusic(MusicId.TrainerEyesMeetYoungster);
       yield return new WaitForSeconds(0.5f);
       
       tournamentIntro = null;
       BeginNewRound(leftBracket.First());
   }

   private IEnumerator LoadPlayerPartyRoutine()
   {
       foreach (var member in playerContestant.pokemonPartyData)
       {
           yield return _pokemonOperations.HandlePokemonCreation(GetPokemonCopy,
               new NaturalPokemonCreationData
               {
                   pokemon = member.pokemon,
                   evolutionStageNumber = member.evolutionStageNumber,
                   pokemonLevel = RequiredPokemonLevel,
                   splitEvolutionIndex = member.splitEvolutionIndex
               });
           continue;
           void GetPokemonCopy(Pokemon pokemonCopy)
           {
               PokemonOperations.ResolvePokemonNickName(pokemonCopy);
               pokemonCopy.moveSet.Clear();
               foreach (var move in member.moveSet)
               {
                   pokemonCopy.moveSet.Add(InstanceFactory.CreateMove(move));
               }
               _partyHandler.AddMember(pokemonCopy);
           }
       }
   }

   //screen is still black when this starts, the ui is filled before the black is removed
   private IEnumerator ShowPlayerPartyScreen()
   {
       var party = _partyHandler.Party;

       playerName.text = $"Pokemon trainer {_gameLoader.playerData.playerName}";
       SetTexts(playerPartyNames, party.Select(p => p.pokemonName));
       SetSprites(playerPartyImages, party.Select(p => p.partyFrame1));
       partyScreenUi.SetActive(true);

       _gameUi.RemoveColorScreen();
       yield return new WaitForSeconds(PartyScreenDuration);

       partyScreenUi.SetActive(false);
   }

   private IEnumerator ShowVsScreen(TournamentBracket bracket)
   {
       var opponent = bracket.secondContestant;

       yield return _gameUi.FadeInBlackScreen();

       //create the opponent's pokemon so their evolved forms are shown
       var opponentPokemon = new List<Pokemon>();
       yield return CreateOpponentPreviewPokemon(opponent, opponentPokemon);

       playerNameVs.text = _gameLoader.playerData.playerName;
       enemyNameVs.text = opponent.trainerName;
       enemyImageVs.sprite = opponent.battleIntroSprite;
       SetSprites(playerPartyImagesVs, _partyHandler.Party.Select(p => p.partyFrame1));
       SetSprites(enemyPartyImagesVs, opponentPokemon.Select(p => p.partyFrame1));

       var group = GetVsCanvasGroup();
       group.alpha = 1f;
       vsScreenUi.SetActive(true);

       _gameUi.RemoveColorScreen();
       yield return new WaitForSeconds(VsScreenDuration);

       for (var elapsed = 0f; elapsed < VsFadeDuration; elapsed += Time.deltaTime)
       {
           group.alpha = 1f - elapsed / VsFadeDuration;
           yield return null;
       }

       vsScreenUi.SetActive(false);
       group.alpha = 1f;
   }

   private IEnumerator CreateOpponentPreviewPokemon(TournamentContestant opponent, List<Pokemon> result)
   {
       foreach (var member in opponent.pokemonPartyData)
       {
           yield return _pokemonOperations.HandlePokemonCreation(result.Add,
               new NaturalPokemonCreationData
               {
                   pokemon = member.pokemon,
                   evolutionStageNumber = member.evolutionStageNumber,
                   pokemonLevel = RequiredPokemonLevel - opponentLevelGap,
                   splitEvolutionIndex = member.splitEvolutionIndex
               });
       }
   }

   private CanvasGroup GetVsCanvasGroup()
   {
       if (!vsScreenUi.TryGetComponent(out CanvasGroup group))
       {
           group = vsScreenUi.AddComponent<CanvasGroup>();
       }
       return group;
   }

   private static void SetSprites(List<Image> slots, IEnumerable<Sprite> sprites)
   {
       var spriteList = sprites.ToList();
       for (int i = 0; i < slots.Count; i++)
       {
           var hasSprite = i < spriteList.Count;
           slots[i].gameObject.SetActive(hasSprite);
           if (hasSprite) slots[i].sprite = spriteList[i];
       }
   }

   private static void SetTexts(List<TMP_Text> slots, IEnumerable<string> texts)
   {
       var textList = texts.ToList();
       for (int i = 0; i < slots.Count; i++)
       {
           var hasText = i < textList.Count;
           slots[i].gameObject.SetActive(hasText);
           if (hasText) slots[i].text = textList[i];
       }
   }
   
   private void DetermineNextBracket(bool battleWon, List<TrainerData> enemies)
   {
       if (roundTransition != null) return;
       roundTransition = StartCoroutine(ProcessRoundTransition(battleWon));
   }
   private IEnumerator ProcessRoundTransition(bool battleWon)
   {
       yield return AwaitBattleTeardown();
        
       SoundManager.StopMusic();
       
       DestroyCurrentOpponent();

       if (!battleWon)
       {
           //loss dialogue
           EndTournament(false);
           roundTransition = null;
           yield break;
       }

       var wasFinalRound = rightBracket.Count == 0;
       if (wasFinalRound)
       {
           //victory dialogue
           EndTournament(true);
           roundTransition = null;
           yield break;
       }

       _partyHandler.HealPartyPokemon();
       SetupNewBrackets();
       currentRound++;
       GiveRoundHelpItems();

       //between-round dialogue, anything yielded here holds the next round back

       yield return ShowVsScreen(leftBracket.First());

       roundTransition = null;
       
       SoundManager.PlayMusic(MusicId.TrainerEyesMeetYoungster);
       yield return new WaitForSeconds(0.5f);
       
       BeginNewRound(leftBracket.First());
   }

   private IEnumerator AwaitBattleTeardown()
   {
       yield return new WaitUntil(() => !_battleHandler.isBattleCompetition);
       yield return null;
   }
   
    private void SetupNewBrackets()
    {
        var leftWinners = GetBracketWinners(leftBracket);
        var rightWinners = GetBracketWinners(rightBracket);

        leftBracket.Clear();
        rightBracket.Clear();
        leftContestants.Clear();
        rightContestants.Clear();

        leftContestants.AddRange(leftWinners);
        rightContestants.AddRange(rightWinners);

        var isFinalRound = leftWinners.Count == 1;
        if (isFinalRound)
        {
            leftBracket.Add(new TournamentBracket
            {
                firstContestant = leftWinners[0],
                secondContestant = rightWinners[0]
            });
            return;
        }

        leftBracket.AddRange(PairWinnersIntoBrackets(leftWinners));
        rightBracket.AddRange(PairWinnersIntoBrackets(rightWinners));
    }

    private List<TournamentBracket> PairWinnersIntoBrackets(List<TournamentContestant> winners)
    {
        var newBrackets = new List<TournamentBracket>();
        for (int i = 0; i < winners.Count; i += 2)
        {
            var newBracketIndex = i / 2;
            winners[i].bracketIndex = newBracketIndex;
            winners[i + 1].bracketIndex = newBracketIndex;

            newBrackets.Add(new TournamentBracket
            {
                firstContestant = winners[i],
                secondContestant = winners[i + 1]
            });
        }
        return newBrackets;
    }

    private List<TournamentContestant> GetBracketWinners(List<TournamentBracket> brackets)
    {
        return brackets.Select(GetBracketWinner).ToList();
    }

    private TournamentContestant GetBracketWinner(TournamentBracket bracket)
    {
        if (bracket.firstContestant == playerContestant) return playerContestant;

        var firstWins = Utility.RandomRange(0, 2) == 0;
        return firstWins ? bracket.firstContestant : bracket.secondContestant;
    }

    private void EndTournament(bool playerWonTournament)
    {
        _gameUi.SetMenuAccessibility(true);
        _battleHandler.OnTrainerBattleResult -= DetermineNextBracket;
        
        RestorePlayerParty();
        RestorePlayerBag();
        ResetTournamentState();

        if (playerWonTournament)
        {
            //bag is reloaded above, so the reward limit checks see the player's real items
            var rewards = GetRewards();
            _dialogueHandler.DisplayDetails($"Well done, you won! You received: {string.Join(", ", rewards)}");
        }
        else
        {
            _dialogueHandler.DisplayBattleInfo("Try again next time!");
        }
        //spawn player outside battlezone, release movement restriction
        _areaManager.TeleportToArea(AreaName.BattleZone);
        _playerMovement.AllowPlayerMovement(MovementRestrictor.OverworldAction);
    }
    
    #region Rewards
    private static readonly ItemName[] TournamentTms =
    {
        ItemName.Tm06, ItemName.Tm08, ItemName.Tm09, ItemName.Tm13, ItemName.Tm15,
        ItemName.Tm18, ItemName.Tm19, ItemName.Tm24, ItemName.Tm25, ItemName.Tm26,
        ItemName.Tm28, ItemName.Tm31, ItemName.Tm35, ItemName.Tm37, ItemName.Tm40
    };
    private static readonly ItemName[] TournamentVitamins =
    {
        ItemName.HpUp, ItemName.Protein, ItemName.Iron,
        ItemName.Carbos, ItemName.Calcium, ItemName.Zinc,
        ItemName.PpUp,ItemName.PpMax
    };

    //reward pool per difficulty
    private static readonly Dictionary<BattleCompetitionDifficulty, (ItemName[] Major, ItemName[] Minor)> RewardPools = new()
    {
        {
            BattleCompetitionDifficulty.Easy,
            (new[] { ItemName.GoodRod, ItemName.ExpShare },
             TournamentTms.Take(7).ToArray())
        },
        {
            BattleCompetitionDifficulty.Hard,
            (new[] { ItemName.GoodRod, ItemName.SuperRod, ItemName.ExpShare, ItemName.LuckyEgg },
             TournamentTms)
        },
        {
            BattleCompetitionDifficulty.Crazy,
            (new[] { ItemName.GoodRod, ItemName.SuperRod, ItemName.ExpShare, ItemName.LuckyEgg },
             TournamentTms.Concat(TournamentVitamins).ToArray())
        }
    };

    //how many of each major prize the player is allowed to own
    private static readonly Dictionary<ItemName, int> MajorRewardLimits = new()
    {
        { ItemName.ExpShare, 2 },
        { ItemName.LuckyEgg, 2 },
        { ItemName.GoodRod, 1 },
        { ItemName.SuperRod, 1 }
    };

    /// <summary>
    /// Gives the player their tournament rewards (one major + one minor, or two minors if no major can be given)
    /// and returns the names of the items received
    /// </summary>
    private List<string> GetRewards()
    {
        var pool = RewardPools[currentDifficulty];
        var rewards = new List<ItemName>();

        var major = RollMajorReward(pool.Major);
        var minorCount = 1;
        if (major.HasValue)
        {
            rewards.Add(major.Value);
        }
        else
        {
            //both major rolls were already owned to the max, so double the minor prizes
            minorCount = 2;
        }
        rewards.AddRange(RollMinorRewards(pool.Minor, minorCount));

        foreach (var reward in rewards)
        {
            _bag.AddItem(LoadRewardItem(reward));
        }
        return rewards.Select(NameDB.GetItem).ToList();
    }

    //roll once, if the player owns the max of it reroll once for a different major, otherwise no major is given
    private ItemName? RollMajorReward(ItemName[] majorPool)
    {
        var firstPick = majorPool[Utility.RandomRange(0, majorPool.Length)];
        if (!IsAtOwnedLimit(firstPick)) return firstPick;

        var rerollPool = majorPool.Where(item => item != firstPick).ToArray();
        if (rerollPool.Length == 0) return null;

        var secondPick = rerollPool[Utility.RandomRange(0, rerollPool.Length)];
        return IsAtOwnedLimit(secondPick) ? null : secondPick;
    }

    //picks without replacement so a double reward never gives the same item twice
    private static List<ItemName> RollMinorRewards(ItemName[] minorPool, int count)
    {
        var remaining = new List<ItemName>(minorPool);
        var picks = new List<ItemName>();
        for (int i = 0; i < count && remaining.Count > 0; i++)
        {
            var index = Utility.RandomRange(0, remaining.Count);
            picks.Add(remaining[index]);
            remaining.RemoveAt(index);
        }
        return picks;
    }

    private bool IsAtOwnedLimit(ItemName item)
    {
        if (!MajorRewardLimits.TryGetValue(item, out var limit)) return false;
        return GetOwnedQuantity(item) >= limit;
    }

    private int GetOwnedQuantity(ItemName itemName)
    {
        var itemNameString = NameDB.GetItem(itemName);

        // items in the bag
        var owned = _bag.allItems
            .Where(item => item.itemName == itemNameString)
            .Sum(item => item.quantity);

        // items held by party members, each Pokemon can only hold one
        foreach(var pokemon in _partyHandler.Party)
        {
            if (!pokemon.hasItem)continue;
            if(pokemon.heldItem.itemName == itemNameString)
            {
                owned++;
            }
        }
        return owned;
    }
    
    private static Item LoadRewardItem(ItemName itemName)
    {
        var assetDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Items) + NameDB.GetItem(itemName);
        var item = Resources.Load<Item>(assetDirectory);
        if (item is null)
        {
            throw new Exception($"[Tournament Rewards],{assetDirectory} doesnt exist, check item name in pool");
        }
        return InstanceFactory.CreateItem(item);
    }
    #endregion

    #region Round Help Items
    private const int RoundOneXItemCount = 2;
    private static readonly ItemName[] XItems =
    {
        ItemName.XAttack, ItemName.XDefense, ItemName.XSpAttack,
        ItemName.DireHit, ItemName.XSpeed, ItemName.XAccuracy
    };
    private static readonly ItemName[] RoundTwoHelpItems = { ItemName.FullRestore, ItemName.MaxPotion };
    private static readonly ItemName[] RoundThreeHelpItems = { ItemName.Revive, ItemName.FullRestore };
    private static readonly ItemName[] FinalRoundHelpItems = { ItemName.MaxRevive, ItemName.GuardSpec, ItemName.MaxPotion };

    //temporary items go into the tournament bag, which is wiped when the saved bag is restored at the end
    private void GiveRoundHelpItems()
    {
        foreach (var item in GetRoundHelpItems())
        {
            _bag.AddItem(LoadRewardItem(item));
        }
    }

    private List<ItemName> GetRoundHelpItems()
    {
        //the final round is the only one with no right bracket
        if (rightBracket.Count == 0) return new List<ItemName>(FinalRoundHelpItems);

        switch (currentRound)
        {
            case 1: return RollMinorRewards(XItems, RoundOneXItemCount);
            case 2: return new List<ItemName>(RoundTwoHelpItems);
            case 3: return new List<ItemName>(RoundThreeHelpItems);
            default: return new List<ItemName>();
        }
    }
    #endregion

    private void SavePlayerBag()
    {
        playerSavedItems.AddRange(_bag.allItems);
        _bag.allItems.Clear();
    }
    private void RestorePlayerBag()
    {
        _bag.allItems.Clear();
        _bag.allItems.AddRange(playerSavedItems);
        playerSavedItems.Clear();
    }
    private void RestorePlayerParty()
    {
        _partyHandler.ClearPartyMembers();
        foreach (var pokemon in playerSavedPokemon)
        {
            _partyHandler.AddMemberFromSystemProcess(pokemon);
        }
        playerSavedPokemon.Clear();
    }
    private void ResetTournamentState()
    {
        if (tournamentIntro != null)
        {
            StopCoroutine(tournamentIntro);
            tournamentIntro = null;
        }
        partyScreenUi.SetActive(false);
        vsScreenUi.SetActive(false);

        DestroyAllNpcInstances();

        leftBracket.Clear();
        rightBracket.Clear();
        leftContestants.Clear();
        rightContestants.Clear();
        playerContestant = null;
        currentRound = 0;

        foreach (var contestant in contestantRegistry.contestants)
        {
            contestant.bracketIndex = 0;
            contestant.logic = null;
        }
    }
   
   private void BeginNewRound(TournamentBracket bracket)
   {
       var npcContestant = bracket.secondContestant;
       int movementIndex = 1;
       
       npcContestant.logic = CreateNpcContestant(npcContestant);
       var movementHandler = npcContestant.logic.movementHandler;
       movementHandler.OnMovementEnded += MoveToPlayerAndStartBattle;
       movementHandler.MoveToSpecific(MovementDirection.Down,4,5);
       return;
       void MoveToPlayerAndStartBattle()
       {
           if (movementIndex == 2)
           {
               movementHandler.OnMovementEnded -= MoveToPlayerAndStartBattle;
               npcTrainer.PokemonParty.Clear();
               npcTrainer.trainerName = npcContestant.trainerName;
               npcTrainer.battleIntroSprite = npcContestant.battleIntroSprite;
               foreach (var member in npcContestant.pokemonPartyData)
               {
                   var newData = ScriptableObject.CreateInstance<TrainerPokemonData>();
                   newData.data = new NaturalPokemonCreationData
                   {
                       pokemon = member.pokemon,
                       pokemonLevel = RequiredPokemonLevel - opponentLevelGap,
                       evolutionStageNumber = member.evolutionStageNumber,
                       moveSet = member.moveSet
                   };
                   npcTrainer.PokemonParty.Add(newData);
               }
               _battleHandler.isBattleCompetition = true;
               _dialogueHandler.StartInteraction(battleInteraction);
           } else
           {
               movementHandler.MoveToSpecific(MovementDirection.Left,3,5);
               movementIndex++;
           }  
       }
   }
   private NpcLogic CreateNpcContestant(TournamentContestant contestant)
   {
       var npcInstance = Instantiate(_areaManager.npcPrefab, npcIntroPosition, _areaManager.npcPrefab.transform.rotation,
           _areaManager.overworldNpcParent);
      
       var npcLogic = npcInstance.GetComponentInChildren<NpcLogic>();
       npcLogic.movementHandler.isControlled = true;
       
       var walkOutData = new NpcAnimationData
       {
           spriteData = contestant.spriteData,
           movementDirections = new List<NpcMovementDirection>
           {
               new(MovementDirection.Down,4),
               new(MovementDirection.Left,3)
           }
       };
       npcLogic.movementHandler.animationData = walkOutData;
       npcLogic.Inject(serviceContainer, new OverwolrdNpcData
       {
           autoDetectPlayer = false,
           npcData = new NpcCreationData
           {
               npcAnimationData = walkOutData,npcPosition = npcIntroPosition
           }
       });
       
       npcInstance.SetActive(true);
       spawnedNpcInstances.Add(npcInstance);
       currentOpponentInstance = npcInstance;
       return npcLogic;
   }
   private void DestroyCurrentOpponent()
   {
       if (currentOpponentInstance == null) return;

       spawnedNpcInstances.Remove(currentOpponentInstance);
       Destroy(currentOpponentInstance);
       currentOpponentInstance = null;
   }
   private void DestroyAllNpcInstances()
   {
       foreach (var npcInstance in spawnedNpcInstances)
       {
           if (npcInstance != null) Destroy(npcInstance);
       }
       spawnedNpcInstances.Clear();
       currentOpponentInstance = null;

       foreach (var contestant in contestantRegistry.contestants)
       {
           contestant.logic = null;
       }
   }
}