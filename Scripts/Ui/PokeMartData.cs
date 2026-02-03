using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PokeMartData", menuName = "PokeMart/Mart data")]
public class PokeMartData : ScriptableObject
{
    public AreaName location;
    public List<Item> availableItems = new ();
}
