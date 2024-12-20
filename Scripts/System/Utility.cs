using UnityEngine;

public static class Utility
{
    public static int Get_rand(int min,int exclusive_lim)
    {
        return Random.Range(min, exclusive_lim);
    }

}
