using System.Collections.Generic;
public static class NameDB
{
    public static string GetMoveName(LearnSetMoveName moveName)
    {
        return learnSetMoveNames[moveName];
    }
    public static Dictionary<LearnSetMoveName, string> learnSetMoveNames = new()
    {
        // 🐞 Bug-type
        { LearnSetMoveName.FuryCutter, "Fury Cutter" },
        { LearnSetMoveName.LeechLife, "Leech Life" },
        { LearnSetMoveName.SilverWind, "Silverwind" },
        { LearnSetMoveName.StringShot, "String Shot" },
        { LearnSetMoveName.PinMissile, "Pin Missile" },
        // 🐉 Dragon-type
        { LearnSetMoveName.DragonBreath, "Dragon Breath" },

        // ⚡ Electric-type
        { LearnSetMoveName.Thundershock, "Thundershock" },
        { LearnSetMoveName.ThunderWave, "Thunder Wave" },
        { LearnSetMoveName.Thunderbolt, "Thunderbolt" },
        { LearnSetMoveName.Thunder, "Thunder" },

        // 🥋 Fighting-type
        { LearnSetMoveName.BulkUp, "Bulk Up" },
        { LearnSetMoveName.Detect, "Detect" },
        { LearnSetMoveName.DoubleKick, "Double Kick" },
        { LearnSetMoveName.SkyUppercut, "Sky Uppercut" },

        // 🔥 Fire-type
        { LearnSetMoveName.BlazeKick, "Blaze Kick" },
        { LearnSetMoveName.Ember, "Ember" },
        { LearnSetMoveName.FirePunch, "Fire Punch" },
        { LearnSetMoveName.FireSpin, "Fire Spin" },
        { LearnSetMoveName.Flamethrower, "Flamethrower" },

        // 🛫 Flying-type
        { LearnSetMoveName.AirCutter, "Air Cutter" },
        { LearnSetMoveName.Gust, "Gust" },
        { LearnSetMoveName.MirrorMove, "Mirror Move" },
        { LearnSetMoveName.Peck, "Peck" },
        { LearnSetMoveName.WingAttack, "Wing Attack" },

        // 🌿 Grass-type
        { LearnSetMoveName.Absorb, "Absorb" },
        { LearnSetMoveName.GigaDrain, "Giga Drain" },
        { LearnSetMoveName.LeafBlade, "Leaf Blade" },
        { LearnSetMoveName.MegaDrain, "Mega Drain" },
        { LearnSetMoveName.StunSpore, "Stun Spore" },

        // 🌍 Ground-type
        { LearnSetMoveName.Dig, "Dig" },
        { LearnSetMoveName.Earthquake, "Earthquake" },
        { LearnSetMoveName.Magnitude, "Magnitude" },
        { LearnSetMoveName.MudSlap, "Mud-Slap" },
        { LearnSetMoveName.MudShot, "Mud Shot" },
        { LearnSetMoveName.MudSport, "Mud Sport" },
        { LearnSetMoveName.SandAttack, "Sand-Attack" },
        { LearnSetMoveName.SandTomb, "Sand Tomb" },

        // 🪨 Rock-type
        { LearnSetMoveName.SandStorm, "Sandstorm" },

        // 💜 Normal-type
        { LearnSetMoveName.Attract, "Attract" },
        { LearnSetMoveName.SonicBoom, "Sonic Boom" },
        { LearnSetMoveName.Harden, "Harden" },
        { LearnSetMoveName.BellyDrum, "Belly Drum" },
        { LearnSetMoveName.Bide, "Bide" },
        { LearnSetMoveName.Covet, "Covet" },
        { LearnSetMoveName.DoubleTeam, "Double Team" },
        { LearnSetMoveName.Endeavor, "Endeavor" },
        { LearnSetMoveName.Foresight, "Foresight" },
        { LearnSetMoveName.FocusEnergy, "Focus Energy" },
        { LearnSetMoveName.FalseSwipe, "False Swipe" },
        { LearnSetMoveName.Flail, "Flail" },
        { LearnSetMoveName.FurySwipes, "Fury Swipes" },
        { LearnSetMoveName.Growl, "Growl" },
        { LearnSetMoveName.Headbutt, "Headbutt" },
        { LearnSetMoveName.HyperBeam, "Hyper Beam" },
        { LearnSetMoveName.Leer, "Leer" },
        { LearnSetMoveName.MeanLook, "Mean Look" },
        { LearnSetMoveName.MorningSun, "Morning Sun" },
        { LearnSetMoveName.MoonLight, "Moonlight" },
        { LearnSetMoveName.OdorSleuth, "Odor Sleuth" },
        { LearnSetMoveName.Pound, "Pound" },
        { LearnSetMoveName.Protect, "Protect" },
        { LearnSetMoveName.QuickAttack, "Quick Attack" },
        { LearnSetMoveName.Scratch, "Scratch" },
        { LearnSetMoveName.Screech, "Screech" },
        { LearnSetMoveName.Slam, "Slam" },
        { LearnSetMoveName.Slash, "Slash" },
        { LearnSetMoveName.Supersonic, "Supersonic" },
        { LearnSetMoveName.TailWhip, "Tail Whip" },
        { LearnSetMoveName.Tackle, "Tackle" },
        { LearnSetMoveName.TakeDown, "Take Down" },
        { LearnSetMoveName.Whirlwind, "Whirlwind" },

        // 💀 Poison-type
        { LearnSetMoveName.PoisonFang, "Poison Fang" },
        { LearnSetMoveName.PoisonSting, "Poison Sting" },
        { LearnSetMoveName.Toxic, "Toxic" },

        // 🔮 Psychic-type
        { LearnSetMoveName.Agility, "Agility" },
        { LearnSetMoveName.Confusion, "Confusion" },
        { LearnSetMoveName.LightScreen, "Light Screen" },
        { LearnSetMoveName.Psybeam, "Psybeam" },
        { LearnSetMoveName.Reflect, "Reflect" },
        { LearnSetMoveName.Rest, "Rest" },

        // 🌊 Water-type
        { LearnSetMoveName.HydroPump, "Hydro Pump" },
        { LearnSetMoveName.MuddyWater, "Muddy Water" },
        { LearnSetMoveName.WaterGun, "Water Gun" },
        { LearnSetMoveName.Whirlpool, "Whirlpool" },

        // 👻 Ghost-type
        { LearnSetMoveName.Astonish, "Astonish" },
        { LearnSetMoveName.ConfuseRay, "Confuse Ray" },

        // ❄️ Ice-type
        { LearnSetMoveName.Haze, "Haze" },

        // 🌑 Dark-type
        { LearnSetMoveName.Bite, "Bite" },
        { LearnSetMoveName.Crunch, "Crunch" },
        { LearnSetMoveName.FaintAttack, "Faint Attack" },
        { LearnSetMoveName.Pursuit, "Pursuit" }
    };


    private static Dictionary<AbilityName, string> _abilityNames = new()
    {
        { AbilityName.Guts, "Guts" },
        { AbilityName.PickUp, "Pickup" },
        { AbilityName.Blaze, "Blaze" },
        { AbilityName.Levitate, "Levitate" },
        { AbilityName.Overgrow, "Overgrow" },
        { AbilityName.Torrent, "Torrent" },
        { AbilityName.ParalysisCombo, "Paralysis combo" },
        { AbilityName.ArenaTrap, "Arena Trap" },
        { AbilityName.Static, "Static" },
        { AbilityName.ShedSkin, "Shed skin" },
        { AbilityName.Swarm, "Swarm" },
        { AbilityName.InnerFocus, "Inner Focus" },
    };

    public static string GetAbility(AbilityName ability)
    {
        return _abilityNames[ability];
    }

    private static Dictionary<EvolutionStone, string> _stoneNames = new()
    {
        { EvolutionStone.ThunderStone, "Thunder Stone" },
        { EvolutionStone.FireStone, "Fire Stone" },
        { EvolutionStone.WaterStone, "Water Stone" },
        { EvolutionStone.LeafStone, "Leaf Stone" },
    };
    public static string GetStoneName(EvolutionStone stone)
    {
        return _stoneNames[stone];
    }
    public static string GetStatName(Stat stat)
    {
        if (stat == Stat.SpecialAttack)
            return "Special Attack";
        if (stat == Stat.SpecialDefense)
            return "Special Defense";
        return stat.ToString();
    }
}

public enum TM_Name
{
    BulletSeed,
    BrickBreak,
    BulkUp,
    AerialAce,
    Thunderbolt,
    Thunder,
    Flamethrower,
    GigaDrain,
    Dig,
    Earthquake,
    Sandstorm,
    HyperBeam,
    Toxic,
    IceBeam,
    RainDance
}

public enum HM_Name
{
    Surf,
    Fly
}

public enum AbilityName
{
    Guts,PickUp,Blaze,Levitate,Overgrow,Torrent,ParalysisCombo,ArenaTrap
    ,Static,ShedSkin,Swarm,InnerFocus
}

public enum EvolutionStone
{
    None,ThunderStone,FireStone,WaterStone,LeafStone
}

public enum LearnSetMoveName
{
    // 🐞 Bug-type
    FuryCutter,
    LeechLife,
    SilverWind,
    StringShot,
        
    // 🐉 Dragon-type
    DragonBreath,

    // ⚡ Electric-type
    Thundershock,
    ThunderWave,
    Thunderbolt,
    Thunder,
        
    // 🥋 Fighting-type
    BulkUp,
    Detect,
    DoubleKick,
    SkyUppercut,
        
    // 🔥 Fire-type
    BlazeKick,
    Ember,
    FirePunch,
    FireSpin,
    Flamethrower,
        
    // 🛫 Flying-type
    AirCutter,
    Gust,
    MirrorMove,
    Peck,
    WingAttack,

    // 🌿 Grass-type
    Absorb,
    GigaDrain,
    LeafBlade,
    MegaDrain,
    StunSpore,
        
    // 🌍 Ground-type
    Dig,
    Earthquake,
    Magnitude,
    MudSlap,
    MudShot,
    MudSport,
    SandAttack,
    SandTomb,
        
    //Rock
    SandStorm,
        
    // 💜 Normal-type
    Attract,
    SonicBoom,
    Harden,
    BellyDrum,
    Bide,
    Covet,
    DoubleTeam,
    Endeavor,
    FocusEnergy,
    Foresight,
    FalseSwipe,
    Flail,
    FurySwipes,
    Growl,
    Headbutt,
    HyperBeam,
    Leer, 
    MeanLook,
    MorningSun, // needs making after weather
    MoonLight,// needs making after weather    
    OdorSleuth,
    Pound,
    Protect,
    QuickAttack,
    Scratch,
    Screech,
    Slam,
    Slash,
    Supersonic,
    TailWhip,
    Tackle,
    TakeDown,
    Whirlwind,
        
    // 💀 Poison-type
    PoisonFang,
    PoisonSting,
    Toxic,

    // 🔮 Psychic-type
    Agility,
    Confusion,
    LightScreen,
    Psybeam,
    Reflect,
    Rest,
        
    // 🌊 Water-type
    HydroPump,
    MuddyWater,
    WaterGun,
    Whirlpool,
        
    // 👻 Ghost-type
    Astonish,
    ConfuseRay,
        
    //Ice-type
    Haze,

    // 🌑 Dark-type
    Bite,
    Crunch,
    FaintAttack,
    Pursuit,
    //forgot
    PinMissile
}
