using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Battle/Battle Zone Contestant")]
public class BattleZoneContestantRegistry : ScriptableObject
{
    public List<TournamentContestant> contestants = new();
}
[Serializable]
public class TournamentContestant
{
    public string trainerName;
    public Sprite battleIntroSprite;
    public SpriteDataForNpc spriteData;
    [HideInInspector] public NpcLogic logic;
    [HideInInspector] public List<Pokemon> pokemonParty = new();
    [HideInInspector]public int bracketIndex;
}