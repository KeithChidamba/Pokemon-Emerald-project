using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AbilityHandler : BattleParticipantModule
{
    public event Action OnAbilityUsed;
    private Action _onAbilityReset;
    
    private bool _abilityTriggered;
    private AbilityName _currentAbility;
    private readonly Dictionary<AbilityName, Action> _abilityMethods = new ();
    private readonly Dictionary<AbilityName, DamageBuffAbilityData> _damageBuffCombinations = new();
    
    private DialogueHandler _dialogueHandler;
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    public AbilityHandler(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _battleHandler.OnBattleEnd += ResetState;
        _turnBasedCombatHandler.OnNewTurn += CheckAbilityUsability;
        void CheckAbilityUsability()
        {
            if (!participant.isActive) return;
            OnAbilityUsed?.Invoke();
        }
        
        _abilityMethods.Add(AbilityName.InnerFocus,InnerFocus);
        _abilityMethods.Add(AbilityName.PickUp,PickUp);
        _abilityMethods.Add(AbilityName.Guts,Guts);
        _abilityMethods.Add(AbilityName.Levitate,Levitate);
        _abilityMethods.Add(AbilityName.Blaze,ApplyDamageBuffAbility);
        _abilityMethods.Add(AbilityName.Overgrow,ApplyDamageBuffAbility);
        _abilityMethods.Add(AbilityName.Torrent,ApplyDamageBuffAbility);
        _abilityMethods.Add(AbilityName.ParalysisCombo,ApplyDamageBuffAbility);
        _abilityMethods.Add(AbilityName.Swarm,ApplyDamageBuffAbility);
        _abilityMethods.Add(AbilityName.ArenaTrap,ArenaTrap);
        _abilityMethods.Add(AbilityName.Static,Static);
        _abilityMethods.Add(AbilityName.ShedSkin,ShedSkin);
        
        //damage buffers
        bool HealthBased(BattleParticipant attacker, PokemonType typeRequirement)
        {
            return attacker.pokemon.HasType(typeRequirement) &&
                   attacker.pokemon.hp < (attacker.pokemon.maxHp * 0.33f);
        }
        bool StatusEffectCheck(BattleParticipant victim, StatusEffect statusEffect)
        {
            return victim.pokemon.statusEffect == statusEffect;
        }
        
        List<DamageBuff> healthBasedBuffs = new()
        {
            new (AbilityName.Blaze,1.5f,PokemonType.Fire),
            new (AbilityName.Torrent,1.5f,PokemonType.Water),
            new (AbilityName.Overgrow, 1.5f,PokemonType.Grass),
            new (AbilityName.Swarm,1.5f,PokemonType.Bug)
        };

        foreach (var possibleBuff in healthBasedBuffs)
        {
            var newData = new DamageBuffAbilityData(
                possibleBuff.multiplier,
                (attacker, victim, move) => HealthBased(attacker, possibleBuff.type)
            );
            _damageBuffCombinations.Add(possibleBuff.abilityName, newData);
        }

        var paralysisCombo = new DamageBuff(AbilityName.ParalysisCombo, 2f);
        var parData = new DamageBuffAbilityData(
            paralysisCombo.multiplier,
            (attacker, victim, move) => StatusEffectCheck(victim, StatusEffect.Paralysis)
        );
        _damageBuffCombinations.Add(paralysisCombo.abilityName, parData);
    }
    
    public void SetAbilityMethod()
    {
        _abilityTriggered = false;
        _currentAbility = participant.pokemon.ability.abilityName;
        if (_abilityMethods.TryGetValue(_currentAbility, out Action abilityMethod))
            OnAbilityUsed += abilityMethod;
        else
        {
            Debug.Log($"Ability '{_currentAbility}' not found!");
        }
    }

    public void ResetState()
    {
        OnAbilityUsed = null;
        _abilityTriggered = false;
        _onAbilityReset?.Invoke();
        _onAbilityReset = null;
    }
    private void InnerFocus()
    {
        if (_abilityTriggered) return;
        participant.canBeFlinched = false;
        _abilityTriggered = true;
    }
    
    private void Guts()
    {
        if (_abilityTriggered) return;
        _turnBasedCombatHandler.SubToMoveExecution(ActivateGuts);
        _abilityTriggered = true;
        return;
        IEnumerator ActivateGuts(BattleParticipant currentParticipant)
        {
            if (currentParticipant.participantKey != participant.participantKey) yield break;
            
            if (participant.pokemon.statusEffect == StatusEffect.None) yield break;
            
            var attackBuffData = new StatChangeTransitData(participant, Stat.Attack, true, 1);
            
            _moveUsageHandler.InitiateStatChange(attackBuffData,false);
            _turnBasedCombatHandler.OnTurnEventsCompleted += RemoveSubscription;
            
            yield break;
            void RemoveSubscription()
            {
                _turnBasedCombatHandler.OnTurnEventsCompleted -= RemoveSubscription;
                _turnBasedCombatHandler.UnsubscribeFromMoveExecution(ActivateGuts);
            }
        }
    }
    private void Levitate()
    {
        if (_abilityTriggered) return;
        participant.additionalTypeImmunity = Resources.Load<Type>(DirectoryHandler.GetDirectory(AssetDirectory.Types) + nameof(PokemonType.Ground));
        _abilityTriggered = true;
    }
    private void PickUp()
    {
        if (_abilityTriggered) return;
        _battleHandler.OnBattleEnd += GiveItem;
        _abilityTriggered = true;
        _onAbilityReset += ()=> _battleHandler.OnBattleEnd -= GiveItem;
        return;
        void GiveItem()
        {
            if (Utility.RandomChance(CommonRandom.Rnd90)) return;
            if (Utility.RandomRange100() < participant.pokemon.currentLevel) return;
            CheckItemForPickUpAbility(participant);
        }
    }
    public void CheckItemForPickUpAbility(BattleParticipant currentParticipant)
    {
        if (currentParticipant.pokemon.hasItem) return;
        if (!currentParticipant.pokemon.hasTrainer) return;
        if (currentParticipant.pokemon.currentLevel < 5) return;
        
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
            if (currentParticipant.pokemon.currentLevel >= pool.MinLevel && currentParticipant.pokemon.currentLevel <= pool.MaxLevel)
            {
                possibleItems = pool.Items;
                break;
            }
        }
        if (possibleItems == null) return;
       
        var itemWonIndex = Utility.RandomRange(0, possibleItems.Length);

        var assetDirectory = DirectoryHandler.GetDirectory(AssetDirectory.Items) + possibleItems[itemWonIndex];
        
        var itemWon = Resources.Load<Item>(assetDirectory);
        if (itemWon == null)
        {
            Debug.LogError($"[Pickup Ability],{assetDirectory} doesnt exist, check item name in pool");
            return;
        }
        currentParticipant.pokemon.GiveItem(InstanceFactory.CreateItem(itemWon));
    }
    private void ApplyDamageBuffAbility()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnDamageCalc += IncreaseDamage;
        _abilityTriggered = true;
        _onAbilityReset += ()=> _moveUsageHandler.OnDamageCalc -= IncreaseDamage;
        return;
        float IncreaseDamage(BattleParticipant attacker,BattleParticipant victim,Move move, float damage)
        {
            if (attacker.participantKey != participant.participantKey) return damage;
            if (_damageBuffCombinations.TryGetValue(_currentAbility, out var damageBuffData))
            {
                return damage * damageBuffData.CanBuffDamage(attacker, victim, move);
            }
            return damage;
        }
    }

    private void ArenaTrap()
    {
        if (_abilityTriggered) return;
        
        //first entry in battle doesn't count as switch in, so leave this here
        TrapEnemies();
        
        _battleHandler.OnSwitchIn += TrapEnemies;
        _battleHandler.OnSwitchOut += RemoveTrap;
        _abilityTriggered = true;
        return;
        void RemoveTrap(BattleParticipant thisParticipant)
        {
            if (thisParticipant != participant) return;
            foreach (var enemy in participant.currentEnemies)
                enemy.statusHandler.RemoveTrap(TrapDataInfo.TrapType.PersistentFromAbility);
            _battleHandler.OnSwitchIn -= TrapEnemies;
            _battleHandler.OnSwitchOut -= RemoveTrap;
        } 
        void TrapEnemies()
        {
            foreach (var enemy in participant.currentEnemies)
            {
                if (enemy.pokemon.ability.abilityName == AbilityName.Levitate)
                {
                    continue;
                }
                if (enemy.pokemon.HasType(PokemonType.Flying))
                {
                    continue;
                }
                _moveUsageHandler.ApplyTrap(enemy,TrapDataInfo.TrapType.PersistentFromAbility);
            }
        }
    }
    private void ShedSkin()
    {
        if (_abilityTriggered) return;
        participant.statusHandler.OnStatusCheck += HealStatusEffect;
        _abilityTriggered = true;
        _onAbilityReset += ()=> participant.statusHandler.OnStatusCheck -= HealStatusEffect;
        return;
        void HealStatusEffect()
        {
            ShedSkinAbilityEffect(CommonRandom.Rnd33,participant);
        }
    }
    public void ShedSkinAbilityEffect(CommonRandom chance,BattleParticipant currentParticipant)
    {
        if (currentParticipant.pokemon.statusEffect == StatusEffect.None)return;
        
        var currentStatus = currentParticipant.pokemon.statusEffect;
        if (Utility.RandomChance(chance))
        {
            if (currentStatus is StatusEffect.Sleep
                or StatusEffect.Freeze
                or StatusEffect.Paralysis)
            {
                if(!currentParticipant.isFlinched)
                    currentParticipant.canAttack = true;
            }
            currentParticipant.pokemon.statusEffect = StatusEffect.None;
            _dialogueHandler.DisplayBattleInfo(currentParticipant.pokemon.pokemonDisplayName+"'s shed skin healed it");
            currentParticipant.RefreshStatusEffectImage();
        }
    }
    private void Static()
    {
        if (_abilityTriggered) return;
        _moveUsageHandler.OnMoveHit += GiveStatic;
        _abilityTriggered = true;
        _onAbilityReset += ()=> _moveUsageHandler.OnMoveHit -= GiveStatic;
        return;
        void GiveStatic(BattleParticipant attacker,BattleParticipant victim,Move moveUsed)
        {
            if (victim.participantKey != participant.participantKey) return;
            if (attacker.participantKey == participant.participantKey) return;
            if (attacker.pokemon.statusEffect != StatusEffect.None) return;
            if (!attacker.canBeDamaged) return;
            if (!moveUsed.isContact)return;
            
            //simulate a pokemon's attack
            _moveUsageHandler.OnStatusEffectHit += NotifyStaticHit; 
            var placeholderMove = ScriptableObject.CreateInstance<Move>();
            placeholderMove.statusEffect = StatusEffect.Paralysis;
            _moveUsageHandler.HandleStatusApplication(attacker, placeholderMove,false);
        }
        //status is unused here but is required for method signature
        void NotifyStaticHit(BattleParticipant attacker,StatusEffect status)
        {
            _moveUsageHandler.OnStatusEffectHit-=NotifyStaticHit; 
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s static paralysed "+attacker.pokemon.pokemonDisplayName);
        }
    }
}
