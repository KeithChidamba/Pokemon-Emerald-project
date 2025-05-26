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
        Turn_Based_Combat.Instance.OnNewTurn += CheckAbilityUsability;
        _abilityMethods.Add("pickup",PickUp);
        _abilityMethods.Add("blaze",Blaze);
        _abilityMethods.Add("guts",Guts);
        _abilityMethods.Add("levitate",Levitate);
        _abilityMethods.Add("overgrow",Overgrow);
        _abilityMethods.Add("torrent",Torrent);
        _abilityMethods.Add("paralysiscombo",ParalysisCombo);
        _abilityMethods.Add("arenatrap",ArenaTrap);
        _abilityMethods.Add("static",Static);
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
            Debug.Log($"Ability '{_currentAbility}' not found!");
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
        Battle_handler.Instance.OnSwitchIn -= TrapEnemy;
        Battle_handler.Instance.OnSwitchOut -= RemoveTrap;
    }
    void PickUp()
    {
        if (_abilityTriggered) return;
        Battle_handler.Instance.OnBattleEnd += GiveItem;
        _abilityTriggered = true;
    }
    void ArenaTrap()
    {
        if (_abilityTriggered) return;
        TrapEnemy();//first entry in battle doesnt count as switch in, so leave this here
        Battle_handler.Instance.OnSwitchIn += TrapEnemy;
        Battle_handler.Instance.OnSwitchOut += RemoveTrap;
        _abilityTriggered = true;
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
        if (_participant.pokemon.statusEffect == "None") return;
        var attackBuffData = new BuffDebuffData(_participant.pokemon, "Attack", true, 1);
        BattleOperations.CanDisplayDialougue = false; 
        Move_handler.Instance.SelectRelevantBuffOrDebuff(attackBuffData);
        _abilityTriggered = true;
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

    void ShedSkin()
    {
        HealStatusEffect(_participant,_participant.pokemon.statusEffect);//incase you already had status when entering battle
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

    private void RemoveTrap(Battle_Participant participant)
    {
        if (participant != _participant) return;
        foreach (var enemy in _participant.currentEnemies)
            enemy.canEscape = true;
    }
    private void TrapEnemy()
    {
        foreach (var enemy in _participant.currentEnemies)
        {
            if(!enemy.canEscape)continue;
            if(!enemy.pokemon.HasType("Flying") & !enemy.pokemon.HasType("Ghost") & enemy.pokemon.ability.abilityName!="Levitate")
                enemy.canEscape = false;
        }
    }
    void HealStatusEffect(Battle_Participant victim,string status)
    {
        if (victim != _participant) return;
        if (_participant.pokemon.statusEffect == "None") return;
        if (Utility.RandomRange(1, 4) < 2)
        {
            _participant.pokemon.statusEffect = "None";
            if (_participant.pokemon.statusEffect == "sleep" | _participant.pokemon.statusEffect == "freeze"| _participant.pokemon.statusEffect == "paralysis")
                _participant.pokemon.canAttack = true;
            Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+"'s shed skin healed it");
        }
    }
    void GiveItem()
    {
        if (_participant.pokemon.hasItem) return;
        //Check level and 10% pickup chance
        if (_participant.pokemon.currentLevel < 5) return;
        if (Utility.RandomRange(1, 101) > 10) return;
        List<(int MinLevel, int MaxLevel, string[] Items)> itemPools = new()
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
        string[] possibleItems = null;
        foreach (var pool in itemPools)
        {
            if (_participant.pokemon.currentLevel >= pool.MinLevel && _participant.pokemon.currentLevel <= pool.MaxLevel)
            {
                possibleItems = pool.Items;
                break;
            }
        }
        if (possibleItems == null) return;
        
        var itemWonIndex = Utility.RandomRange(0, possibleItems.Length);
        var itemWon = Resources.Load<Item>("Pokemon_project_assets/Player_obj/Bag/" + possibleItems[itemWonIndex]);
        if (Utility.RandomRange(1, 101) < _participant.pokemon.currentLevel)
            _participant.pokemon.heldItem = Obj_Instance.CreateItem(itemWon);
    }
    void GiveStatic(Battle_Participant attacker,bool isSpecialMove)
    {
        if (attacker.pokemon.statusEffect != "None") return;
        if (attacker == _participant) return;
        if (!attacker.pokemon.canBeDamaged)
            return;
        if(isSpecialMove)return; 
        //simulate a pokemon's attack
        Move_handler.Instance.OnStatusEffectHit+=NotifyStaticHit; 
        var placeholderMove = ScriptableObject.CreateInstance<Move>();
        placeholderMove.statusEffect = "Paralysis";
        Move_handler.Instance.HandleStatusApplication(attacker, placeholderMove,false);
    }

    private void NotifyStaticHit(Battle_Participant attacker,string status)//status is unused here but is required for method signature
    {
        Move_handler.Instance.OnStatusEffectHit-=NotifyStaticHit; 
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+"'s static paralysed "+attacker.pokemon.pokemonName);
    }
    float IncreaseDamage(Battle_Participant attacker,Battle_Participant victim,Move move, float damage)
    {
        if (_currentAbility == "swarm" & (_participant.pokemon.hp < (_participant.pokemon.maxHp * 0.33f)) & move.type.typeName=="Bug")
            return damage*1.5f;
        if (_currentAbility == "paralysiscombo")
        {
            if (victim.pokemon.statusEffect=="Paralysis")
                return damage*2;
        }
        if (_currentAbility == "torrent" & _participant.pokemon.hp < (_participant.pokemon.maxHp * 0.33f))
            if (move.type.typeName == "Water")
                return damage*1.5f;
        if (_currentAbility == "overgrow" & _participant.pokemon.hp < (_participant.pokemon.maxHp * 0.33f))
            if (move.type.typeName == "Grass")
                return damage*1.5f;
        if (_currentAbility == "blaze" & _participant.pokemon.hp < (_participant.pokemon.maxHp * 0.33f))
            if (move.type.typeName == "Fire")
                return damage*1.5f;
        return damage;
    }
}
