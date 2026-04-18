using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[Serializable]
public class AbilityHandler : BattleParticipantModule
{
    public event Action OnAbilityUsed;
    private bool _abilityTriggered;
    private string _currentAbility;
    private readonly Dictionary<string, Action> _abilityMethods = new ();
    private readonly Dictionary<string,string> _damageBuffCombinations = new()
    {
        {"blaze", "Fire"},
        {"torrent", "Water"},
        {"overgrow", "Grass"},
        {"swarm", "Bug"}
    };
    private Dialogue_handler _dialogueHandler;
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Move_handler _moveUsageHandler;
    private BattleOperations _battleOperationsHandler;
    
    public AbilityHandler(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        OnInject();
    }

    private void OnInject()
    {
        _battleHandler.OnBattleEnd += ResetState;
        _turnBasedCombatHandler.OnNewTurn += CheckAbilityUsability;
        _abilityMethods.Add("innerfocus",InnerFocus);
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
        if (!participant.isActive) return;
        if(participant.pokemon.hp>0)
            OnAbilityUsed?.Invoke();
    }

    public void SetAbilityMethod()
    {
        _currentAbility = participant.pokemon.ability.abilityName.ToLower().Replace(" ","");
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
        _moveUsageHandler.OnMoveHit -= GiveStatic;
        _battleHandler.OnBattleEnd -= GiveItem;
        _moveUsageHandler.OnDamageCalc -= IncreaseDamage;
        participant.statusHandler.OnStatusCheck -= HealStatusEffect;
    }

    void InnerFocus()
    {
        if (_abilityTriggered) return;
        participant.pokemon.canBeFlinched = false;
        _abilityTriggered = true;
    }
    void PickUp()
    {
        if (_abilityTriggered) return;
        _battleHandler.OnBattleEnd += GiveItem;
        _abilityTriggered = true;
    }
    void ArenaTrap()
    {
        if (_abilityTriggered) return;
        TrapEnemies();//first entry in battle doesnt count as switch in, so leave this here
        _battleHandler.OnSwitchIn += TrapEnemies;
        _battleHandler.OnSwitchOut += RemoveTrap;
        _abilityTriggered = true;
    }
    void Blaze()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnDamageCalc += IncreaseDamage;
        _abilityTriggered = true;
    }
    void Guts()
    {
        if (_abilityTriggered) return;
        if (participant.pokemon.statusEffect == StatusEffect.None) return;
        var attackBuffData = new BuffDebuffData(participant, Stat.Attack, true, 1);
        _battleOperationsHandler.canDisplayChange = false; 
        _moveUsageHandler.ExecuteBuffOrDebuff(attackBuffData);
        _abilityTriggered = true;
    }
    void Levitate()
    {
        if (_abilityTriggered) return;
        participant.additionalTypeImmunity = Resources.Load<Type>(AssetDirectory.Types+"Ground");
        _abilityTriggered = true;
    }
    void Overgrow()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnDamageCalc += IncreaseDamage;
        _abilityTriggered = true;
    }
    void ParalysisCombo()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnDamageCalc += IncreaseDamage;
        _abilityTriggered = true;
    }
    void ShedSkin()
    {
        if (_abilityTriggered) return;
        participant.statusHandler.OnStatusCheck += HealStatusEffect;
        _abilityTriggered = true;
    }
    void Static()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnMoveHit += GiveStatic;
        _abilityTriggered = true;
    }
    
    void Swarm()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnDamageCalc += IncreaseDamage;
        _abilityTriggered = true;
    }
    void Torrent()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnDamageCalc += IncreaseDamage;
        _abilityTriggered = true;
    }

    private void RemoveTrap(Battle_Participant thisParticipant)
    {
        if (thisParticipant != participant) return;
        foreach (var enemy in participant.currentEnemies)
            enemy.statusHandler.RemoveTrap();
        _battleHandler.OnSwitchIn -= TrapEnemies;
        _battleHandler.OnSwitchOut -= RemoveTrap;
    }
    private void TrapEnemies()
    {
        foreach (var enemy in participant.currentEnemies)
        {
            if (enemy.pokemon.HasType(Types.Flying) || enemy.pokemon.HasType(Types.Ghost))
                continue;
            _moveUsageHandler.ApplyTrap(enemy,false);
        }
    }
    void HealStatusEffect(Battle_Participant thisParticipant)
    {
        var currentStatus = participant.pokemon.statusEffect;
        
        if (Utility.RandomRange(1, 4) < 2)
        {
            if (currentStatus == StatusEffect.Sleep
                || currentStatus == StatusEffect.Freeze
                || currentStatus == StatusEffect.Paralysis)
            {
                if(!participant.isFlinched)
                    participant.canAttack = true;
            }
            participant.pokemon.statusEffect = StatusEffect.None;
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName+"'s shed skin healed it");
            participant.RefreshStatusEffectImage();
        }
    }
    void GiveItem()
    {
        if (participant.pokemon.hasItem) return;
        if (!participant.pokemon.hasTrainer) return;//wild pokemon dont need to be picking up items when battle ends
        //Check level and 10% pickup chance
        if (participant.pokemon.currentLevel < 5) return;
        if (Utility.RandomRange(1, 101) > 10) return;
        //only happens at end of battle so no need to cache list
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
            if (participant.pokemon.currentLevel >= pool.MinLevel && participant.pokemon.currentLevel <= pool.MaxLevel)
            {
                possibleItems = pool.Items;
                break;
            }
        }
        if (possibleItems == null) return;
        string[] nonMartItems = { "Rare Candy", "Ether" };
        var itemWonIndex = Utility.RandomRange(0, possibleItems.Length);

        var assetDirectory = nonMartItems.Contains(possibleItems[itemWonIndex])?
            Save_manager.GetDirectory(AssetDirectory.NonMartItems) + possibleItems[itemWonIndex]
            : Save_manager.GetDirectory(AssetDirectory.MartItems) + possibleItems[itemWonIndex];
        
        var itemWon = Resources.Load<Item>(assetDirectory);
        if (Utility.RandomRange(1, 101) < participant.pokemon.currentLevel)
        {
            participant.pokemon.GiveItem(InstanceFactory.CreateItem(itemWon));
        }
    }
    void GiveStatic(Battle_Participant attacker,Move moveUsed)
    {
        if (attacker.pokemon.statusEffect != StatusEffect.None) return;
        if (attacker == participant) return;
        if (!attacker.canBeDamaged)
            return;
        if(!moveUsed.isContact)return; 
        //simulate a pokemon's attack
        _moveUsageHandler.OnStatusEffectHit+=NotifyStaticHit; 
        var placeholderMove = ScriptableObject.CreateInstance<Move>();
        placeholderMove.statusEffect = StatusEffect.Paralysis;
        _moveUsageHandler.HandleStatusApplication(attacker, placeholderMove,false);
    }

    private void NotifyStaticHit(Battle_Participant attacker,StatusEffect status)//status is unused here but is required for method signature
    {
        _moveUsageHandler.OnStatusEffectHit-=NotifyStaticHit; 
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName+"'s static paralysed "+attacker.pokemon.pokemonName);
    }
    float IncreaseDamage(Battle_Participant attacker,Battle_Participant victim,Move move, float damage)
    {
        if (attacker != participant) return damage;
        if (_currentAbility == "paralysiscombo")
        {
            if (victim.pokemon.statusEffect == StatusEffect.Paralysis)
                return damage*2;
        }
        if (_damageBuffCombinations.TryGetValue(_currentAbility, out var typeName))
            return damage * GetAbilityDamageBuff(move.type.typeName, typeName);
        
        return damage;
    }

    private float GetAbilityDamageBuff(string moveTypeName, string typeName)
    {
        if (participant.pokemon.hp < (participant.pokemon.maxHp * 0.33f))
            if (moveTypeName == typeName)
                return 1.5f;
        return 1f;
    }
}
