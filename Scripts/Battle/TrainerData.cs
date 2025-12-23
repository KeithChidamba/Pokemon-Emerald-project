using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Trainer Data", menuName = "trainer data")]
public class TrainerData : ScriptableObject
{
    public string TrainerName;
    public string TrainerType ;
    public int BaseMoneyPayout;
    public Encounter_Area TrainerLocation;
    public List<TrainerPokemonData> PokemonParty = new();
    public enum BattleType{Single,SingleDouble,Double}
    public BattleType battleType;
    public enum AiFlags{CheckBadMove,CheckViability,CheckStatus,CheckSetup,CheckSwitching,CheckPriority}
    public List<AiFlags> trainerAiFlags;
    public Sprite battleIntroSprite;
    public string battleLossMessage;
    public string battleIntroMessage;
}
