using System;
using UnityEngine.Serialization;

[Serializable]
public struct LearnSetMove
{
    public MoveName learnSetMove;
    public int requiredLevel;

    public string GetName()
    {
       return NameDB.GetMoveName(learnSetMove);
    }
}
