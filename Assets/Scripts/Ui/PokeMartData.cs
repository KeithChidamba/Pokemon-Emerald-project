using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PokeMartData", menuName = "PokeMart/Mart data")]
public class PokeMartData : AdditionalInfoModule
{
    public List<Item> availableItems = new ();
}
