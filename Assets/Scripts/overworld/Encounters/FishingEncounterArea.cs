using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Encounter", menuName = "Overworld/Fishing Encounter Table")]
public class FishingEncounterTable : EncounterTable
{
    public FishingTableForRod[] fishingTables;
}
[Serializable]
public struct FishingTableForRod
{
    public EncounterTableData tableData;
    public RodType rodType;
}
public enum RodType
{ 
    OldRod,
    GoodRod,
    SuperRod,
}