using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TournamentBracket
{
    public TournamentContestant firstContestant;
    public TournamentContestant secondContestant;
}
public class BattleZoneHandler : MonoBehaviour,IInjectable
{
    public BattleZoneContestantRegistry contestantRegistry;
    public TrainerData npcTrainer;
    public Interaction battleInteraction;
    public Vector3 npcIntroPosition;
    public Vector3 playerBattlePosition;
    private const int ContestantPartyCount = 3;
    private const int ContestantCount = 16;
    private const int RequiredPokemonLevel = 65;
    private const int MovesPerPokemon = 4;

    private IReadOnlyList<Pokemon> allPokemon;
    private IReadOnlyList<Move> allMoves;
    
    public List<TournamentBracket> leftBracket = new();
    public List<TournamentContestant> leftContestants = new();
    public List<TournamentBracket> rightBracket = new();
    public List<TournamentContestant> rightContestants = new();
    private TournamentContestant playerContestant;
    private List<Pokemon> playerSavedPokemon = new();
    private readonly List<GameObject> spawnedNpcInstances = new();
    private GameObject currentOpponentInstance;
    
    private Coroutine roundTransition;
    
    private AreaManager _areaManager;
    private PokemonPartyHandler _partyHandler;
    private DialogueHandler _dialogueHandler;
    private PlayerMovementHandler _playerMovement;
    private GameLoadingHandler _gameLoader;
    private PokemonOperations _pokemonOperations;
    private BattleHandler _battleHandler;
    private GameUiHandler _gameUi;
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
   }
   
   public void OnInject()
   {
      gameObject.SetActive(true);
      allPokemon = Resources.LoadAll<Pokemon>(DirectoryHandler.GetDirectory(AssetDirectory.Pokemon));
      allMoves = Resources.LoadAll<Move>(DirectoryHandler.GetDirectory(AssetDirectory.Moves));
   }

   public void BeginTournament()
   {
       _gameUi.SetMenuAccessibility(false);
       _battleHandler.OnTrainerBattleResult += DetermineNextBracket;
      
       leftBracket.Clear();
       rightBracket.Clear();
       leftContestants.Clear();
       rightContestants.Clear();

       playerSavedPokemon.AddRange(_partyHandler.Party);
       _partyHandler.ClearPartyMembers();
       
       _playerMovement.SetPlayerPosition(playerBattlePosition);
       _playerMovement.FaceOppositeDirection(MovementDirection.Left);
       _playerMovement.RestrictPlayerMovement(MovementRestrictor.OverworldAction);

       var numBracketsPerColumn = ContestantCount / 4;
       var npcListCopy = new List<TournamentContestant>(contestantRegistry.contestants);

       playerContestant = new TournamentContestant
       {
           trainerName = _gameLoader.playerData.playerName,
           bracketIndex = 0
       };

       leftBracket.Add(new TournamentBracket
       {
           firstContestant = playerContestant,
           secondContestant = GetRandomContestant(0)
       });
       
       //Give player random pokemon
       SetContestantParty(leftBracket[0].firstContestant);
       
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
           SetContestantParty(contestant);
           contestant.bracketIndex = bracketIndex;
           return contestant;
       }
       
       StartCoroutine(SetPokemonData());
       IEnumerator SetPokemonData()
       {
           foreach (var member in leftBracket.First().firstContestant.pokemonParty)
           {
               yield return _pokemonOperations.HandlePokemonCreation(GetPokemonCopy
                   ,member
                   ,RequiredPokemonLevel,
                   Utility.RandomRange(0, member.evolutions.Count + 1));
               continue;
               void GetPokemonCopy(Pokemon pokemonCopy)
               {
                   PokemonOperations.ResolvePokemonNickName(pokemonCopy);
                   _partyHandler.AddMember(pokemonCopy);
                   pokemonCopy.moveSet = GetRandomFromLearnSet(pokemonCopy.learnSet);
               }
           }
       }
      
       BeginNewRound(leftBracket.First());
   }
   
   private void DetermineNextBracket(bool battleWon, List<TrainerData> enemies)
   {
       if (roundTransition != null) return;
       roundTransition = StartCoroutine(ProcessRoundTransition(battleWon));
   }
   private IEnumerator ProcessRoundTransition(bool battleWon)
   {
       yield return AwaitBattleTeardown();

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

       //between-round dialogue, anything yielded here holds the next round back

       roundTransition = null;
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
        ResetTournamentState();

        if (playerWonTournament)
        {
            //reward player
            _dialogueHandler.DisplayDetails("well done, you won. Rewards are coming soon");
        }
        //spawn player outside battlezone, release movement restriction
        _areaManager.TeleportToArea(AreaName.BattleZone);
        _playerMovement.AllowPlayerMovement(MovementRestrictor.OverworldAction);
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
        DestroyAllNpcInstances();

        leftBracket.Clear();
        rightBracket.Clear();
        leftContestants.Clear();
        rightContestants.Clear();
        playerContestant = null;

        foreach (var contestant in contestantRegistry.contestants)
        {
            contestant.pokemonParty.Clear();
            contestant.bracketIndex = 0;
            contestant.logic = null;
        }
    }
    private void SetContestantParty(TournamentContestant contestant)
    {
        contestant.pokemonParty.Clear();
        for (int i = 0; i < ContestantPartyCount; i++)
        {
            var randomPartyIndex = Utility.RandomRange(0, allPokemon.Count);
            while (contestant.pokemonParty.Contains(allPokemon[randomPartyIndex]))
            {
                randomPartyIndex = Utility.RandomRange(0, allPokemon.Count);
            }

            contestant.pokemonParty.Add(allPokemon[randomPartyIndex]);
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
               foreach (var member in npcContestant.pokemonParty)
               {
                   var newData = ScriptableObject.CreateInstance<TrainerPokemonData>();
                   newData.data = new NaturalPokemonCreationData
                   {
                       pokemon = member,
                       pokemonLevel = RequiredPokemonLevel,
                       evolutionStageNumber = Utility.RandomRange(0, member.evolutions.Count + 1),
                       moveSet = GetRandomFromLearnSet(member.learnSet)
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
    private List<Move> GetRandomFromLearnSet(LearnSetMove[] learnset)
    {
        var availableMoves = learnset
            .GroupBy(m => m.GetName())
            .Select(group => group.First())
            .ToList();

        var moveCount = Mathf.Min(MovesPerPokemon, availableMoves.Count);
        var moveList = new List<Move>();

        for (int i = 0; i < moveCount; i++)
        {
            var random = Utility.RandomRange(0, availableMoves.Count);
            var learnSetMove = availableMoves[random];
            availableMoves.RemoveAt(random);

            moveList.Add(allMoves.First(m => m.moveName == learnSetMove.GetName()));
        }
        return moveList;
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
