using UnityEngine;

public static class Utility
{
    public static int RandomRange(int min,int exclusive_lim)
    {
        return Random.Range(min, exclusive_lim);
    }
    public static int Random16Bit()
    {
        System.Random rand = new System.Random();
        int  random16bit = rand.Next(0, 65536);
        return random16bit;
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
