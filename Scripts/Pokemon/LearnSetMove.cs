using System;

[Serializable]
public struct LearnSetMove
{
    public PokemonOperations.Types moveType;
    public string moveName;
    public int requiredLevel;

    public string GetMoveType()//for loading moves from assets, because file names are lowered
    {
        return moveType.ToString().ToLower();
    }
}
