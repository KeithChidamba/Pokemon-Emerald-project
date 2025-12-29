using System;
using UnityEngine.Serialization;

[Serializable]
public struct LearnSetMove
{
    public LearnSetMoveName learnSetMove;
    public int requiredLevel;

    public string GetName()
    {
       return NameDB.GetMoveName(learnSetMove);
    }
}
