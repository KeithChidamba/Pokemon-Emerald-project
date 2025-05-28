using UnityEngine;
using System.Linq;
public class Testing : MonoBehaviour
{
    string[] _types = { "Normal", "Fire", "Water", "Electric", "Grass", "Ice", "Fighting", "Poison",
        "Ground", "Flying", "Psychic", "Bug", "Rock", "Ghost", "Dragon", "Dark", "Steel"
    };
    private void CheckTypes()
    {
        foreach (var type in _types)
        {
            var currentType = Resources.Load<Type>("Pokemon_project_assets/Pokemon_obj/Types/"+type);
            CheckType(currentType,currentType.immunities,"immune");
            CheckType(currentType,currentType.weaknesses,"weakness");
            CheckType(currentType,currentType.resistances,"resist");
        }
    }

    void CheckType(Type typeToCheck,string[] listOfTypes,string listName)
    {
        foreach (var type in listOfTypes)
        {
            if (type == "None") return;
            
            if(!_types.Contains(type))
                Debug.Log(type+ " invalid on asset: "+typeToCheck.typeName+"'s "+listName);
            else
                Debug.Log("valid");
        }
    }
}
