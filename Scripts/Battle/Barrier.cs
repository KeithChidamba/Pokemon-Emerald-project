using UnityEngine.Serialization;

[System.Serializable]
public class Barrier
{
    public string barrierName;
    public float barrierEffect;
    public int barrierDuration;

    public Barrier(string barrierName, float barrierEffect, int barrierDuration)
    {
        this.barrierName = barrierName;
        this.barrierEffect = barrierEffect;
        this.barrierDuration = barrierDuration;
    }
}
