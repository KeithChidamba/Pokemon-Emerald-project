using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Abilities : MonoBehaviour
{
    public Battle_Participant participant;
    public event Action OnAbilityUsed;
    private Ability CurrentAbility;
    private bool AbilityTriggered;
    private string pkm_ability;
    private Dictionary<string, Action> AbilityMethods = new ();
    private void Start()
    {
        Turn_Based_Combat.instance.OnNewTurn += CheckAbilityUsage;
        AbilityMethods.Add("pickup",pickup);
        AbilityMethods.Add("blaze",blaze);
        AbilityMethods.Add("guts",guts);
        AbilityMethods.Add("levitate",levitate);
        AbilityMethods.Add("overgrow",overgrow);
        AbilityMethods.Add("torrent",torrent);
        AbilityMethods.Add("paralysiscombo",paralysiscombo);
        AbilityMethods.Add("sandpit",sandpit);
        AbilityMethods.Add("static",static_);//underscore because some ability names are c# keywords
        AbilityMethods.Add("shedskin",shedskin);
        AbilityMethods.Add("swarm",swarm);
    }

    void CheckAbilityUsage()
    {
        OnAbilityUsed?.Invoke();
    }

    public void Set_ability()
    {
        CurrentAbility = participant.pokemon.ability;
        pkm_ability = CurrentAbility.abilityName.ToLower().Replace(" ","");
        if (AbilityMethods.TryGetValue(pkm_ability, out Action ability))
            OnAbilityUsed += ability;
        else
            Console.WriteLine($"Ability '{pkm_ability}' not found!");
        AbilityTriggered = false;
    }

    public void ResetState()
    {
        OnAbilityUsed = null;
        AbilityTriggered = false;
        Move_handler.instance.OnMoveHit -= GiveStatic;
        Battle_handler.instance.onBattleEnd -= GiveItem;
        Move_handler.instance.OnDamageDeal -= IncreaseDamage;
    }
    void pickup()
    {
        if (AbilityTriggered) return;
        if (participant.pokemon.HeldItem == null)
        {
            Battle_handler.instance.onBattleEnd += GiveItem;
            AbilityTriggered = true;
        }
    }
    void blaze()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
    }
    void guts()
    {
        if (AbilityTriggered) return;
        if (participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
        {
            BuffDebuffData buffData = new BuffDebuffData(participant.pokemon, "Attack", true, 1);
            Move_handler.instance.GiveBuff_Debuff(buffData);
            AbilityTriggered = true;
        }
    }
    void levitate()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        participant.AddtionalTypeImmunity = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/Ground");
    }
    void overgrow()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
    }
    void paralysiscombo()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
    }
    void sandpit()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
    }
    void shedskin()
    {
        if (participant.pokemon.Status_effect == "None") return;
        if (Utility.Get_rand(1, 4) < 2)
        {
            participant.pokemon.Status_effect = "None";
            if (participant.pokemon.Status_effect == "sleep" | participant.pokemon.Status_effect == "freeze"| participant.pokemon.Status_effect == "paralysis")
                participant.pokemon.canAttack = true;
        }
    }
    void static_()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        Move_handler.instance.OnMoveHit += GiveStatic;
    }
    
    void swarm()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
    }
    void torrent()
    {
        if (AbilityTriggered) return;
        AbilityTriggered = true;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
    }

    void GiveItem()
    {
        if(Utility.Get_rand(1,101)<10)
            participant.pokemon.HeldItem=Obj_Instance.set_Item(participant.Current_Enemies[0].pokemon.HeldItem);
    }
    void GiveStatic(Battle_Participant attacker,Battle_Participant victim,bool isSpecialMove)
    {
        if (victim.pokemon.Status_effect != "None") return;
        if (!victim.pokemon.CanBeDamaged)
            return;
        if(isSpecialMove)return;
        Move PlaceholderMove = new Move();
        PlaceholderMove.Status_effect = "Paralysis";
        Move_handler.instance.CheckStatus(attacker, PlaceholderMove);
    }
    float IncreaseDamage(Battle_Participant attacker,Battle_Participant victim,Move move, float damage)
    {
        if (pkm_ability == "swarm")
            return damage+(damage/2f);
        if (pkm_ability == "paralysiscombo")
        {
            Type electric = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/Electric");
            if (victim.pokemon.types.Contains(electric))
                return damage*2;
        }
        if (pkm_ability == "torrent")
            if (move.type.Type_name == "Water")
                return damage*1.5f;
        if (pkm_ability == "overgrow")
            if (move.type.Type_name == "Grass")
                return damage*1.5f;
        if (pkm_ability == "blaze")
            if (move.type.Type_name == "Fire")
                return damage*1.5f;
        return damage;
    }

}
