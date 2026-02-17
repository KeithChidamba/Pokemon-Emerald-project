using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AreaData
{
    public AreaTransitionData data;
    public List<GameObject> npcList;
    
    public void LoadNpcObjects(bool enabled)
    {
        if(npcList.Count > 0) npcList.ForEach(obj=>obj.SetActive(enabled));   
    }
}
