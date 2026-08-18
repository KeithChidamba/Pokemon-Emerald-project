using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

class AiMoveScoreData
{ 
    public int moveIndex; 
    public int moveScore;
    public BattleParticipantKey enemyKey;

    public AiMoveScoreData(int moveIndex, int moveScore)
    {
        this.moveIndex = moveIndex;
        this.moveScore = moveScore;
    }
}

public enum BehaviorMode
{
    Natural,Controlled
}
[Serializable]
public class EnemyAiHandler : BattleParticipantModule
{
    public TrainerData trainerData;
    public IReadOnlyList<Pokemon> TrainerParty => trainerParty;
    
    [SerializeField]private List<Pokemon> trainerParty = new();
    
    private Dictionary<AiFlags, Func<BattleParticipant,Move,int>> _aiLogicCalculators = new();
    private Action _currentBehaviorAction;
    private BehaviorMode _behaviorMode;
    
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private BattleIntro _battleIntroHandler;
    private BattleOperations _battleOperations;
    private PokemonOperations _pokemonOperations;
    
    public EnemyAiHandler(ServiceContainer container)
    {
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _battleOperations = container.Resolve<BattleOperations>();
        _pokemonOperations = container.Resolve<PokemonOperations>();
        
        _aiLogicCalculators.Add(AiFlags.CheckBadMove ,AiCheckBadMove);
        _aiLogicCalculators.Add(AiFlags.CheckViability ,AiCheckViability);
        _aiLogicCalculators.Add(AiFlags.CheckStatus ,AiCheckStatus);
        _aiLogicCalculators.Add(AiFlags.CheckSetup ,AiCheckSetup);
        _aiLogicCalculators.Add(AiFlags.CheckPriority ,AiCheckPriority);
        //switching doesnt involve calculators
    }

    public List<Pokemon> GetPartyLink()
    {
        return trainerParty;
    }
    public void CopyPartnerData(List<Pokemon> party, TrainerData data)
    {
        trainerParty = party;
        trainerData = data;
    }
    public IEnumerator SetupTrainerForBattle(TrainerData copyOfTrainerData)
    {
        trainerData = InstanceFactory.CreateTrainer(copyOfTrainerData);
        trainerParty.Clear();
        foreach (var member in trainerData.PokemonParty)
        {
            yield return _pokemonOperations.HandlePokemonCreation(GetPokemonCopy,member.data.pokemon
                ,member.data.pokemonLevel,member.data.evolutionStageNumber);
            
            void GetPokemonCopy(Pokemon pokemonCopy)
            {
                trainerParty.Add(pokemonCopy);
                pokemonCopy.moveSet.Clear();
                foreach (var move in member.data.moveSet)
                {
                    pokemonCopy.moveSet.Add(InstanceFactory.CreateMove(move));
                }
                if (member.data.hasItem) pokemonCopy.GiveItem(InstanceFactory.CreateItem(member.data.heldItem));
            }
        }
    }
    public void SwapIndexes(int partyPosition,int memberToSwapWith)
    {
        (trainerParty[partyPosition], trainerParty[memberToSwapWith]) = (trainerParty[memberToSwapWith], trainerParty[partyPosition]);
    }
    public int GetLivingPokemonCount()
    {
        return trainerParty.Count(pokemon => pokemon.hp > 0);
    }
    public List<int> GetLivingPokemonIndexes()
    {
        return Enumerable.Range(0, trainerParty.Count)
            .Where(i => trainerParty[i].hp > 0)
            .ToList();
    }

    private List<int> GetNonParticipatingList()
    {
        var activePokemon = new List<Pokemon>
        {
            _battleHandler.GetParticipant(BattleParticipantKey.Enemy).pokemon
        };

        if (_battleHandler.isDoubleBattle)
            activePokemon.Add(
                _battleHandler.GetParticipant(BattleParticipantKey.EnemyPartner).pokemon
            );

        return Enumerable.Range(0, trainerParty.Count)
            .Where(i => trainerParty[i].hp > 0)
            .Where(i => !activePokemon.Contains(trainerParty[i]))
            .ToList();
    }
    public IEnumerator CheckIfLoss()
    {
        if (GetLivingPokemonCount() == 0)
        {
            _battleHandler.EndBattle(BattleEndState.PlayerWon,participant.pokemon);
        }
        else
        {
            if (_battleHandler.isDoubleBattle)//double battle
            {//only select the pokemon that werent in battle
                var notParticipatingList = GetNonParticipatingList();
                if (notParticipatingList.Count == 0)
                {//1 left
                    if(participant.pokemon.hp<=0)
                    {
                        participant.DeactivateParticipant();
                        participant.DeactivateUI();
                        _battleHandler.CheckParticipantStates();
                    }
                }
                else
                {
                    var randomLeftOver = Utility.RandomRange(0, notParticipatingList.Count);
                    var pokemonIndex = notParticipatingList[randomLeftOver];
                    var participantPartyIndex = participant.participantKey < participant.GetPartnerKey() ? 0 : 1;
                    SwapIndexes(pokemonIndex,participantPartyIndex);
                    
                    yield return _turnBasedCombatHandler.AllowPlayerSwitchIn(trainerData.TrainerName,
                        trainerParty[participantPartyIndex].pokemonDisplayName);
                    
                    yield return _battleIntroHandler.SwitchInPokemon(participant,trainerParty[participantPartyIndex],false);
                }
            }
            else
            {
                var livingList = GetLivingPokemonIndexes();
                var randomMember = Utility.RandomRange(0, livingList.Count);
                var pokemonIndex = livingList[randomMember];
                var participantPartyIndex = 0;
                SwapIndexes(pokemonIndex,participantPartyIndex);
                
                yield return _turnBasedCombatHandler.AllowPlayerSwitchIn(trainerData.TrainerName, 
                    trainerParty[participantPartyIndex].pokemonDisplayName);
                
                yield return _battleIntroHandler.SwitchInPokemon(participant,newPokemon: trainerParty[participantPartyIndex],false);
            }
        }
        participant.EndFaintEvent();
    }
    private int AiCheckValidSwitch(BattleParticipant enemy)
    {
        if (participant.canEscape && _battleOperations.HardCountered(participant.pokemon,enemy.pokemon))
        {
            List<(int pokemonIndex,float effectivenessScore)> pokemonScores = new();  
            var participatingIndex = _battleHandler.isDoubleBattle? 2:1;
            //skip participating pokemon
            for (int i=participatingIndex; i<trainerParty.Count;i++)
            {
                if (trainerParty[i].hp<=0) continue;
                
                if (_battleOperations.HardCountered(trainerParty[i], enemy.pokemon)) continue;
                
                float typeEffectiveness = 0;
                foreach (var type in enemy.pokemon.types)
                {
                    typeEffectiveness += _battleOperations.GetTypeEffectiveness(trainerParty[i], type);
                }
                pokemonScores.Add(new(i,typeEffectiveness));
            }
            if (pokemonScores.Count > 0)
            {

                var ordered = pokemonScores
                    .OrderByDescending(pokemon => pokemon.effectivenessScore).ToList();
                
                return ordered.Last().pokemonIndex;
            }
        }
        return -1;
    }

    public void SwitchPokemon(int partyIndex)
    {
        int partyPosition = 0;
        if (_battleHandler.isDoubleBattle)
        {
            partyPosition = participant.participantKey < participant.GetPartnerKey() ? 0 : 1;
        }
        var switchData = new SwitchOutData(partyPosition,partyIndex,participant);
        _turnBasedCombatHandler.SaveSwitchTurn(switchData);
    }

    public void SetBehavior(BehaviorMode behaviorMode)
    {
        _behaviorMode = behaviorMode;
    }
    public void AssignBehaviorAction(Action action)
    {
        _currentBehaviorAction = action;
    }
    public void MakeBattleDecision()
    {
        if (_behaviorMode==BehaviorMode.Controlled)
        {
            _currentBehaviorAction?.Invoke();
            return;
        }
        var numParticipating = _battleHandler.isDoubleBattle? 2:1;
        
        if (GetLivingPokemonCount() > numParticipating)//can a switch be made?
        {
            if(trainerData.trainerAiFlags.Contains(AiFlags.CheckSwitching))
            {
                if (_battleHandler.isDoubleBattle)
                {
                    foreach (var enemy in participant.currentEnemies)
                    {
                        if (!enemy.isActive) continue;
                        var switchIndex = AiCheckValidSwitch(enemy);
                        if(switchIndex > -1)
                        {
                            SwitchPokemon(switchIndex);
                            break;
                        }
                    }
                    UseSelectedMove();
                }
                else
                {
                    var switchIndex = AiCheckValidSwitch(participant.currentEnemies[0]);
                    if(switchIndex > -1)
                    {
                        SwitchPokemon(switchIndex);
                    }
                    else
                    {
                        UseSelectedMove();
                    }
                }
            }
            else UseSelectedMove();
        }
        else UseSelectedMove();

    }
    private void UseSelectedMove()
    {
        bool emptyMoves = true;
        foreach (var move in participant.pokemon.moveSet)
        {
            if(move.powerpoints>0)
            {
                emptyMoves = false;
                break;
            }
        }

        if (emptyMoves)
        {
            _turnBasedCombatHandler.SaveStruggleTurn(participant);
            return;
        }
        
        var selectedMoveData = GetBestMoveDecision();
        var selectedMove = participant.pokemon.moveSet[selectedMoveData.moveIndex];
        _battleHandler.UseMove(selectedMove,participant,selectedMoveData.enemyKey);
    }

    private bool AiOnlySwitching()
    {
        return trainerData.trainerAiFlags.Count == 1
               && trainerData.trainerAiFlags[0] == AiFlags.CheckSwitching;
    }
    private AiMoveScoreData GetBestMoveDecision()
    {
        if(AiOnlySwitching())
        {
            BattleParticipantKey enemyKey = BattleParticipantKey.Player;
            if (_battleHandler.isDoubleBattle)
            {
                var validEnemyKeys = participant.currentEnemies
                    .Where(p => p.isActive)
                    .Select(p => p.participantKey)
                    .ToList();
                enemyKey = validEnemyKeys[Utility.RandomRange(0, validEnemyKeys.Count)];
            }
            var moveIndex = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            var randomMoveData = new AiMoveScoreData(moveIndex,0);
            randomMoveData.enemyKey = enemyKey;
            return randomMoveData;
        }
        
        if(_battleHandler.isDoubleBattle)
        {
            List<AiMoveScoreData> bestMovesForEnemies = new();
            foreach (var enemy in participant.currentEnemies)
            {
                if (!enemy.isActive) continue;
                var newMoveScore = GetBestMove(enemy);
                newMoveScore.enemyKey = enemy.participantKey;
                bestMovesForEnemies.Add(newMoveScore);
            }
            var orderList = bestMovesForEnemies.OrderByDescending(move=>move.moveScore).ToList();
            //select the attack that hits a particular enemy the hardest out of all of them
            var bestAttackingDecision = orderList[0];
            return bestAttackingDecision;
        }
        //single battle
        var singleBattleMoveScore = GetBestMove(_battleHandler.GetParticipant(BattleParticipantKey.Player));
        return singleBattleMoveScore;
    }
    private AiMoveScoreData GetBestMove(BattleParticipant enemy)
    {
        List<AiMoveScoreData> moveScores = new();
    
        for (int i=0; i<participant.pokemon.moveSet.Count;i++)
        {
            var currentMoveCheck = participant.pokemon.moveSet[i];
            moveScores.Add(new AiMoveScoreData(i,GetMoveScore(enemy,currentMoveCheck)));
        }
    
        var orderList = moveScores.OrderByDescending(move=>move.moveScore).ToList();
        var topScore = orderList[0].moveScore;
        var bestMoves = orderList.Where(m => m.moveScore == topScore).ToList();
        return bestMoves[Utility.RandomRange(0, bestMoves.Count)];
    }
    private int GetMoveScore(BattleParticipant enemy, Move currentMoveCheck)
    {
        int currentScore = (int)currentMoveCheck.moveDamage;
       
        foreach (var flag in trainerData.trainerAiFlags)
        {
            if (!_aiLogicCalculators.ContainsKey(flag))
            {//skip check switching because it's not a not calculator
                if (flag!=AiFlags.CheckSwitching)
                {
                    //Debug.Log($"{flag} flag has not been accounted for");
                }
                continue;
            }
            currentScore += _aiLogicCalculators[flag].Invoke(enemy,currentMoveCheck);
        }
        currentScore += Utility.RandomRange(-3, 4);//variable difference
        return currentScore;
    }

    private int AiCheckBadMove(BattleParticipant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if (_battleOperations.HasImmunity(enemy.pokemon, currentMoveCheck.type))
        {
            scoreDifference-=120;
        }
        if ( currentMoveCheck.effectType==EffectType.WeatherHealthGain && participant.pokemon.hp>=participant.pokemon.maxHp)
        {
            scoreDifference-=100;
        }
        return scoreDifference;
    }
    private int AiCheckViability(BattleParticipant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if (_battleOperations.IsStab(participant.pokemon, currentMoveCheck.type))
        {
            scoreDifference+=12;
        }
        var typeEffectiveness = _battleOperations.CheckTypeEffectiveness(enemy, currentMoveCheck.type);
        scoreDifference += (int)typeEffectiveness * 15;
        
        return scoreDifference;
    }
    private int AiCheckStatus(BattleParticipant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if (currentMoveCheck.hasStatus && currentMoveCheck.moveDamage==0)
        {
            if (enemy.pokemon.statusEffect==StatusEffect.Sleep ||
                enemy.pokemon.statusEffect==StatusEffect.Paralysis)
            {
                scoreDifference = 18;
            }
            if (enemy.pokemon.statusEffect==StatusEffect.None)
            {
                scoreDifference = -35;
            }
        }
        return scoreDifference;
    }
    private int AiCheckSetup(BattleParticipant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if ( (participant.pokemon.hp >= participant.pokemon.maxHp*0.5f) && participant.pokemon.speed>=enemy.pokemon.speed)
        {
            if (currentMoveCheck.canChangeStats && currentMoveCheck.isSelfTargeted)
            {
                scoreDifference = 40;
            }
        }
        return scoreDifference;
    }
    private int AiCheckPriority(BattleParticipant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if (currentMoveCheck.priority>0)
        {
            if (enemy.pokemon.hp<=currentMoveCheck.moveDamage)
            {
                scoreDifference += 70;
            }
            else if ( (participant.pokemon.hp <= participant.pokemon.maxHp*0.33f))
            {
                scoreDifference += 45;
            }
        }
        return scoreDifference;
    }
}
