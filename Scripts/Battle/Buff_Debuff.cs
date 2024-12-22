using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buff_Debuff
{
    public string Stat;
    public int Stage = 0;

    public Buff_Debuff(string stat, int stage)
    {
        Stat = stat;
        Stage = stage;
    }
}
