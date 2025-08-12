using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HM", menuName = "hmMove")]
public class HM : AdditionalItemInfo
{
    public int HmNumber;
    public NameDB.HM HmName;
    public Move move;
}
