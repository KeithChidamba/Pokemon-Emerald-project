using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Battle/Battle Zone Contestant")]
public class BattleZoneContestantRegistry : ScriptableObject
{
    public List<TournamentContestant> contestants = new();
    public List<ContestantPokemonData> playerPokemonPool = new();
}
[Serializable]
public class TournamentContestant
{
    public string trainerName;
    public Sprite battleIntroSprite;
    public SpriteDataForNpc spriteData;
    public List<ContestantPokemonData> pokemonPartyData = new();
    [HideInInspector] public NpcLogic logic;
    [HideInInspector]public int bracketIndex;
}
[Serializable]
public class ContestantPokemonData
{
    public Pokemon pokemon;
    [Range(0,2)]
    public int evolutionStageNumber;
    [Range(0, 1)] public int splitEvolutionIndex;
    public List<Move> moveSet;
}