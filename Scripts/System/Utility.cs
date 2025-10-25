using UnityEngine;

public static class Utility
{
    public static int RandomRange(int min,int exclusiveLimit)
    {
        return Random.Range(min, exclusiveLimit);
    }

    public static ushort Random16Bit()
    {
        var rand = new System.Random();
        ushort  random16BIT = (ushort)rand.Next(0, 65536);
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
