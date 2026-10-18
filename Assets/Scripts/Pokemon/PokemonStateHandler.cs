using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class PokemonStateHandler
{
    public Pokemon pokemon;
    public int healthPhase;
    /// <summary>
    /// Allows an external source like item usage to decide what happens when a
    /// pokem on evolves [Evolving after rare candy]
    /// </summary>
    public bool handleEvolutionExternally;
    /// <summary>
    /// flag for systemic leve-lup, to make specific custom pokemon
    /// </summary>
    [HideInInspector]public bool canEvolve = true;
    
    /// <summary>
    /// [Testing] Notify when exp is gained
    /// </summary>
    public event Action<int> OnExpGained;
    public event Action OnNewLevel;
    public event Action<Pokemon> OnLevelUp;
    public event Action<int> OnEvolutionSuccessful;
    public event Action OnHealthChanged;
    
    private BattleHandler _battleHandler;
    private DialogueHandler _dialogueHandler;
    private PokemonOperations _pokemonOperationsHandler;
    
    public PokemonStateHandler(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        
        _battleHandler.OnBattleEnd += ClearBattleState;
        _battleHandler.OnBattleStarted += SetupBattleState;
        return;
        void SetupBattleState()
        {
            handleEvolutionExternally = true;
        }
        void ClearBattleState()
        {
            OnLevelUp = null;
            OnNewLevel = null;
            OnEvolutionSuccessful = null;
            OnExpGained = null;
            handleEvolutionExternally = false;
        }
    }
    /// <summary>
    /// Short-lived version
    /// </summary>
    public PokemonStateHandler(ServiceContainer container,Pokemon newPokemon,bool externalEvolution)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        pokemon = newPokemon;
        handleEvolutionExternally = externalEvolution;
    }

    /// <summary>
    /// Resets move data in case of in-battle modification
    /// </summary>
    public void ResetMoveData()
    {
        List<MoveSaveData> uniqueMoveData = new ();
        pokemon.moveSet.ForEach(move=>uniqueMoveData.Add(
            new (move.moveName,move.powerpoints,move.maxPowerpoints)
        ));
        PokemonOperations.LoadMovesFromSource(uniqueMoveData,pokemon);
    }
    public void NotifyHealthChange()
    {
        OnHealthChanged?.Invoke();
    }
    public IEnumerator ReceiveExperienceOutsideBattle(int amount,bool isPartPokemon)
    {
        if (pokemon.currentLevel >= 100) yield break;
        int remainingExp = amount;
        while (remainingExp > 0 && pokemon.currentLevel < 100)
        {
            int expToNextLevel = pokemon.nextLevelExpAmount - pokemon.currentExpAmount;
            int expThisLoop = Mathf.Min(remainingExp, expToNextLevel);
            pokemon.currentExpAmount += expThisLoop;
            remainingExp -= expThisLoop;
            if (pokemon.currentExpAmount >= pokemon.nextLevelExpAmount && pokemon.currentLevel < 100)
            {
                LevelUp(false);
                yield return _pokemonOperationsHandler.WaitForNewMoveCheck(pokemon,isPartPokemon);
                pokemon.nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel, pokemon.expGroup);
            }
        }
    }

    private void LevelUp(bool increaseFriendship)
    {
        OnLevelUp?.Invoke(pokemon);
        
        if(increaseFriendship)
        {
            pokemon.DetermineFriendshipLevelChange(FriendshipModifier.LevelUp);
        }
        pokemon.currentLevel++;
        pokemon.currentLevelExpAmount = PokemonOperations.CalculateExpForLevel(pokemon.currentLevel,pokemon.expGroup);
        pokemon.nextLevelExpAmount = PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel,pokemon.expGroup);
        pokemon.IncreaseStats();
        
        if (!pokemon.requiresEvolutionStone)
        {
            if (canEvolve)
            {
                if(pokemon.splitEvolution)
                {
                    // Determine Split Evolution
                    var evolutionValue = pokemon.personalityValue % 10;
                    if (evolutionValue < 5)
                        CheckEvolutionRequirements(0);
                    else if (evolutionValue < 10)
                        CheckEvolutionRequirements(2);
                }
                else
                    CheckEvolutionRequirements(0);
            }
        }
        OnNewLevel?.Invoke();
    }
    private void CheckEvolutionRequirements(int evoIndex)
    {
        if (pokemon.requiresFriendshipEvolution)
        {
            if (pokemon.friendshipLevel >= pokemon.friendshipEvolutionRequirement.friendshipRequirement)
            {
                SetupEvolution();
            } 
            return;
        } 
        
//regular evolution
        if (pokemon.currentEvolutionLineIndex >= pokemon.evolutionLineLevels.Length)
        {
            return;//max evolution
        }
        if (pokemon.currentLevel >= pokemon.evolutionLineLevels[pokemon.currentEvolutionLineIndex])
        {
            SetupEvolution();
        }
        return;
        void SetupEvolution()
        {
            if (handleEvolutionExternally)
            {
                OnEvolutionSuccessful?.Invoke(pokemon.currentEvolutionLineIndex+evoIndex);
            }
            else
            {
                pokemon.Evolve(pokemon.evolutions[pokemon.currentEvolutionLineIndex + evoIndex]);
            }
        }
    }
    
    /// <summary>
    /// Use to give a pokemon exp from battle
    /// </summary>
    /// <returns></returns>
    public IEnumerator ReceiveExperienceAndDisplay(int amount)
    {
        if (pokemon.currentLevel >= 100) yield break;

        int remainingExp = Mathf.FloorToInt(pokemon.AccountForExpGainModifier() * amount);

        OnExpGained?.Invoke(remainingExp);

        while (remainingExp > 0 && pokemon.currentLevel < 100)
        {
            int expToNextLevel = pokemon.nextLevelExpAmount - pokemon.currentExpAmount;

            // How much EXP should we add this loop? Either all remaining or just enough to hit the next level.
            int expThisLoop = Mathf.Min(remainingExp, expToNextLevel);
            float displayExp = pokemon.currentExpAmount;
            var expAfterChange = pokemon.currentExpAmount + expThisLoop;

            // Animate EXP bar filling
            var increaseDeltaMultiplier = expThisLoop / 100f > 3 ? expThisLoop / 100f : 3;
            SoundManager.Play(SfxId.ExpGain);
            while (displayExp < expAfterChange)
            {
                displayExp = Mathf.MoveTowards(displayExp, expAfterChange,
                    30f * increaseDeltaMultiplier * Time.deltaTime);
                pokemon.currentExpAmount = (int)Mathf.Floor(displayExp);
                yield return null;
            }

            // Deduct what we just gave
            remainingExp -= expThisLoop;
            // If we reached or passed the next level threshold > level up

            if (pokemon.currentExpAmount >= pokemon.nextLevelExpAmount && pokemon.currentLevel < 100)
            {
                SoundManager.Play(JingleId.LevelUp);
                
                LevelUp(true);
                
                _dialogueHandler.DisplayBattleInfo(pokemon.pokemonDisplayName + " grew to lv" + pokemon.currentLevel);

                yield return _dialogueHandler.AwaitAllDialogue();

                yield return _pokemonOperationsHandler.WaitForNewMoveCheck(pokemon, true);

                pokemon.nextLevelExpAmount =
                    PokemonOperations.CalculateExpForNextLevel(pokemon.currentLevel, pokemon.expGroup);
            }
        }
        _dialogueHandler.DisplayBattleInfo($"{pokemon.pokemonDisplayName} gained {amount} EXP points");
    }
}
