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
    public event Action OnMoveEnd;
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
        attacker_ = Battle_handler.instance.Battle_Participants[turn.attackerIndex];
        victim_ = Battle_handler.instance.Battle_Participants[turn.victimIndex];
        if (current_turn.move_.isMultiTarget || current_turn.move_.isSelfTargeted)
            Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" used "+current_turn.move_.Move_name+"!");
        else
            Dialogue_handler.instance.Battle_Info(attacker_.pokemon.Pokemon_name+" used "+current_turn.move_.Move_name+" on "+victim_.pokemon.Pokemon_name+"!");
        StartCoroutine(Move_Sequence());
    }
    void Set_Sequences()
    {
        Dialouge_order[0] = new("Deal_Damage", current_turn.move_.Move_damage > 0,1.5f);
        Dialouge_order[1] = new("Get_status", current_turn.move_.Has_status,1.5f);
        Dialouge_order[2] = new("Move_effect", current_turn.move_.Has_effect,2f);
        Dialouge_order[3] = new("Set_buff_Debuff", current_turn.move_.is_Buff_Debuff,1.5f);
        Dialouge_order[4] = new("flinch_enemy", current_turn.move_.Can_flinch,1f);
    }
    IEnumerator Move_Sequence()
    {
        float MoveEffectiveness = BattleOperations.TypeEffectiveness(victim_.pokemon, current_turn.move_.type);
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
                d.Execute();
                if (d.Condition)
                    yield return new WaitForSeconds(d.duration);
                yield return new WaitUntil(() => !MoveDelay);
                yield return new WaitUntil(() => !Turn_Based_Combat.instance.LevelEventDelay);
                yield return new WaitUntil(() => !Turn_Based_Combat.instance.FainEventDelay);
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
        if (Utility.Get_rand(1, (int)(100 / attacker_.pokemon.crit_chance) + 1) < 2)
        {
            crit = 2;
            Dialogue_handler.instance.Battle_Info("Critical Hit!");
        }
        float damage_dealt;
        float level_factor = ((attacker_.pokemon.Current_level * 2) / 5f) + 2;
        float Stab = 1f;
        float Attack_type = 0;
        float atk_def_ratio;
        float random_factor = (float)Utility.Get_rand(85, 101) / 100;
        float type_effectiveness = BattleOperations.TypeEffectiveness(CurrentVictim.pokemon, current_turn.move_.type);
        if (current_turn.move_.isSpecial)
            Attack_type = attacker_.pokemon.SP_ATK;
        else
            Attack_type = attacker_.pokemon.Attack;
        atk_def_ratio = Set_AtkDefRatio(crit,Attack_type,current_turn.move_.isSpecial,CurrentVictim);
        if (BattleOperations.is_Stab(attacker_.pokemon, current_turn.move_.type))
            Stab = 1.5f;
        float base_Damage = (level_factor * move.Move_damage *
                             (Attack_type/ move.Move_damage))/attacker_.pokemon.Current_level;
        float Modifier = crit*Stab*random_factor*type_effectiveness;
        damage_dealt = math.trunc(Modifier * base_Damage * atk_def_ratio);
        if(!current_turn.move_.is_Consecutive)
            GetEffectiveness(type_effectiveness,CurrentVictim);
        return damage_dealt;
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
    float Set_AtkDefRatio(float crit,float AttackType,bool isSpecial,Battle_Participant victim)
    {
        float Ratio=0;
        float def = AttackType/victim.pokemon.Defense;
        float spDef =  AttackType/victim.pokemon.SP_DEF;
        if (crit == 1)//if no crit
            if (!isSpecial)
                Ratio = def;
            else
                Ratio = spDef;
        else if (crit == 2)//if crit, ignore enemy defense buff, and attacker attack debuff
        {
            if (!isSpecial)
                if (victim.data.Defense < victim.pokemon.Defense) //def buff
                    Ratio = AttackType / victim.data.Defense;
                else
                    Ratio = def;
            else
                if (victim.data.SP_DEF < victim.pokemon.SP_DEF) //def buff
                    Ratio = AttackType / victim.data.SP_DEF;
                else
                    Ratio = spDef;
        }
        return Ratio;
    }
    void Deal_Damage()
    {
        if (current_turn.move_.Has_effect) return;
        victim_.pokemon.HP -= Calc_Damage(current_turn.move_,victim_);
    } 
    void Move_done()
    {
        OnMoveEnd?.Invoke();
        Doing_move = false;
        CancelMove = false;
    }
    void Get_status()
    {
        if (current_turn.move_.Has_effect) return;
        if (!current_turn.move_.Has_status)return;
        if (victim_.pokemon.Status_effect != "None") return;
        if (victim_.pokemon.HP <= 0) return;
        if (!victim_.pokemon.CanBeDamaged)
        {
            Dialogue_handler.instance.Battle_Info(victim_.pokemon.Pokemon_name+" protected itself");
            return;
        }
        if (Utility.Get_rand(1, 101) < current_turn.move_.Status_chance)
            if(current_turn.move_.isMultiTarget)
                foreach (Battle_Participant enemy in attacker_.Current_Enemies)
                    CheckStatus(enemy);
            else{
                CheckStatus(victim_);
            }
    }
    bool CheckInvalidStatusEffect(string status,string type_name)
    {
        string[] InvalidCombinations = {
            "poisonpoison","badlypoisonpoison", "burnfire", "paralysiselectric", "freezeice" };
        foreach(string s in InvalidCombinations)
            if ((status.Replace(" ", "") + type_name).ToLower() == s)
            {
                if(current_turn.move_.Move_damage==0)//if its only a status causing move
                    Dialogue_handler.instance.Battle_Info("It failed");
                return true;
            }
        return false;
    }

    void CheckStatus(Battle_Participant victim)
    {
        foreach (Type t in victim.pokemon.types)
            if(CheckInvalidStatusEffect(current_turn.move_.Status_effect, t.Type_name))return;
        Dialogue_handler.instance.Battle_Info(victim.pokemon.Pokemon_name+" recieved a "+current_turn.move_.Status_effect+" effect!");
        Set_Status(victim,current_turn.move_.Status_effect);
    }
    public void Set_Status(Battle_Participant p,String Status)
    {
        p.pokemon.Status_effect = Status;
        int num_turns = 0;
        if(Status=="Sleep") 
            num_turns = Utility.Get_rand(1, 5);
        p.status.Get_statusEffect(num_turns);
    }
    void flinch_enemy()
    {
        if (!current_turn.move_.Can_flinch) return;
        if (!victim_.pokemon.CanBeDamaged) return;
        if (Utility.Get_rand(1, 101) < current_turn.move_.Status_chance)
        {
            victim_.pokemon.canAttack = false;
            victim_.pokemon.isFlinched=true;
        }
    }
    void Set_buff_Debuff()
    {
        if (!current_turn.move_.is_Buff_Debuff) return;
        if (Utility.Get_rand(1, 101) > current_turn.move_.Debuff_chance)
            return;
        string buffDebuffInfo = current_turn.move_.Buff_Debuff;
        int buff_amount = int.Parse(buffDebuffInfo[1].ToString());
        string stat = buffDebuffInfo.Substring(2, buffDebuffInfo.Length - 2);
        bool isIncreasing = (buffDebuffInfo[0] == '+');//buff or debuff
        if (!current_turn.move_.isSelfTargeted)
        {//affecting enemy
            if(!current_turn.move_.isMultiTarget | !Battle_handler.instance.isDouble_battle)
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
        foreach (Battle_Participant enemy in attacker_.Current_Enemies )
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
                data.Reciever.Defense = Get_buff_debuff(victim_.pokemon.Defense,data);
                break;
            case"Attack":
                data.Reciever.Attack = Get_buff_debuff(victim_.pokemon.Attack,data);
                break;
            case"Special Defense":
                data.Reciever.SP_DEF = Get_buff_debuff(victim_.pokemon.SP_DEF,data);
                break;
            case"Special Attack":
                data.Reciever.SP_ATK = Get_buff_debuff(victim_.pokemon.SP_ATK,data);
                break;
            case"Speed":
                data.Reciever.speed = Get_buff_debuff(victim_.pokemon.speed,data);
                break;
            case"Accuracy":
                data.Reciever.Accuracy = Get_buff_debuff(victim_.pokemon.Accuracy,data);
                break;
            case"Evasion":
                data.Reciever.Evasion = Get_buff_debuff(victim_.pokemon.Evasion,data);
                break;
            case"Crit":
                data.Reciever.crit_chance = Get_buff_debuff(victim_.pokemon.crit_chance,data);
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
        List<Battle_Participant> OtherParticipants = Battle_handler.instance.Battle_Participants.ToList();
        OtherParticipants = Battle_handler.instance.Battle_Participants.ToList().Where(p =>
            p.is_active).ToList();
        OtherParticipants.RemoveAll(p => p.pokemon.Pokemon_ID == attacker_.pokemon.Pokemon_ID);
        return OtherParticipants;
    }
    void MultiTargetMoveDamage(List<Battle_Participant> Targets)
    {
        foreach (Battle_Participant enemy in Targets)
            enemy.pokemon.HP -= Calc_Damage(current_turn.move_,enemy);
    }
    IEnumerator ConsecutiveMove()
    {
        int NumRepetitions = Utility.Get_rand(1, 6);
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
            GetEffectiveness(BattleOperations.TypeEffectiveness(victim_.pokemon, current_turn.move_.type), victim_);
            yield return new WaitUntil(() => !Dialogue_handler.instance.messagesLoading);
            Dialogue_handler.instance.Battle_Info("It hit (x" + NumHits + ") times");
        }
        MoveDelay = false;
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
     }
    void protect()
    {
        if(attacker_.previousMove.Split('/')[0] == "Protect")
        {
            int numUses = int.Parse(attacker_.previousMove.Split('/')[1]);
            int chance = 100;
            for (int i = 0; i < numUses; i++)
                chance /= 2;
            if (Utility.Get_rand(1, 101) <= chance) //success
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
    }

    void surf()
    {
        MultiTargetMoveDamage(attacker_.Current_Enemies);
        MoveDelay = false;
    }
    void earthquake()
    {
        MultiTargetMoveDamage(SetTargets());
        MoveDelay = false;
    }
    void magnitude()
    {
        int MagnitudeStrength = Utility.Get_rand(4, 11);
        float baseDamage = 10f;
        float DamageIncrease = 0f;
        if(MagnitudeStrength > 4)
            DamageIncrease = 20f;
        baseDamage += DamageIncrease * (MagnitudeStrength - 4);
        if (MagnitudeStrength == 10)
            baseDamage += 20f;
        Dialogue_handler.instance.Battle_Info("Magnitude level "+MagnitudeStrength);
        current_turn.move_.Move_damage = baseDamage;
        MultiTargetMoveDamage(SetTargets());
        MoveDelay = false;
    }
}