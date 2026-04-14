using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class AiMoveScoreData
{ 
    public int moveIndex; 
    public int moveScore;
    public int enemyIndex;

    public AiMoveScoreData(int moveIndex, int moveScore, int enemyIndex)
    {
        this.moveIndex = moveIndex;
        this.moveScore = moveScore;
        this.enemyIndex = enemyIndex;
    }
}
[Serializable]
public class Enemy_trainer : BattleParticipantModule
{
    public TrainerData trainerData;
    public List<Pokemon> trainerParty = new();
    
    private Dictionary<AiFlags, Func<int>> AiLogicCalculators = new();
    private Move _currentMoveCheck;
    [SerializeField]private Battle_Participant _currentEnemy;
    
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private BattleIntro _battleIntroHandler;
    
    public Enemy_trainer(ServiceContainer container)
    {
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
       
    }
    private void Start()
    {
        AiLogicCalculators.Add(AiFlags.CheckBadMove ,AiCheckBadMove);
        AiLogicCalculators.Add(AiFlags.CheckViability ,AiCheckViability);
        AiLogicCalculators.Add(AiFlags.CheckStatus ,AiCheckStatus);
        AiLogicCalculators.Add(AiFlags.CheckSetup ,AiCheckSetup);
        AiLogicCalculators.Add(AiFlags.CheckPriority ,AiCheckPriority);
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
    
    public void MakeBattleDecision()
    {
        if (!participant.isActive) return;
        
        var numParticipating = _battleHandler.isDoubleBattle? 2:1;
        
        if (!_battleHandler.isDoubleBattle)
        {
            _currentEnemy = _battleHandler.battleParticipants[0];
            _battleHandler.currentEnemyIndex = 0;
        }
        
        if (GetLivingPokemon().Count > numParticipating)//can a switch be made?
        {
            if(trainerData.trainerAiFlags.Contains(AiFlags.CheckSwitching))
            {
                if (_battleHandler.isDoubleBattle)
                {
                    foreach (var enemy in participant.currentEnemies)
                    {
                        _currentEnemy = enemy;
                        var switchIndex = AiCheckValidSwitch();
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
                    var switchIndex = AiCheckValidSwitch();
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
        var selectedMove = participant.pokemon.moveSet[GetBestMoveIndex()];
        _battleHandler.UseMove(selectedMove,participant);
    }
    private int AiCheckValidSwitch()
    {
        if (participant.canEscape && BattleOperations.HardCountered(participant.pokemon,_currentEnemy.pokemon))
        {
            List<(int pokemonIndex,float effectivenessScore)> pokemonScores = new();  
            var participatingIndex = _battleHandler.isDoubleBattle? 2:1;
            //skip participating pokemon
            for (int i=participatingIndex; i<trainerParty.Count;i++)
            {
                if (trainerParty[i].hp<=0) continue;
                
                if (BattleOperations.HardCountered(trainerParty[i], _currentEnemy.pokemon)) continue;
                
                float typeEffectiveness = 0;
                foreach (var type in _currentEnemy.pokemon.types)
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
    private int GetBestMoveIndex()
    {
        if(trainerData.trainerAiFlags.Count==0)
        {
            if (_battleHandler.isDoubleBattle)
            {
                var randomEnemyIndex = Utility.RandomRange(0, 2);
                _battleHandler.currentEnemyIndex = randomEnemyIndex;
            }
            return Utility.RandomRange(0, participant.pokemon.moveSet.Count);//random move index
        }
        
        if(_battleHandler.isDoubleBattle)
        {
            List<AiMoveScoreData> bestMovesForEnemies = new();
            foreach (var enemy in participant.currentEnemies)
            {
                _currentEnemy = enemy;
                bestMovesForEnemies.Add(GetBestMove());
            }
            var orderList = bestMovesForEnemies.OrderByDescending(move=>move.moveScore).ToList();
            //select the attack that hits a particular enemy the hardest out of all of them
            var bestAttackingDecision = orderList[0];
            _battleHandler.currentEnemyIndex = bestAttackingDecision.enemyIndex;
            return bestAttackingDecision.moveIndex;
        }
        return GetBestMove().moveIndex;
    }
    private AiMoveScoreData GetBestMove()
    {
        var currentEnemyIndex = Array.IndexOf(_battleHandler.battleParticipants, _currentEnemy);
        List<AiMoveScoreData> moveScores = new();
    
        for (int i=0; i<participant.pokemon.moveSet.Count;i++)
        {
            _battleHandler.currentEnemyIndex = Array.IndexOf(_battleHandler.battleParticipants, _currentEnemy);
            _currentMoveCheck = participant.pokemon.moveSet[i];
            moveScores.Add(new AiMoveScoreData(i,GetMoveScore(), currentEnemyIndex));
        }
    
        var orderList = moveScores.OrderByDescending(move=>move.moveScore).ToList();
        var topScore = orderList[0].moveScore;
        var bestMoves = orderList.Where(m => m.moveScore == topScore).ToList();
        return bestMoves[Utility.RandomRange(0, bestMoves.Count)];
    }
    private int GetMoveScore()
    {
        int currentScore = (int)_currentMoveCheck.moveDamage;
        foreach (var flag in trainerData.trainerAiFlags)
        {
            if (!AiLogicCalculators.ContainsKey(flag)) continue;
            currentScore += AiLogicCalculators[flag].Invoke();
        }
        currentScore += Utility.RandomRange(-3, 4);//variable difference
        return currentScore;
    }

    private int AiCheckBadMove()
    {
        int scoreDifference = 0;
        if (BattleOperations.HasImmunity(_currentEnemy.pokemon, _currentMoveCheck.type))
        {
            scoreDifference-=120;
        }
        if ( _currentMoveCheck.effectType==EffectType.WeatherHealthGain && participant.pokemon.hp>=participant.pokemon.maxHp)
        {
            scoreDifference-=100;
        }
        return scoreDifference;
    }
    private int AiCheckViability()
    {
        int scoreDifference = 0;
        if (BattleOperations.IsStab(participant.pokemon, _currentMoveCheck.type))
        {
            scoreDifference+=12;
        }
        scoreDifference += (int)(BattleOperations.CheckTypeEffectiveness(_currentEnemy, _currentMoveCheck.type)*15);
        
        return scoreDifference;
    }
    private int AiCheckStatus()
    {
        int scoreDifference = 0;
        if (_currentMoveCheck.hasStatus && _currentMoveCheck.moveDamage==0)
        {
            if (_currentEnemy.pokemon.statusEffect==StatusEffect.Sleep ||
                _currentEnemy.pokemon.statusEffect==StatusEffect.Paralysis)
            {
                scoreDifference = 18;
            }
            if (_currentEnemy.pokemon.statusEffect==StatusEffect.None)
            {
                scoreDifference = -35;
            }
        }
        return scoreDifference;
    }
    private int AiCheckSetup()
    {
        int scoreDifference = 0;
        if ( (participant.pokemon.hp >= participant.pokemon.maxHp*0.5f) && participant.pokemon.speed>=_currentEnemy.pokemon.speed)
        {
            if (_currentMoveCheck.isBuffOrDebuff && _currentMoveCheck.isSelfTargeted)
            {
                scoreDifference = 40;
            }
        }
        return scoreDifference;
    }
    private int AiCheckPriority()
    {
        int scoreDifference = 0;
        if (_currentMoveCheck.priority>0)
        {
            if (_currentEnemy.pokemon.hp<=_currentMoveCheck.moveDamage)
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
