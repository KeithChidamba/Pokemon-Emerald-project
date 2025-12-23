using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Enemy_trainer : MonoBehaviour
{
    public Battle_Participant participant;
    public TrainerData trainerData;
    public List<Pokemon> trainerParty;
    public bool inBattle;
    public bool canAttack = true;
    [SerializeField]private bool usedMove;
    private Dictionary<TrainerData.AiFlags, Func<int>> AiLogicCalculators = new();
    private Move _currentMoveCheck;
    private Battle_Participant _currentEnemy;
    private void Update()
    {
        if (!inBattle) return;
        MakeBattleDecision();
    }
    private void Start()
    {
        Turn_Based_Combat.Instance.OnNewTurn += ResetMoveUsage;
        Battle_handler.Instance.OnBattleEnd += ResetAfterBattle;
        AiLogicCalculators.Add(TrainerData.AiFlags.CheckBadMove ,AiCheckBadMove);
        AiLogicCalculators.Add(TrainerData.AiFlags.CheckViability ,AiCheckViability);
        AiLogicCalculators.Add(TrainerData.AiFlags.CheckStatus ,AiCheckStatus);
        AiLogicCalculators.Add(TrainerData.AiFlags.CheckSetup ,AiCheckSetup);
        AiLogicCalculators.Add(TrainerData.AiFlags.CheckPriority ,AiCheckPriority);
    }
    public void CanAttack()
    {
        canAttack = true;
    }

    void ResetAfterBattle()
    {
        trainerData = null;
        canAttack = true;
        usedMove = false;
    }
    void ResetMoveUsage()
    {
        if (!inBattle) return;
        usedMove = false;
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
            if(pokemon!=Battle_handler.Instance.battleParticipants[2].pokemon
               && pokemon!=Battle_handler.Instance.battleParticipants[3].pokemon)//not already participating
                notParticipatingList.Add(pokemon);
        notParticipatingList.RemoveAll(p => p.hp <= 0);
        return notParticipatingList;
    }
    public IEnumerator CheckIfLoss()
    {
        var numAlive = GetLivingPokemon();
        if (numAlive.Count == 0)
        {
            Battle_handler.Instance.lastOpponent = participant.pokemon;
            Battle_handler.Instance.EndBattle(true);
        }
        else
        {
            if (Battle_handler.Instance.isDoubleBattle)//double battle
            {//only select the pokemon that werent in battle
                var notParticipatingList = GetNonParticipatingList();
                if (notParticipatingList.Count == 0)
                {//1 left
                    if(participant.pokemon.hp<=0)
                    {
                        participant.DeactivateParticipant();
                        participant.pokemon = null;
                        inBattle = false;
                        participant.DeactivateUI();
                        Battle_handler.Instance.CheckParticipantStates();
                    }
                }
                else
                {
                    var randomLeftOver = Utility.RandomRange(0, notParticipatingList.Count - 1);
                    yield return BattleIntro.Instance.SwitchInPokemon(participant,notParticipatingList[randomLeftOver],true);
                }
            }
            else
            {
                var randomMember = Utility.RandomRange(0, numAlive.Count - 1);
                yield return BattleIntro.Instance.SwitchInPokemon(participant,newPokemon:numAlive[randomMember],true);
            }
        }
        Turn_Based_Combat.Instance.faintEventDelay = false;
    }
    public void SetupTrainerForBattle(TrainerData copyOfTrainerData)
    {
        participant = GetComponent<Battle_Participant>();
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
    private void MakeBattleDecision()
    {
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon
            != participant.pokemon)return;
        if (usedMove || !canAttack) return;
        
        var randomEnemy = Utility.RandomRange(0, participant.currentEnemies.Count);
        Battle_handler.Instance.currentEnemyIndex = randomEnemy;
        _currentEnemy = Battle_handler.Instance.battleParticipants[randomEnemy];
        
       if(trainerData.trainerAiFlags.Contains(TrainerData.AiFlags.CheckSwitching))
       {
           AiCheckSwitching();
           canAttack = false;
           return;
       }
        
        var selectedMove = participant.pokemon.moveSet[GetSelectedMoveIndex()];
        canAttack = false;
        Battle_handler.Instance.UseMove(selectedMove,participant);
        usedMove = true;
    }
    private void AiCheckSwitching()
    {
        if (!participant.canEscape || BattleOperations.HardCountered(participant,_currentEnemy))
        {
            for (int i=0; i<trainerParty.Count;i++)
            {
                float typeEffectiveness = 0;
                foreach (var type in trainerParty[i].types)
                {
                    typeEffectiveness += BattleOperations.GetTypeEffectiveness(participant, type);
                }
                if (typeEffectiveness < 2)
                {
                    SwitchPokemon(i);
                    return;
                }
            }
            var randomLeftOver = Utility.RandomRange(0, GetNonParticipatingList().Count - 1);
            SwitchPokemon(randomLeftOver);
        }
    }

    private void SwitchPokemon(int partyIndex)
    {
        var switchData = new SwitchOutData(Turn_Based_Combat.Instance.currentTurnIndex
            ,partyIndex,participant);
        Turn_Based_Combat.Instance.SaveSwitchTurn(switchData);
    }
    private int GetSelectedMoveIndex()
    {
        if(trainerData.trainerAiFlags.Count==0)
            return Utility.RandomRange(0, participant.pokemon.moveSet.Count);
        List<(int moveIndex,int moveScore)> moveScores = new();
        
        for (int i=0; i<participant.pokemon.moveSet.Count;i++)
        {
            _currentMoveCheck = participant.pokemon.moveSet[i];
            moveScores.Add(new(i,GetMoveScore()));
        }
        
        var orderList = moveScores.OrderByDescending(move=>move.moveScore).ToList();
        return orderList[0].moveIndex;
    }

    private int GetMoveScore()
    {
        int currentScore = (int)_currentMoveCheck.moveDamage;
        foreach (var flag in trainerData.trainerAiFlags)
        {
            currentScore += AiLogicCalculators[flag].Invoke();
        }
        return currentScore;
    }

    private int AiCheckBadMove()
    {
        int scoreDifference = 0;
        if (BattleOperations.HasImmunity(_currentEnemy.pokemon, _currentMoveCheck.type))
        {
            scoreDifference-=100;
        }
        if ( _currentMoveCheck.effectType==Move.EffectType.WeatherHealthGain && participant.pokemon.hp>=participant.pokemon.maxHp)
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
            scoreDifference+=10;
        }
        scoreDifference += (int)(BattleOperations.GetTypeEffectiveness(_currentEnemy, _currentMoveCheck.type)*10);
        
        return scoreDifference;
    }
    private int AiCheckStatus()
    {
        int scoreDifference = 0;
        if (_currentEnemy.pokemon.statusEffect!=PokemonOperations.StatusEffect.None && _currentMoveCheck.hasStatus && _currentMoveCheck.moveDamage==0)
        {
            scoreDifference -= 30;
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
                scoreDifference += 45;
            }
        }
        return scoreDifference;
    }
    private int AiCheckPriority()
    {
        int scoreDifference = 0;
        if ( (participant.pokemon.hp <= participant.pokemon.maxHp*0.33f) || _currentEnemy.pokemon.hp<=_currentMoveCheck.moveDamage)
        {
            if (_currentMoveCheck.priority>1)
            {
                scoreDifference += 65;
            }
        }
        return scoreDifference;
    }
}
