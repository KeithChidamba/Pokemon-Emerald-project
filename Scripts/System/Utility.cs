using UnityEngine;

public static class Utility
{
    public static int RandomRange(int min,int exclusiveLimit)
    {
        return Random.Range(min, exclusiveLimit);
    }
    public static int RandomRange(int min,int exclusiveLimit,int excludedValue)
    {
        var currentRandom = RandomRange(min, exclusiveLimit);
        if(currentRandom==excludedValue)
            RandomRange(min,exclusiveLimit,excludedValue);
        return currentRandom;
    }
    public static int Random16Bit()
    {
        var rand = new System.Random();
        int  random16BIT = rand.Next(0, 65536);
        return random16BIT;
    }

    public static int Cube(int num)
    {
        return num * num * num;
    }
    public static int Square(int num)
    {
        return num * num;
    }
}
