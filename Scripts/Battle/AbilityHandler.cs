using System;
using System.Collections.Generic;
using UnityEngine;

public class AbilityHandler : MonoBehaviour
{
    private Battle_Participant _participant;
    public event Action OnAbilityUsed;
    private bool _abilityTriggered;
    private string _currentAbility;
    private readonly Dictionary<string, Action> _abilityMethods = new ();
    private void Start()
    {
        _participant =  GetComponent<Battle_Participant>();
        Turn_Based_Combat.Instance.OnMoveExecute += CheckAbilityUsability;
        _abilityMethods.Add("pickup",PickUp);
        _abilityMethods.Add("blaze",Blaze);
        _abilityMethods.Add("guts",Guts);
        _abilityMethods.Add("levitate",Levitate);
        _abilityMethods.Add("overgrow",Overgrow);
        _abilityMethods.Add("torrent",Torrent);
        _abilityMethods.Add("paralysiscombo",ParalysisCombo);
        _abilityMethods.Add("arenatrap",arena_trap);
        _abilityMethods.Add("static",Static);//underscore because some ability names are c# keywords
        _abilityMethods.Add("shedskin",ShedSkin);
        _abilityMethods.Add("swarm",Swarm);
    }

    void CheckAbilityUsability()
    {
        if(!_participant.fainted)
            OnAbilityUsed?.Invoke();
    }

    public void SetAbilityMethod()
    {
        _currentAbility = _participant.pokemon.ability.abilityName.ToLower().Replace(" ","");
        if (_abilityMethods.TryGetValue(_currentAbility, out Action abilityMethod))
            OnAbilityUsed += abilityMethod;
        else
            Console.WriteLine($"Ability '{_currentAbility}' not found!");
        _abilityTriggered = false;
    }

    public void ResetState()
    {
        OnAbilityUsed = null;
        _abilityTriggered = false;
        Move_handler.Instance.OnMoveHit -= GiveStatic;
        Battle_handler.Instance.OnBattleEnd -= GiveItem;
        Move_handler.Instance.OnDamageDeal -= IncreaseDamage;
        Move_handler.Instance.OnStatusEffectHit -= HealStatusEffect;
    }
    void PickUp()
    {
        if (_abilityTriggered) return;
        Battle_handler.Instance.OnBattleEnd += GiveItem;
        _abilityTriggered = true;
    }

    void arena_trap()
    {
        Debug.Log("triggered arena trap");
        foreach (var enemy in _participant.currentEnemies)
        {
            if(!enemy.pokemon.HasType("Flying") & enemy.pokemon.ability.abilityName!="Levitate")
                enemy.canEscape = false;
        }
    }
    void Blaze()
    {
        if (_abilityTriggered) return;
        Move_handler.Instance.OnDamageDeal += IncreaseDamage;
        _abilityTriggered = true;
    }
    void Guts()
    {
        if (_abilityTriggered) return;
        if (_participant.pokemon.HP < (_participant.pokemon.max_HP * 0.33f))
        {
            BuffDebuffData AttackBuffData = new BuffDebuffData(_participant.pokemon, "Attack", true, 1);
            BattleOperations.CanDisplayDialougue = false; 
            Move_handler.Instance.SelectRelevantBuffOrDebuff(AttackBuffData);
            _abilityTriggered = true;
        }
    }
    void Levitate()
    {
        if (_abilityTriggered) return;
        Debug.Log("activated levitate");
        _participant.additionalTypeImmunity = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/Ground");
        _abilityTriggered = true;
    }
    void Overgrow()
    {
        if (_abilityTriggered) return;
        Move_handler.Instance.OnDamageDeal += IncreaseDamage;
        _abilityTriggered = true;
    }
    void ParalysisCombo()
    {
        if (_abilityTriggered) return;
        Move_handler.Instance.OnDamageDeal += IncreaseDamage;
        _abilityTriggered = true;
    }
    void HealStatusEffect(Battle_Participant victim,string status)
    {
        if (victim != _participant) return;
        if (_participant.pokemon.Status_effect == "None") return;
        if (Utility.RandomRange(1, 4) < 2)
        {
            _participant.pokemon.Status_effect = "None";
            if (_participant.pokemon.Status_effect == "sleep" | _participant.pokemon.Status_effect == "freeze"| _participant.pokemon.Status_effect == "paralysis")
                _participant.pokemon.canAttack = true;
            Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+"'s shed skin healed it");
        }
    }
    void ShedSkin()
    {
        HealStatusEffect(_participant,_participant.pokemon.Status_effect);//incase you already had status when entering battle
        if (_abilityTriggered) return;
        Move_handler.Instance.OnStatusEffectHit += HealStatusEffect;
        _abilityTriggered = true;
    }
    void Static()
    {
        if (_abilityTriggered) return;
        Move_handler.Instance.OnMoveHit += GiveStatic;
        _abilityTriggered = true;
    }
    
    void Swarm()
    {
        if (_abilityTriggered) return;
        Move_handler.Instance.OnDamageDeal += IncreaseDamage;
        _abilityTriggered = true;
    }
    void Torrent()
    {
        if (_abilityTriggered) return;
        Move_handler.Instance.OnDamageDeal += IncreaseDamage;
        _abilityTriggered = true;
    }

    void GiveItem()
    {
        if (_participant.pokemon.HasItem) return;
        //Check level and 10% pickup chance
        if (_participant.pokemon.Current_level < 5) return;
        if (Utility.RandomRange(1, 101) > 10) return;
        List<(int MinLevel, int MaxLevel, string[] Items)> ItemPools = new()
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
        foreach (var pool in ItemPools)
        {
            if (_participant.pokemon.Current_level >= pool.MinLevel && _participant.pokemon.Current_level <= pool.MaxLevel)
            {
                PossibleItems = pool.Items;
                break;
            }
        }
        if (PossibleItems != null)
        {
            int ItemWonIndex = Utility.RandomRange(0, PossibleItems.Length);
            Item ItemWon = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/" + PossibleItems[ItemWonIndex]);
            if (Utility.RandomRange(1, 101) < _participant.pokemon.Current_level)
                _participant.pokemon.HeldItem = Obj_Instance.CreateItem(ItemWon);
        }

    }
    void GiveStatic(Battle_Participant attacker,bool isSpecialMove)
    {
        if (attacker.pokemon.Status_effect != "None") return;
        if (attacker == _participant) return;
        Debug.Log("triggered static: "+attacker.pokemon.Pokemon_name);
        if (!attacker.pokemon.CanBeDamaged)
            return;
        if(isSpecialMove)return;
        //simulate a pokemon's attack
        Move PlaceholderMove = ScriptableObject.CreateInstance<Move>();
        PlaceholderMove.Status_effect = "Paralysis";
        Move_handler.Instance.HandleStatusApplication(attacker, PlaceholderMove);
    }
    float IncreaseDamage(Battle_Participant attacker,Battle_Participant victim,Move move, float damage)
    {
        if (_currentAbility == "swarm" & (_participant.pokemon.HP < (_participant.pokemon.max_HP * 0.33f)) & move.type.Type_name=="Bug")
            return damage*1.5f;
        if (_currentAbility == "paralysiscombo")
        {
            if (victim.pokemon.Status_effect=="Paralysis")
                return damage*2;
        }
        if (_currentAbility == "torrent" & _participant.pokemon.HP < (_participant.pokemon.max_HP * 0.33f))
            if (move.type.Type_name == "Water")
                return damage*1.5f;
        if (_currentAbility == "overgrow" & _participant.pokemon.HP < (_participant.pokemon.max_HP * 0.33f))
            if (move.type.Type_name == "Grass")
                return damage*1.5f;
        if (_currentAbility == "blaze" & _participant.pokemon.HP < (_participant.pokemon.max_HP * 0.33f))
            if (move.type.Type_name == "Fire")
                return damage*1.5f;
        return damage;
    }
}
