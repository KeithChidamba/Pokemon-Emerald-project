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
    public int enemyIndex;

    public AiMoveScoreData(int moveIndex, int moveScore)
    {
        this.moveIndex = moveIndex;
        this.moveScore = moveScore;
    }
}
[Serializable]
public class Enemy_trainer : BattleParticipantModule
{
    public TrainerData trainerData;
    public List<Pokemon> trainerParty = new();
    
    private Dictionary<AiFlags, Func<Battle_Participant,Move,int>> AiLogicCalculators = new();
    
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private BattleIntro _battleIntroHandler;
    
    public Enemy_trainer(ServiceContainer container)
    {
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        OnInject();
    }

    private void OnInject()
    {
        AiLogicCalculators.Add(AiFlags.CheckBadMove ,AiCheckBadMove);
        AiLogicCalculators.Add(AiFlags.CheckViability ,AiCheckViability);
        AiLogicCalculators.Add(AiFlags.CheckStatus ,AiCheckStatus);
        AiLogicCalculators.Add(AiFlags.CheckSetup ,AiCheckSetup);
        AiLogicCalculators.Add(AiFlags.CheckPriority ,AiCheckPriority);
        //switching doesnt involve calculators
    }
    public void SetupTrainerForBattle(TrainerData copyOfTrainerData)
    {
        trainerData = Obj_Instance.CreateTrainer(copyOfTrainerData);
        trainerParty.Clear();
        foreach (TrainerPokemonData member in trainerData.PokemonParty)
        {
            var pokemonCopy = Obj_Instance.CreatePokemon(member.pokemon);
            trainerParty.Add(pokemonCopy);
            var expForNextLevel = PokemonOperations.CalculateExpForNextLevel(member.pokemonLevel, pokemonCopy.expGroup)+1;
            pokemonCopy.ReceiveExperience(expForNextLevel);
            pokemonCopy.hp = pokemonCopy.maxHp;
            pokemonCopy.moveSet.Clear();
            foreach (Move move in member.moveSet)
                pokemonCopy.moveSet.Add(Obj_Instance.CreateMove(move));
            
            if (member.hasItem) pokemonCopy.GiveItem(Obj_Instance.CreateItem(member.heldItem));
        }
    }
    public List<Pokemon> GetLivingPokemon()
    {
        List<Pokemon> alivePokemon = new(trainerParty);
        alivePokemon.RemoveAll(p => p.hp <= 0);
        return alivePokemon;
    }

    private List<Pokemon> GetNonParticipatingList()
    {
        List<Pokemon> notParticipatingList = new();
        foreach (Pokemon pokemon in trainerParty)
        {
            //not already participating
            if (pokemon != _battleHandler.battleParticipants[2].pokemon)
            {
                if (_battleHandler.isDoubleBattle)
                {
                    if (pokemon != _battleHandler.battleParticipants[3].pokemon)
                    {
                        notParticipatingList.Add(pokemon);
                    }
                }
                else
                {
                    notParticipatingList.Add(pokemon);
                }
            }
        }
        notParticipatingList.RemoveAll(p => p.hp <= 0);
        return notParticipatingList;
    }
    public IEnumerator CheckIfLoss()
    {
        var numAlive = GetLivingPokemon();
        if (numAlive.Count == 0)
        {
            _battleHandler.lastOpponent = participant.pokemon;
            _battleHandler.EndBattle(true);
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
                    var randomLeftOver = Utility.RandomRange(0, notParticipatingList.Count - 1);
                    yield return _turnBasedCombatHandler.AllowPlayerSwitchIn(trainerData.TrainerName,
                        notParticipatingList[randomLeftOver].pokemonName);
                    yield return _battleIntroHandler.SwitchInPokemon(participant,notParticipatingList[randomLeftOver],false);
                }
            }
            else
            {
                var randomMember = Utility.RandomRange(0, numAlive.Count - 1);
                yield return _turnBasedCombatHandler.AllowPlayerSwitchIn(trainerData.TrainerName, 
                    numAlive[randomMember].pokemonName);
                yield return _battleIntroHandler.SwitchInPokemon(participant,newPokemon:numAlive[randomMember],false);
            }
        }
        _turnBasedCombatHandler.faintEventDelay = false;
    }
    private int AiCheckValidSwitch(Battle_Participant enemy)
    {
        if (participant.canEscape && BattleOperations.HardCountered(participant.pokemon,enemy.pokemon))
        {
            List<(int pokemonIndex,float effectivenessScore)> pokemonScores = new();  
            var participatingIndex = _battleHandler.isDoubleBattle? 2:1;
            //skip participating pokemon
            for (int i=participatingIndex; i<trainerParty.Count;i++)
            {
                if (trainerParty[i].hp<=0) continue;
                
                if (BattleOperations.HardCountered(trainerParty[i], enemy.pokemon)) continue;
                
                float typeEffectiveness = 0;
                foreach (var type in enemy.pokemon.types)
                {
                    typeEffectiveness += BattleOperations.GetTypeEffectiveness(trainerParty[i], type);
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

    private void SwitchPokemon(int partyIndex)
    {
        int partyPosition = 0;
        
        if (_battleHandler.isDoubleBattle)
        {
            partyPosition = _turnBasedCombatHandler.currentTurnIndex == 2 ? 0 : 1;
        }
        var switchData = new SwitchOutData(partyPosition,partyIndex,participant);
        _turnBasedCombatHandler.SaveSwitchTurn(switchData);
    }
    public void MakeBattleDecision()
    {
        if (!participant.isActive) return;
        
        var numParticipating = _battleHandler.isDoubleBattle? 2:1;
        
        if (GetLivingPokemon().Count > numParticipating)//can a switch be made?
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
        var selectedMoveData = GetBestMoveDecision();
        var selectedMove = participant.pokemon.moveSet[selectedMoveData.moveIndex];
        _battleHandler.UseMove(selectedMove,participant,selectedMoveData.enemyIndex);
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
            var enemyIndex = 0;
            if (_battleHandler.isDoubleBattle)
            {
                var validEnemies = participant.currentEnemies
                    .Where(e => e.isActive)
                    .Select(e => Array.IndexOf(_battleHandler.battleParticipants, e))
                    .ToList();
                enemyIndex = validEnemies[Utility.RandomRange(0, validEnemies.Count)];
            }
            var moveIndex = Utility.RandomRange(0, participant.pokemon.moveSet.Count);
            var randomMoveData = new AiMoveScoreData(moveIndex,0);
            randomMoveData.enemyIndex = enemyIndex;
            return randomMoveData;
        }
        
        if(_battleHandler.isDoubleBattle)
        {
            List<AiMoveScoreData> bestMovesForEnemies = new();
            foreach (var enemy in participant.currentEnemies)
            {
                if (!enemy.isActive) continue;
                var currentIndex = Array.IndexOf(_battleHandler.battleParticipants, enemy);
                var newMoveScore = GetBestMove(enemy);
                newMoveScore.enemyIndex = currentIndex;
                bestMovesForEnemies.Add(newMoveScore);
            }
            var orderList = bestMovesForEnemies.OrderByDescending(move=>move.moveScore).ToList();
            //select the attack that hits a particular enemy the hardest out of all of them
            var bestAttackingDecision = orderList[0];
            return bestAttackingDecision;
        }
        //single battle
        var singleBattleMoveScore = GetBestMove(_battleHandler.battleParticipants[0]);
        return singleBattleMoveScore;
    }
    private AiMoveScoreData GetBestMove(Battle_Participant enemy)
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
    private int GetMoveScore(Battle_Participant enemy, Move currentMoveCheck)
    {
        int currentScore = (int)currentMoveCheck.moveDamage;
       
        foreach (var flag in trainerData.trainerAiFlags)
        {
            if (!AiLogicCalculators.ContainsKey(flag))
            {//skip check switching because it's not a not calculator
                if (flag!=AiFlags.CheckSwitching)
                {
                    Debug.Log($"{flag} flag has not been accounted for");
                }
                continue;
            }
            currentScore += AiLogicCalculators[flag].Invoke(enemy,currentMoveCheck);
        }
        currentScore += Utility.RandomRange(-3, 4);//variable difference
        return currentScore;
    }

    private int AiCheckBadMove(Battle_Participant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if (BattleOperations.HasImmunity(enemy.pokemon, currentMoveCheck.type))
        {
            scoreDifference-=120;
        }
        if ( currentMoveCheck.effectType==EffectType.WeatherHealthGain && participant.pokemon.hp>=participant.pokemon.maxHp)
        {
            scoreDifference-=100;
        }
        return scoreDifference;
    }
    private int AiCheckViability(Battle_Participant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if (BattleOperations.IsStab(participant.pokemon, currentMoveCheck.type))
        {
            scoreDifference+=12;
        }
        var typeEffectiveness = BattleOperations.CheckTypeEffectiveness(enemy, currentMoveCheck.type);
        scoreDifference += (int)typeEffectiveness * 15;
        
        return scoreDifference;
    }
    private int AiCheckStatus(Battle_Participant enemy,Move currentMoveCheck)
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
    private int AiCheckSetup(Battle_Participant enemy,Move currentMoveCheck)
    {
        int scoreDifference = 0;
        if ( (participant.pokemon.hp >= participant.pokemon.maxHp*0.5f) && participant.pokemon.speed>=enemy.pokemon.speed)
        {
            if (currentMoveCheck.isBuffOrDebuff && currentMoveCheck.isSelfTargeted)
            {
                scoreDifference = 40;
            }
        }
        return scoreDifference;
    }
    private int AiCheckPriority(Battle_Participant enemy,Move currentMoveCheck)
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
