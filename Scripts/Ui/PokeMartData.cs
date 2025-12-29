using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PokeMartData", menuName = "pmd")]
public class PokeMartData : ScriptableObject
{
    public AreaName location;
    public List<Item> availableItems = new ();
}
