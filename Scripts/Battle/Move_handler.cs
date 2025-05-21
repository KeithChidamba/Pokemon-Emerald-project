using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class Move_handler:MonoBehaviour
{
    public bool doingMove = false;
    public static Move_handler Instance;
    private Turn _currentTurn;
    [SerializeField]private Battle_Participant attacker;
    [SerializeField]private Battle_Participant victim;
    private readonly float[] _statLevels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] _accuracyAndEvasionLevels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] _critLevels = {6.25f,12.5f,25f,50f};
    private Battle_event[] _dialougeOrder={null,null,null,null,null};
    private bool _moveDelay = false;
    private bool _cancelMove = false;
    public bool processingOrder = false;
    public event Action OnMoveEnd;
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageDeal;
    public event Action<Battle_Participant,bool> OnMoveHit;
    public event Action<Battle_Participant,string> OnStatusEffectHit;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void ExecuteMove(Turn turn)
    {
        _currentTurn = turn;
        attacker = Battle_handler.Instance.battleParticipants[turn.attackerIndex];
        victim = Battle_handler.Instance.battleParticipants[turn.victimIndex];
        if (_currentTurn.move.isMultiTarget || _currentTurn.move.isSelfTargeted)
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" used "+_currentTurn.move.Move_name+"!");
        else
            Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" used "+_currentTurn.move.Move_name+" on "+victim.pokemon.Pokemon_name+"!");
        StartCoroutine(MoveSequence());
    }
    void SetMoveSequence()
    {
        _dialougeOrder[0] = new Battle_event(DealDamage, _currentTurn.move.Move_damage > 0);
        _dialougeOrder[1] = new Battle_event(CheckVictimVulnerabilityToStatus, _currentTurn.move.Has_status);
        _dialougeOrder[2] = new Battle_event(ExecuteMoveWithSpecialEffect, _currentTurn.move.Has_effect);
        _dialougeOrder[3] = new Battle_event(CheckBuffOrDebuffApplicability, _currentTurn.move.is_Buff_Debuff);
        _dialougeOrder[4] = new Battle_event(FlinchEnemy, _currentTurn.move.Can_flinch);
    }
    private IEnumerator MoveSequence()
    {
        var moveEffectiveness = BattleOperations.GetTypeEffectiveness(victim, _currentTurn.move.type);
        if (moveEffectiveness == 0 & !_currentTurn.move.isMultiTarget)
            Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.Pokemon_name+" is immune to it!");
        else
        {
            SetMoveSequence();
            foreach (Battle_event d in _dialougeOrder)
            {
                if (_cancelMove)
                    break;
                yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
                if (!d.Condition)
                    continue;
                processingOrder = true;
                d.Execute();
                yield return new WaitUntil(() => !processingOrder);
                yield return new WaitUntil(() => !_moveDelay);
                yield return new WaitUntil(() => !Turn_Based_Combat.Instance.levelEventDelay);
                yield return new WaitUntil(() => !Turn_Based_Combat.Instance.faintEventDelay);
            } 
        }
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        ResetMoveUsage();
    }
    void ExecuteMoveWithSpecialEffect()
    {
        if (!_currentTurn.move.Has_effect) return;
        _moveDelay = true;
        if(!_currentTurn.move.is_Consecutive)
            Invoke(_currentTurn.move.Move_name.ToLower(), 0f);
        else
            StartCoroutine(ExecuteConsecutiveMove());
        processingOrder = false;
    }
    private bool IsInvincible(Move move,Battle_Participant currentVictim)
    {
        if (currentVictim.pokemon.CanBeDamaged || move.Move_damage == 0) return false;
        Dialogue_handler.Instance.DisplayBattleInfo(currentVictim.pokemon.Pokemon_name+" protected itself");
        if(!_currentTurn.move.isMultiTarget)
            _cancelMove = true;
        return true;
    }
    private int CheckIfCrit()
    {
        float critChance = attacker.pokemon.crit_chance;
        if (UnityEngine.Random.Range(0f, 100f) >= critChance) return 1;
        Dialogue_handler.Instance.DisplayBattleInfo("Critical Hit!");
        return 2;
    }

    private float CalculateMoveDamage(Move move,Battle_Participant currentVictim)
    {
        if (IsInvincible(move, currentVictim)) return 0;
        var critValue = CheckIfCrit();
        float damageDealt = 0;
        var levelFactor = ((attacker.pokemon.Current_level * 2) / 5f) + 2;
        var stab = 1f;
        float attackTypeValue = 0;
        float attackDefenseRatio = 0;
        var randomFactor = (float)Utility.RandomRange(85, 101) / 100;
        var typeEffectiveness = BattleOperations.GetTypeEffectiveness(currentVictim, _currentTurn.move.type);
        attackTypeValue = _currentTurn.move.isSpecial? attacker.pokemon.SP_ATK : attacker.pokemon.Attack;
        attackDefenseRatio = SetAtkDefRatio(critValue,_currentTurn.move.isSpecial,attacker,currentVictim);
        if (BattleOperations.is_Stab(attacker.pokemon, _currentTurn.move.type))
            stab = 1.5f;
        float baseDamage = (levelFactor * move.Move_damage *
                             (attackTypeValue/ move.Move_damage))/attacker.pokemon.Current_level;
        float damageModifier = critValue*stab*randomFactor*typeEffectiveness;
        damageDealt = math.trunc(damageModifier * baseDamage * attackDefenseRatio);
        if(!_currentTurn.move.is_Consecutive)
            DisplayEffectiveness(typeEffectiveness,currentVictim);
        float damageAfterBuff = OnDamageDeal?.Invoke(attacker,victim,_currentTurn.move,damageDealt) ?? damageDealt; 
        OnMoveHit?.Invoke(attacker,move.isSpecial);
        return damageAfterBuff;
    }
    private void DisplayEffectiveness(float typeEffectiveness,Battle_Participant currentVictim)
    {
        if ((int)math.trunc(typeEffectiveness) == 1) return;
        var message = "";
        if (typeEffectiveness == 0)
            message=currentVictim.pokemon.Pokemon_name+" was not affected!";
        else
            message=(typeEffectiveness > 1)?"The move is Super effective!":"The move is not very effective!";
        Dialogue_handler.Instance.DisplayBattleInfo(message);
    }
    private float SetAtkDefRatio(int crit, bool isSpecial, Battle_Participant currentAttacker, Battle_Participant currentVictim)
    {
        float atk, def;
        bool canIgnoreStages = currentAttacker.pokemon.Current_level >= currentVictim.pokemon.Current_level  && crit == 2;
        if (!isSpecial)
        {
            atk = canIgnoreStages && currentAttacker.statData.attack < currentAttacker.pokemon.Attack
                ? currentAttacker.pokemon.Attack  // Ignore debuff
                : currentAttacker.statData.attack;
            
            def = canIgnoreStages && currentVictim.statData.defense > currentVictim.pokemon.Defense
                ? currentVictim.pokemon.Defense  // Ignore buff
                : currentVictim.statData.defense;
        }
        else
        {
            atk = canIgnoreStages && currentAttacker.statData.spAtk < currentAttacker.pokemon.SP_ATK
                ? currentAttacker.pokemon.SP_ATK
                : currentAttacker.statData.spAtk;
            
            def = canIgnoreStages && currentVictim.statData.spDef > currentVictim.pokemon.SP_DEF
                ? currentVictim.pokemon.SP_DEF
                : currentVictim.statData.spDef;
        }
        return atk / def;
    }

    private void DealDamage()
    {
        if (_currentTurn.move.Has_effect)
        { processingOrder = false; return; }
        victim.pokemon.HP -= CalculateMoveDamage(_currentTurn.move, victim);
        processingOrder = false;
    } 
    private void ResetMoveUsage()
    {
        OnMoveEnd?.Invoke();
        doingMove = false;
        _cancelMove = false;
    }

    private void CheckVictimVulnerabilityToStatus()
    {
        if (_currentTurn.move.Has_effect | !_currentTurn.move.Has_status)
        { processingOrder = false; return; }
        if (victim.pokemon.Status_effect != "None")
        { 
            Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.Pokemon_name+" already has a "+victim.pokemon.Status_effect+" effect!");
            processingOrder = false;return;
        }
        if (victim.pokemon.HP <= 0){processingOrder = false; return;}
        if (!victim.pokemon.CanBeDamaged)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.Pokemon_name+" protected itself");
            processingOrder = false;return;
        }
        if (Utility.RandomRange(1, 101) < _currentTurn.move.Status_chance)
            if(_currentTurn.move.isMultiTarget)
                foreach (Battle_Participant enemy in attacker.currentEnemies)
                    HandleStatusApplication(enemy,_currentTurn.move,true);
            else{
                HandleStatusApplication(victim,_currentTurn.move,true);
            }
        processingOrder = false;
    }
    bool CheckInvalidStatusEffect(string status,string typeName,Move move)
    {
        string[] invalidCombinations = {
            "poisonpoison","badlypoisonpoison", "burnfire", "paralysiselectric", "freezeice" };
        foreach(string s in invalidCombinations)
            if ((status.Replace(" ", "") + typeName).ToLower() == s)
            {
                if(move.Move_damage==0)//if its only a status causing move
                    Dialogue_handler.Instance.DisplayBattleInfo("It failed");
                return true;
            }
        return false;
    }
    public void HandleStatusApplication(Battle_Participant currentVictim,Move move, bool displayMessage)
    {
        foreach (var type in currentVictim.pokemon.types)
            if(CheckInvalidStatusEffect(move.Status_effect, type.Type_name,move))return;
        OnStatusEffectHit?.Invoke(currentVictim,move.Status_effect);
        if(displayMessage)
            Dialogue_handler.Instance.DisplayBattleInfo(currentVictim.pokemon.Pokemon_name+" received a "+move.Status_effect+" effect!");
        ApplyStatusToVictim(currentVictim,move.Status_effect);
    }
    public void ApplyStatusToVictim(Battle_Participant participant,string status)
    {
        participant.pokemon.Status_effect = status;
        var numTurnsOfStatus = (status=="Sleep")? Utility.RandomRange(1, 5) : 0;
        participant.statusHandler.Get_statusEffect(numTurnsOfStatus);
    }

    public void ApplyStatDropImmunity(Battle_Participant participant,int numTurns)
    {
        if (!participant.isActive) return;
        participant.statusHandler.GetStatDropImmunity(numTurns);
    }
    void FlinchEnemy()
    {
        if (!_currentTurn.move.Can_flinch) {processingOrder = false;return;}
        if (!victim.pokemon.CanBeDamaged) {processingOrder = false;return;}
        if (Utility.RandomRange(1, 101) < _currentTurn.move.Status_chance)
        {
            victim.pokemon.canAttack = false;
            victim.pokemon.isFlinched=true;
        }
        processingOrder = false;
    }
    void CheckBuffOrDebuffApplicability()
    {
        if (!_currentTurn.move.is_Buff_Debuff) { processingOrder = false;return;}
        if (Utility.RandomRange(1, 101) > _currentTurn.move.Debuff_chance)
        { processingOrder = false; return;}
        var buffDebuffInfo = _currentTurn.move.Buff_Debuff;
        var buffAmount = int.Parse(buffDebuffInfo[1].ToString());
        var statName = buffDebuffInfo.Substring(2, buffDebuffInfo.Length - 2);
        var isIncreasing = (buffDebuffInfo[0] == '+');
        if (!_currentTurn.move.isSelfTargeted)
        {//affecting enemy
            if ( (_currentTurn.move.isMultiTarget & !Battle_handler.Instance.isDoubleBattle) 
                | !_currentTurn.move.isMultiTarget)
            {
                if (!victim.pokemon.CanBeDamaged | victim.pokemon.immuneToStatReduction)
                {
                    Dialogue_handler.Instance.DisplayBattleInfo(victim.pokemon.Pokemon_name + " protected itself");
                }
                else
                {
                    var data = new BuffDebuffData(victim.pokemon, statName, isIncreasing, buffAmount);
                    SelectRelevantBuffOrDebuff(data);
                }
            } 
            if(_currentTurn.move.isMultiTarget & Battle_handler.Instance.isDoubleBattle)
                StartCoroutine(MultiTargetBuff_Debuff(statName, isIncreasing, buffAmount));
        }
        else//affecting attacker
        {
            var data = new BuffDebuffData(attacker.pokemon, statName, isIncreasing, buffAmount);
            SelectRelevantBuffOrDebuff(data);
        }
        processingOrder = false;
    }

    IEnumerator MultiTargetBuff_Debuff(string stat, bool isIncreasing,int buffAmount)
    {
        foreach (Battle_Participant enemy in new List<Battle_Participant>(attacker.currentEnemies) )
        {
            if (enemy.pokemon.CanBeDamaged & !enemy.pokemon.immuneToStatReduction)
            {
                var data = new BuffDebuffData(enemy.pokemon, stat, isIncreasing,buffAmount);
                SelectRelevantBuffOrDebuff(data);
            }
            else
                Dialogue_handler.Instance.DisplayBattleInfo(enemy.pokemon.Pokemon_name + " protected itself");
            yield return new WaitUntil(()=>!Dialogue_handler.Instance.messagesLoading);
        }
    }
    public void SelectRelevantBuffOrDebuff(BuffDebuffData data)
    {
        switch (data.StatName)
        {
            case"Defense":
                data.Reciever.Defense = GetUpdatedStat(data.Reciever.Defense,data);
                break;
            case"Attack":
                data.Reciever.Attack = GetUpdatedStat(data.Reciever.Attack,data);
                break;
            case"Special Defense":
                data.Reciever.SP_DEF = GetUpdatedStat(data.Reciever.SP_DEF,data);
                break;
            case"Special Attack":
                data.Reciever.SP_ATK = GetUpdatedStat(data.Reciever.SP_ATK,data);
                break;
            case"Speed":
                data.Reciever.speed = GetUpdatedStat(data.Reciever.speed,data);
                break;
            case"Accuracy":
                data.Reciever.Accuracy = GetUpdatedStat(data.Reciever.Accuracy,data);
                break;
            case"Evasion":
                data.Reciever.Evasion = GetUpdatedStat(data.Reciever.Evasion,data);
                break;
            case"Crit":
                data.Reciever.crit_chance = GetUpdatedStat(data.Reciever.crit_chance,data);
                break;
        }
    }
    private float GetUpdatedStat(float currentStatValue, BuffDebuffData data)
    {
        BattleOperations.ChangeOrCreateBuffOrDebuff(data);
        Buff_Debuff buff = BattleOperations.SearchForBuffOrDebuff(data.Reciever, data.StatName);
        if (data.StatName == "Accuracy" | data.StatName == "Evasion")
            return math.trunc(currentStatValue * _accuracyAndEvasionLevels[buff.Stage+6]); 
        if(data.StatName=="Crit")    
            return _critLevels[buff.Stage];
        return math.trunc(currentStatValue * _statLevels[buff.Stage+6]);
    }

    List<Battle_Participant> TargetAllExceptSelf()
    {
        var allParticipants = Battle_handler.Instance.battleParticipants.ToList();
        allParticipants.RemoveAll(p => !p.isActive);
        allParticipants.RemoveAll(p => p.pokemon.Pokemon_ID == attacker.pokemon.Pokemon_ID);
        return allParticipants;
    }

    IEnumerator ExecuteConsecutiveMove()
    {
        var numRepetitions = Utility.RandomRange(1, 6);
        var numHits = 0;
        for (int i = 0; i < numRepetitions; i++)
        {
            if (!victim.pokemon.CanBeDamaged)
                break;
            if (victim.pokemon.HP <= 0) break;
            Dialogue_handler.Instance.DisplayBattleInfo("Hit "+(i+1)+"!");//remove later if added animations
            victim.pokemon.HP -= CalculateMoveDamage(_currentTurn.move,victim);
            numHits++;
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        }
        if (numHits>0)
        {
            DisplayEffectiveness(BattleOperations.GetTypeEffectiveness(victim, _currentTurn.move.type), victim);
            yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
            Dialogue_handler.Instance.DisplayBattleInfo("It hit (x" + numHits + ") times");
        }
        _moveDelay = false;
        processingOrder = false;
    } 
    void absorb()
     {
         var damage = CalculateMoveDamage(_currentTurn.move,victim);
         var healAmount = damage/ 2f;
         victim.pokemon.HP -= damage;
         attacker.pokemon.HP =( (healAmount+attacker.pokemon.HP) < attacker.pokemon.max_HP)? 
             math.trunc(math.abs(healAmount)) : attacker.pokemon.max_HP;
         Dialogue_handler.Instance.DisplayBattleInfo(attacker.pokemon.Pokemon_name+" gained health");
         _moveDelay = false;
         processingOrder = false;
     }
    void protect()
    {
        if(attacker.previousMove.Split('/')[0] == "Protect")
        {
            int numUses = int.Parse(attacker.previousMove.Split('/')[1]);
            int chance = 100;
            for (int i = 0; i < numUses; i++)
                chance /= 2;
            if (Utility.RandomRange(1, 101) <= chance) //success
                attacker.pokemon.CanBeDamaged = false;
            else
            {
                attacker.pokemon.CanBeDamaged = true;
                Dialogue_handler.Instance.DisplayBattleInfo("It failed!");
            }
        }
        else//success
          attacker.pokemon.CanBeDamaged = false;
        _moveDelay = false;
        processingOrder = false;
    }
    IEnumerator ApplyMultiTargetDamage(List<Battle_Participant> targets)
    {
        foreach (Battle_Participant enemy in targets)
            enemy.pokemon.HP -= CalculateMoveDamage(_currentTurn.move,enemy);
        yield return new WaitUntil(() => !Dialogue_handler.Instance.messagesLoading);
        _moveDelay = false;
        processingOrder = false;
    }
    void surf()
    {
        StartCoroutine(ApplyMultiTargetDamage(attacker.currentEnemies));
    }
    void earthquake()
    {
        StartCoroutine(ApplyMultiTargetDamage(TargetAllExceptSelf()));
    }
    void magnitude()
    {
        var magnitudeStrength = Utility.RandomRange(4, 11);
        var baseDamage = 10f;
        var damageIncrease = 0f;
        if(magnitudeStrength > 4)
            damageIncrease = 20f;
        baseDamage += damageIncrease * (magnitudeStrength - 4);
        if (magnitudeStrength == 10)
            baseDamage += 20f;
        Dialogue_handler.Instance.DisplayBattleInfo("Magnitude level "+magnitudeStrength);
        _currentTurn.move.Move_damage = baseDamage;
        StartCoroutine(ApplyMultiTargetDamage(TargetAllExceptSelf()));
    }
}