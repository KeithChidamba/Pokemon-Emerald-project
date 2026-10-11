using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Trainer Data", menuName = "Additional Info Module/trainer data")]

public class TrainerData : AdditionalInfoModule
{
    public string TrainerName;
    public TrainerType trainerType;
    public int BaseMoneyPayout;
    public AreaTransitionData TrainerLocationData;
    public List<TrainerPokemonData> PokemonParty = new();
    public BattleType battleType;
    public List<AiFlags> trainerAiFlags = new();
    public Sprite battleIntroSprite;
    public string battleLossMessage;
    public string battleIntroMessage;
}

public enum BattleType
{
    Single,SingleDouble,Double
}

public enum AiFlags
{
    CheckBadMove,CheckViability,CheckStatus,CheckSetup,CheckSwitching,CheckPriority
}

public enum TrainerType
{
    RocketGrunt
}