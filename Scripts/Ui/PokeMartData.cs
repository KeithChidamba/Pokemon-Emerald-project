using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PokeMartData", menuName = "pmd")]
public class PokeMartData : ScriptableObject
{
    public string location;
    public List<Item> availableItems = new ();
}
