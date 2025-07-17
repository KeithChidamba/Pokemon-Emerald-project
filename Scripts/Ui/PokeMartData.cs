using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PokeMartData", menuName = "pmd")]
public class PokeMartData : ScriptableObject
{
    public string location;
    public List<string> availableItems = new ();
    public List<NameDB.ItemName> itemList = new ();
    public void SetDataValues()
    {
        availableItems.Clear();
        foreach (var item in itemList)
        {
            availableItems.Add(NameDB.GetItemName(item));
        }
    }
}
