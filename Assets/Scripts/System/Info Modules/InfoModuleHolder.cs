using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InfoModuleHolder : ScriptableObject
{
    [SerializeReference] public List<DynamicAdditionalInfo> dynamicInfoModules = new();
    
    public T GetDynamicModule<T>() where T : DynamicAdditionalInfo
        => dynamicInfoModules.FirstOrDefault(m => m is T) as T;
}

