using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour
{
    public bool Doing_move = false;
    public static Move_handler instance;
    private Turn current_turn;
    [SerializeField]private Battle_Participant attacker_;
    [SerializeField]private Battle_Participant victim_;
    private readonly float[] Stat_Levels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] Accuracy_And_Evasion_Levels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] Crit_Levels = {6.25f,12.5f,25f,50f};
    private Battle_event[] Dialouge_order={null,null,null,null,null};
    private bool MoveDelay = false;
    private bool CancelMove = false;
    public bool ProcessingOrder = false;
    public event Action OnMoveEnd;
    public event Func<Battle_Participant,Battle_Participant,Move,float,float> OnDamageDeal;
    public event Action<Battle_Participant,bool> OnMoveHit;
    public event Action<Battle_Participant,string> OnStatusEffectHit;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Do_move(Turn turn)
    {
        current_turn = turn;
        attacker_ = Battle_handler.Instance.battleParticipants[turn.attackerIndex];
        victim_ = Battle_handler.Instance.battleParticipants[turn.victimIndex];
        if (current_turn.move_.isMultiTarget || current_turn.move_.isSelfTargeted)
            Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" used "+current_turn.move_.Move_name+"!");
        else
            Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" used "+current_turn.move_.Move_name+" on "+victim_.pokemon.Pokemon_name+"!");
        StartCoroutine(Move_Sequence());
    }
    void Set_Sequences()
    {
        Dialouge_order[0] = new(Deal_Damage, current_turn.move_.Move_damage > 0);
        Dialouge_order[1] = new(Get_status, current_turn.move_.Has_status);
        Dialouge_order[2] = new(Move_effect, current_turn.move_.Has_effect);
        Dialouge_order[3] = new(Set_buff_Debuff, current_turn.move_.is_Buff_Debuff);
        Dialouge_order[4] = new(flinch_enemy, current_turn.move_.Can_flinch);
    }
    IEnumerator Move_Sequence()
    {
        float MoveEffectiveness = BattleOperations.TypeEffectiveness(victim_, current_turn.move_.type);
        if (MoveEffectiveness == 0 & !current_turn.move_.isMultiTarget)
            Dialogue_handler.instance.Battle_Info(victim_.pokemon.Pokemon_name+" is immune to it!");
        else
        {
            Set_Sequences();
            foreach (Battle_event d in Dialouge_order)
            {
                if (CancelMove)
                    break;
                yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
                if (!d.Condition)
                    continue;
                ProcessingOrder = true;
                d.Execute();
                yield return new WaitUntil(() => !ProcessingOrder);
                yield return new WaitUntil(() => !MoveDelay);
                yield return new WaitUntil(() => !Turn_Based_Combat.instance.LevelEventDelay);
                yield return new WaitUntil(() => !Turn_Based_Combat.instance.FaintEventDelay);
            } 
        }
        yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        Move_done();
    }
    void Move_effect()
    {
        if (!current_turn.move_.Has_effect) return;
        MoveDelay = true;
        if(!current_turn.move_.is_Consecutive)
            Invoke(current_turn.move_.Move_name.ToLower(), 0f);
        else
            StartCoroutine(ConsecutiveMove());
        ProcessingOrder = false;
    }
    float Calc_Damage(Move move,Battle_Participant CurrentVictim)
    {
        if (!CurrentVictim.pokemon.CanBeDamaged & move.Move_damage > 0)
        {
             Dialogue_handler.instance.Battle_Info(CurrentVictim.pokemon.Pokemon_name+" protected itself");
             if(!current_turn.move_.isMultiTarget)
                CancelMove = true;
             return 0f;
        }
        if (move.Move_damage == 0) return 0f;
       
        int crit = 1;
        if (Utility.RandomRange(1, (int)(100 / attacker_.pokemon.crit_chance) + 1) < 2)
        {
            crit = 2;
            Dialogue_handler.instance.Battle_Info("Critical Hit!");
        }
        float damage_dealt;
        float level_factor = ((attacker_.pokemon.Current_level * 2) / 5f) + 2;
        float Stab = 1f;
        float Attack_type = 0;
        float atk_def_ratio;
        float random_factor = (float)Utility.RandomRange(85, 101) / 100;
        float type_effectiveness = BattleOperations.TypeEffectiveness(CurrentVictim, current_turn.move_.type);
        if (current_turn.move_.isSpecial)
            Attack_type = attacker_.pokemon.SP_ATK;
        else
            Attack_type = attacker_.pokemon.Attack;
        atk_def_ratio = SetAtkDefRatio(crit,current_turn.move_.isSpecial,attacker_,CurrentVictim);
        if (BattleOperations.is_Stab(attacker_.pokemon, current_turn.move_.type))
            Stab = 1.5f;
        float base_Damage = (level_factor * move.Move_damage *
                             (Attack_type/ move.Move_damage))/attacker_.pokemon.Current_level;
        float Modifier = crit*Stab*random_factor*type_effectiveness;
        damage_dealt = math.trunc(Modifier * base_Damage * atk_def_ratio);
        if(!current_turn.move_.is_Consecutive)
            GetEffectiveness(type_effectiveness,CurrentVictim);
        float DamageAfterBuff = OnDamageDeal?.Invoke(attacker_,victim_,current_turn.move_,damage_dealt) ?? damage_dealt; 
        OnMoveHit?.Invoke(attacker_,move.isSpecial);
        return DamageAfterBuff;
    }
    void GetEffectiveness(float type_effectiveness,Battle_Participant victim)
    {
        if (type_effectiveness > 1)
            Dialogue_handler.instance.Battle_Info("The move is Super effective!");
        else if (type_effectiveness == 0)
            Dialogue_handler.instance.Battle_Info(victim.pokemon.Pokemon_name+" was not affected!");
        else if(type_effectiveness < 1)
            Dialogue_handler.instance.Battle_Info("The move is not very effective!");
    }
    float SetAtkDefRatio(int crit, bool isSpecial, Battle_Participant attacker, Battle_Participant victim)
    {
        float atk, def;
        bool canIgnoreStages = attacker.pokemon.Current_level >= victim.pokemon.Current_level  && crit == 2;
        if (!isSpecial)
        {
            atk = canIgnoreStages && attacker.statData.attack < attacker.pokemon.Attack
                ? attacker.pokemon.Attack  // Ignore debuff
                : attacker.statData.attack;
            
            def = canIgnoreStages && victim.statData.defense > victim.pokemon.Defense
                ? victim.pokemon.Defense  // Ignore buff
                : victim.statData.defense;
        }
        else
        {
            atk = canIgnoreStages && attacker.statData.spAtk < attacker.pokemon.SP_ATK
                ? attacker.pokemon.SP_ATK
                : attacker.statData.spAtk;
            
            def = canIgnoreStages && victim.statData.spDef > victim.pokemon.SP_DEF
                ? victim.pokemon.SP_DEF
                : victim.statData.spDef;
        }
        return atk / def;
    }

    void Deal_Damage()
    {
        if (current_turn.move_.Has_effect)
        { ProcessingOrder = false; return; }
        victim_.pokemon.HP -= Calc_Damage(current_turn.move_, victim_);
        ProcessingOrder = false;
    } 
    void Move_done()
    {
        OnMoveEnd?.Invoke();
        Doing_move = false;
        CancelMove = false;
    }

    void Get_status()
    {
        if (current_turn.move_.Has_effect | !current_turn.move_.Has_status)
        { ProcessingOrder = false; return; }
        if (victim_.pokemon.Status_effect != "None")
        { 
            Dialogue_handler.instance.Battle_Info(victim_.pokemon.Pokemon_name+" already has a "+victim_.pokemon.Status_effect+" effect!");
            ProcessingOrder = false;return;
        }
        if (victim_.pokemon.HP <= 0){ProcessingOrder = false; return;}
        if (!victim_.pokemon.CanBeDamaged)
        {
            Dialogue_handler.instance.Battle_Info(victim_.pokemon.Pokemon_name+" protected itself");
            ProcessingOrder = false;return;
        }
        if (Utility.RandomRange(1, 101) < current_turn.move_.Status_chance)
            if(current_turn.move_.isMultiTarget)
                foreach (Battle_Participant enemy in attacker_.currentEnemies)
                    CheckStatus(enemy,current_turn.move_);
            else{
                CheckStatus(victim_,current_turn.move_);
            }
        ProcessingOrder = false;
    }
    bool CheckInvalidStatusEffect(string status,string type_name,Move move)
    {
        string[] InvalidCombinations = {
            "poisonpoison","badlypoisonpoison", "burnfire", "paralysiselectric", "freezeice" };
        foreach(string s in InvalidCombinations)
            if ((status.Replace(" ", "") + type_name).ToLower() == s)
            {
                if(move.Move_damage==0)//if its only a status causing move
                    Dialogue_handler.instance.Battle_Info("It failed");
                return true;
            }
        return false;
    }

    public void CheckStatus(Battle_Participant victim,Move move)
    {
        foreach (Type t in victim.pokemon.types)
            if(CheckInvalidStatusEffect(move.Status_effect, t.Type_name,move))return;
        OnStatusEffectHit?.Invoke(victim,move.Status_effect);
        Dialogue_handler.instance.Battle_Info(victim.pokemon.Pokemon_name+" received a "+move.Status_effect+" effect!");
        Set_Status(victim,move.Status_effect);
    }
    public void Set_Status(Battle_Participant p,String Status)
    {
        p.pokemon.Status_effect = Status;
        int num_turns = (Status=="Sleep")? Utility.RandomRange(1, 5) : 0;
        p.statusHandler.Get_statusEffect(num_turns);
    }
    void flinch_enemy()
    {
        if (!current_turn.move_.Can_flinch) {ProcessingOrder = false;return;}
        if (!victim_.pokemon.CanBeDamaged) {ProcessingOrder = false;return;}
        if (Utility.RandomRange(1, 101) < current_turn.move_.Status_chance)
        {
            victim_.pokemon.canAttack = false;
            victim_.pokemon.isFlinched=true;
        }
        ProcessingOrder = false;
    }
    void Set_buff_Debuff()
    {
        if (!current_turn.move_.is_Buff_Debuff) { ProcessingOrder = false;return;}
        if (Utility.RandomRange(1, 101) > current_turn.move_.Debuff_chance)
        { ProcessingOrder = false; return;}
        string buffDebuffInfo = current_turn.move_.Buff_Debuff;
        int buff_amount = int.Parse(buffDebuffInfo[1].ToString());
        string stat = buffDebuffInfo.Substring(2, buffDebuffInfo.Length - 2);
        bool isIncreasing = (buffDebuffInfo[0] == '+');//buff or debuff
        if (!current_turn.move_.isSelfTargeted)
        {//affecting enemy
            if(!current_turn.move_.isMultiTarget | !Battle_handler.Instance.isDoubleBattle)
            {
                if (!victim_.pokemon.CanBeDamaged)
                    Dialogue_handler.instance.Battle_Info(victim_.pokemon.Pokemon_name + " protected itself");
                else
                {
                    BuffDebuffData buff_debuff = new BuffDebuffData(victim_.pokemon, stat, isIncreasing, buff_amount);
                    GiveBuff_Debuff(buff_debuff);
                }
            }
            else
                StartCoroutine(MultiTargetBuff_Debuff(stat, isIncreasing, buff_amount));
        }
        else//affecting attacker
        {
            BuffDebuffData buff_debuff = new BuffDebuffData(attacker_.pokemon, stat, isIncreasing, buff_amount);
            GiveBuff_Debuff(buff_debuff);
        }
        
    }

    IEnumerator MultiTargetBuff_Debuff(string stat, bool isIncreasing,int buff_amount)
    {
        foreach (Battle_Participant enemy in new List<Battle_Participant>(attacker_.currentEnemies) )
        {
            if (enemy.pokemon.CanBeDamaged)
            {
                BuffDebuffData buff_debuff = new BuffDebuffData(enemy.pokemon, stat, isIncreasing,buff_amount);
                GiveBuff_Debuff(buff_debuff);
            }
            else
                Dialogue_handler.instance.Battle_Info(enemy.pokemon.Pokemon_name + " protected itself");
            yield return new WaitUntil(()=>!Dialogue_handler.instance.messagesLoading);
        }
    }
    public void GiveBuff_Debuff(BuffDebuffData data)
    {
        switch (data.StatName)
        {
            case"Defense":
                data.Reciever.Defense = Get_buff_debuff(data.Reciever.Defense,data);
                break;
            case"Attack":
                data.Reciever.Attack = Get_buff_debuff(data.Reciever.Attack,data);
                break;
            case"Special Defense":
                data.Reciever.SP_DEF = Get_buff_debuff(data.Reciever.SP_DEF,data);
                break;
            case"Special Attack":
                data.Reciever.SP_ATK = Get_buff_debuff(data.Reciever.SP_ATK,data);
                break;
            case"Speed":
                data.Reciever.speed = Get_buff_debuff(data.Reciever.speed,data);
                break;
            case"Accuracy":
                data.Reciever.Accuracy = Get_buff_debuff(data.Reciever.Accuracy,data);
                break;
            case"Evasion":
                data.Reciever.Evasion = Get_buff_debuff(data.Reciever.Evasion,data);
                break;
            case"Crit":
                data.Reciever.crit_chance = Get_buff_debuff(data.Reciever.crit_chance,data);
                break;
        }
    }
    private float Get_buff_debuff(float stat_val, BuffDebuffData data)
    {
        BattleOperations.ChangeBuffs(data);
        Buff_Debuff buff = BattleOperations.GetBuff(data.Reciever, data.StatName);
        if (data.StatName == "Accuracy" | data.StatName == "Evasion")
            return math.trunc(stat_val * Accuracy_And_Evasion_Levels[buff.Stage+6]); 
        if(data.StatName=="Crit")    
            return Crit_Levels[buff.Stage];
        return math.trunc(stat_val * Stat_Levels[buff.Stage+6]);
    }

    List<Battle_Participant> SetTargets()
    {
        List<Battle_Participant> OtherParticipants = Battle_handler.Instance.battleParticipants.ToList();
        OtherParticipants = Battle_handler.Instance.battleParticipants.ToList().Where(p =>
            p.isActive).ToList();
        OtherParticipants.RemoveAll(p => p.pokemon.Pokemon_ID == attacker_.pokemon.Pokemon_ID);
        return OtherParticipants;
    }

    IEnumerator ConsecutiveMove()
    {
        int NumRepetitions = Utility.RandomRange(1, 6);
        int NumHits = 0;
        for (int i = 0; i < NumRepetitions; i++)
        {
            if (!victim_.pokemon.CanBeDamaged)
                break;
            if (victim_.pokemon.HP <= 0) break;
            Dialogue_handler.instance.Battle_Info("Hit "+(i+1)+"!");//remove later if added animations
            victim_.pokemon.HP -= Calc_Damage(current_turn.move_,victim_);
            NumHits++;
            yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        }
        if (NumHits>0)
        {
            GetEffectiveness(BattleOperations.TypeEffectiveness(victim_, current_turn.move_.type), victim_);
            yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
            Dialogue_handler.instance.Battle_Info("It hit (x" + NumHits + ") times");
        }
        MoveDelay = false;
        ProcessingOrder = false;
    } 
    void absorb()
     {
         float damage = Calc_Damage(current_turn.move_,victim_);
         float heal_amount = damage/ 2f;
         victim_.pokemon.HP -= damage;
         if( (heal_amount+attacker_.pokemon.HP) < attacker_.pokemon.max_HP)
             attacker_.pokemon.HP += math.trunc(math.abs(heal_amount));
         else
             attacker_.pokemon.HP = attacker_.pokemon.max_HP;
         Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" gained health");
         MoveDelay = false;
         ProcessingOrder = false;
     }
    void protect()
    {
        if(attacker_.previousMove.Split('/')[0] == "Protect")
        {
            int numUses = int.Parse(attacker_.previousMove.Split('/')[1]);
            int chance = 100;
            for (int i = 0; i < numUses; i++)
                chance /= 2;
            if (Utility.RandomRange(1, 101) <= chance) //success
                attacker_.pokemon.CanBeDamaged = false;
            else
            {
                attacker_.pokemon.CanBeDamaged = true;
                Dialogue_handler.instance.Battle_Info("It failed!");
            }
        }
        else//success
          attacker_.pokemon.CanBeDamaged = false;
        MoveDelay = false;
        ProcessingOrder = false;
    }
    IEnumerator MultiTargetMoveDamage(List<Battle_Participant> Targets)
    {
        foreach (Battle_Participant enemy in Targets)
            enemy.pokemon.HP -= Calc_Damage(current_turn.move_,enemy);
        yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
        MoveDelay = false;
        ProcessingOrder = false;
    }
    void surf()
    {
        StartCoroutine(MultiTargetMoveDamage(attacker_.currentEnemies));
    }
    void earthquake()
    {
        StartCoroutine(MultiTargetMoveDamage(SetTargets()));
    }
    void magnitude()
    {
        int MagnitudeStrength = Utility.RandomRange(4, 11);
        float baseDamage = 10f;
        float DamageIncrease = 0f;
        if(MagnitudeStrength > 4)
            DamageIncrease = 20f;
        baseDamage += DamageIncrease * (MagnitudeStrength - 4);
        if (MagnitudeStrength == 10)
            baseDamage += 20f;
        Dialogue_handler.instance.Battle_Info("Magnitude level "+MagnitudeStrength);
        current_turn.move_.Move_damage = baseDamage;
        StartCoroutine(MultiTargetMoveDamage(SetTargets()));
    }
}