using System;
using UnityEngine.Serialization;

[Serializable]
public struct LearnSetMove
{
    public NameDB.MoveName moveName;
    public int requiredLevel;

    public string GetName()
    {
       return NameDB.GetMoveName(moveName);
    }
}
