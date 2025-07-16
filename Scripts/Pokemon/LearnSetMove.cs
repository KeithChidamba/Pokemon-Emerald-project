using System;
using UnityEngine.Serialization;

[Serializable]
public struct LearnSetMove
{
    public PokemonOperations.Types moveType;
    public NameDB.MoveName moveName;
    public int requiredLevel;

    public string GetName()
    {
       return NameDB.GetMoveName(moveName);
    }
    public string GetMoveType()//for loading moves from assets, because file names are lowered
    {
        return moveType.ToString().ToLower();
    }
}
