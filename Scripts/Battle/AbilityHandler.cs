using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class DamageBuffAbilityData
{
    private float _damageBuffMultiplier;
    public Func<Battle_Participant, Battle_Participant, Move, bool> conditionForBuff;

    public DamageBuffAbilityData(
        float damageBuffMultiplier,
        Func<Battle_Participant, Battle_Participant, Move, bool> conditionForBuff)
    {
        _damageBuffMultiplier = damageBuffMultiplier;
        this.conditionForBuff = conditionForBuff;
    }

    public float CanBuffDamage(
        Battle_Participant attacker,
        Battle_Participant victim,
        Move move)
    {
        if (conditionForBuff(attacker, victim, move))
            return _damageBuffMultiplier;

        return 1f;
    }
}

public class DamageBuff
{
    public string abilityName;
    public PokemonType type;
    public float multiplier;

    public DamageBuff(string abilityName,float multiplier,PokemonType type=PokemonType.Typeless)
    {
        this.abilityName = abilityName;
        this.multiplier = multiplier;
        this.type = type;
    }
}
[Serializable]
public class AbilityHandler : BattleParticipantModule
{
    public event Action OnAbilityUsed;
    private bool _abilityTriggered;
    private string _currentAbility;
    private readonly Dictionary<string, Action> _abilityMethods = new ();
    private readonly Dictionary<string, DamageBuffAbilityData> _damageBuffCombinations = new();
    
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
        
        //damage buffers
        bool HealthBased(Battle_Participant attacker, PokemonType typeRequirement)
        {
            return attacker.pokemon.HasType(typeRequirement) &&
                   attacker.pokemon.hp < (attacker.pokemon.maxHp * 0.33f);
        }
        bool StatusEffectCheck(Battle_Participant victim, StatusEffect statusEffect)
        {
            return victim.pokemon.statusEffect == statusEffect;
        }
        
        List<DamageBuff> healthBasedBuffs = new()
        {
            new ("blaze",1.5f,PokemonType.Fire),
            new ("torrent", 1.5f,PokemonType.Water),
            new ("overgrow", 1.5f,PokemonType.Grass),
            new ("swarm", 1.5f,PokemonType.Bug)
        };

        foreach (var possibleBuff in healthBasedBuffs)
        {
            var newData = new DamageBuffAbilityData(
                possibleBuff.multiplier,
                (attacker, victim, move) => HealthBased(attacker, possibleBuff.type)
            );
            _damageBuffCombinations.Add(possibleBuff.abilityName, newData);
        }

        var paralysisCombo = new DamageBuff("paralysiscombo", 2f);
        var parData = new DamageBuffAbilityData(
            paralysisCombo.multiplier,
            (attacker, victim, move) => StatusEffectCheck(victim, StatusEffect.Paralysis)
        );
        _damageBuffCombinations.Add(paralysisCombo.abilityName, parData);
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
        participant.additionalTypeImmunity = Resources.Load<Type>(AssetDirectory.Types + nameof(PokemonType.Ground));
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
            if (enemy.pokemon.HasType(PokemonType.Flying) || enemy.pokemon.HasType(PokemonType.Ghost))
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
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s shed skin healed it");
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
       
        var itemWonIndex = Utility.RandomRange(0, possibleItems.Length);

        var assetDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Items) + possibleItems[itemWonIndex];
        
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
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s static paralysed "+attacker.pokemon.pokemonDisplayName);
    }
    float IncreaseDamage(Battle_Participant attacker,Battle_Participant victim,Move move, float damage)
    {
        if (attacker != participant) return damage;
        
        if (_damageBuffCombinations.TryGetValue(_currentAbility, out var damageBuffData))
        {
            return damage * damageBuffData.CanBuffDamage(attacker, victim, move);
        }
        return damage;
    }
}
