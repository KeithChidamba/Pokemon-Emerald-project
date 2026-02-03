using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "PowerpointMod", menuName = "Item Info Modules/Powerpoint Modifeir")]
public class PowerpointModifeir : AdditionalInfoModule
{
    public ModiferType  modiferType;
    public ModiferItemType  itemType;
}

public enum ModiferItemType{Ether,MaxEther,Vitamin}

public enum ModiferType{ IncreasePp,MaximisePp,RestorePp}
