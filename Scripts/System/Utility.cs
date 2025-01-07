using UnityEngine;

public static class Utility
{
    public static int Get_rand(int min,int exclusive_lim)
    {
        return Random.Range(min, exclusive_lim);
    }
    public static int Generate_ID()
    {
        System.Random rand = new System.Random();
        int  random16bit = rand.Next(0, 65536);
        return random16bit;
    }
}
