using System;
[Serializable]
public class PowerpointModifeir : DynamicAdditionalInfo
{
    public ModiferType  modiferType;
    public ModiferItemType  itemType;
    public enum ModiferItemType{Ether,MaxEther,Vitamin}
    
    public enum ModiferType{ IncreasePp,MaximisePp,RestorePp}
}


