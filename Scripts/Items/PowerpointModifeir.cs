using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "PowerpointMod", menuName = "ppMod")]
public class PowerpointModifeir : AdditionalInfoModule
{
    public enum ModiferType{ IncreasePp,MaximisePp,RestorePp}
    public enum ModiferItemType{Ether,MaxEther,Vitamin}
    public ModiferType  modiferType;
    public ModiferItemType  itemType;
}
