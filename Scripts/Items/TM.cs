using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "TM", menuName = "tmMove")]
public class TM : AdditionalItemInfo
{
    public int TmNumber;
    public NameDB.TM TmName;
    public Move move;
}
