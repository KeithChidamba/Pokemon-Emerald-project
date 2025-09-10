using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TM", menuName = "tmMove")]
public class TM : AdditionalInfoModule
{
    public int TmNumber;
    public NameDB.TM TmName;
    public Move move;
}
