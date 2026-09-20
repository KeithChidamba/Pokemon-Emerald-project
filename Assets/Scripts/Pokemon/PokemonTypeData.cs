using UnityEngine;

[CreateAssetMenu(fileName = "Pkm_types", menuName = "Pokemon/Type")]
public class PokemonTypeData : ScriptableObject 
{
    public PokemonType typeEnum;
    public string GetTypeName => typeEnum.ToString();
    public PokemonType[] weaknesses;
    public PokemonType[] resistances; 
    public PokemonType[] immunities;
    public Sprite typeImage;
}
