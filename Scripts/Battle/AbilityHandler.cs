using System;
using System.Collections.Generic;
using UnityEngine;
public class AbilityHandler : MonoBehaviour
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
        AbilityMethods.Add("pickup",pick_up);
        AbilityMethods.Add("blaze",blaze);
        AbilityMethods.Add("guts",guts);
        AbilityMethods.Add("levitate",levitate);
        AbilityMethods.Add("overgrow",overgrow);
        AbilityMethods.Add("torrent",torrent);
        AbilityMethods.Add("paralysiscombo",paralysis_combo);
        AbilityMethods.Add("arenatrap",arena_trap);
        AbilityMethods.Add("static",static_);//underscore because some ability names are c# keywords
        AbilityMethods.Add("shedskin",shed_skin);
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
    void pick_up()
    {
        if (AbilityTriggered) return;
        Battle_handler.instance.onBattleEnd += GiveItem;
        AbilityTriggered = true;
    }

    void arena_trap()
    {
        Debug.Log("triggered arena trap");
        foreach (var enemy in participant.Current_Enemies)
        {
            if(!enemy.pokemon.HasType("Flying") & enemy.pokemon.ability.abilityName!="Levitate")
                enemy.CanEscape = false;
        }
    }
    void blaze()
    {
        if (AbilityTriggered) return;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
        AbilityTriggered = true;
    }
    void guts()
    {
        if (AbilityTriggered) return;
        if (participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
        {
            BuffDebuffData buffData = new BuffDebuffData(participant.pokemon, "Attack", true, 1);
            BattleOperations.CanDisplayDialougue = false; 
            Move_handler.instance.GiveBuff_Debuff(buffData);
            AbilityTriggered = true;
        }
    }
    void levitate()
    {
        if (AbilityTriggered) return;
        Debug.Log("activated levitate");
        participant.AddtionalTypeImmunity = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/Ground");
        AbilityTriggered = true;
    }
    void overgrow()
    {
        if (AbilityTriggered) return;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
        AbilityTriggered = true;
    }
    void paralysis_combo()
    {
        if (AbilityTriggered) return;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
        AbilityTriggered = true;
    }
    void shed_skin()
    {
        Debug.Log("triggered shedskin");
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
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
        AbilityTriggered = true;
    }
    void torrent()
    {
        if (AbilityTriggered) return;
        Move_handler.instance.OnDamageDeal += IncreaseDamage;
        AbilityTriggered = true;
    }

    void GiveItem()
    {
        Debug.Log("triggered pickup");
        if (participant.pokemon.HeldItem != null) return;
        //Check level and 10% pickup chance
        if (participant.pokemon.Current_level < 5) return;
        if (Utility.Get_rand(1, 101) > 10) return;
        List<(int MinLevel, int MaxLevel, string[] Items)> ItemPool = new()
        {
            (5, 9, new[] { "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" }),
            (10, 19, new[] { "Super Potion", "Escape Rope", "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" }),
            (20, 29, new[] { "Hyper Potion", "Super Potion", "Escape Rope", "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" }),
            (30, 39, new[] { "Ether", "Full Heal", "Hyper Potion", "Super Potion", "Escape Rope", "Potion", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" }),
            (40, 49, new[] { "Rare Candy", "Full Heal", "Ether", "Hyper Potion", "Super Potion", "Escape Rope", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" }),
            (50, 59, new[] { "Rare Candy", "Full Heal", "Ether", "Revive", "Hyper Potion", "Escape Rope", "Antidote", "Awakening", "Paralyze Heal", "Burn Heal", "Ice Heal" }),
            (60, 69, new[] { "Rare Candy", "Full Heal", "Ether", "Revive", "Hyper Potion",  "Escape Rope" }),
            (70, 100, new[] { "Rare Candy", "Full Heal", "Ether", "Revive", "Hyper Potion",  "PP Up" })
        };
        string[] PossibleItems = null;
        foreach (var pool in ItemPool)
        {
            if (participant.pokemon.Current_level >= pool.MinLevel && participant.pokemon.Current_level <= pool.MaxLevel)
            {
                PossibleItems = pool.Items;
                break;
            }
        }
        if (PossibleItems != null)
        {
            int ItemWonIndex = Utility.Get_rand(0, PossibleItems.Length);
            Item ItemWon = Resources.Load<Item>("Assets/Save_data/Items/" + PossibleItems[ItemWonIndex]);
            if (Utility.Get_rand(1, 101) < participant.pokemon.Current_level)
                participant.pokemon.HeldItem = Obj_Instance.set_Item(ItemWon);
        }

    }
    void GiveStatic(Battle_Participant attacker,Battle_Participant victim,bool isSpecialMove)
    {
        Debug.Log("triggered static");
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
        Debug.Log("upped Damage with "+pkm_ability);
        if (pkm_ability == "swarm" & (participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f)) & move.type.Type_name=="Bug")
            return damage*1.5f;
        if (pkm_ability == "paralysiscombo")
        {
            if (victim.pokemon.Status_effect=="Paralysis")
                return damage*2;
        }
        if (pkm_ability == "torrent" & participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
            if (move.type.Type_name == "Water")
                return damage*1.5f;
        if (pkm_ability == "overgrow" & participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
            if (move.type.Type_name == "Grass")
                return damage*1.5f;
        if (pkm_ability == "blaze" & participant.pokemon.HP < (participant.pokemon.max_HP * 0.33f))
            if (move.type.Type_name == "Fire")
                return damage*1.5f;
        return damage;
    }
}
