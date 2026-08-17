using System.Collections.Generic;
using System.Linq;

public static class NameDB
{
    public static string GetMoveName(MoveName moveName)
    {
        return learnSetMoveNames[moveName];
    }
    public static MoveName ParseMoveName(string moveName)
    {
        var pair = learnSetMoveNames.FirstOrDefault(x => x.Value == moveName);
        return pair.Key;
    }
    private static Dictionary<MoveName, string> learnSetMoveNames = new()
    {
        // 🐞 Bug-type
        { MoveName.FuryCutter, "Fury Cutter" },
        { MoveName.LeechLife, "Leech Life" },
        { MoveName.SilverWind, "Silver Wind" },
        { MoveName.StringShot, "String Shot" },
        { MoveName.PinMissile, "Pin Missile" },
        { MoveName.SleepPowder,"Sleep Powder"},
        
        // 🐉 Dragon-type
        { MoveName.DragonBreath, "Dragon Breath" },

        // ⚡ Electric-type
        { MoveName.Thundershock, "Thundershock" },
        { MoveName.ThunderWave, "Thunder Wave" },
        { MoveName.Thunderbolt, "Thunderbolt" },
        { MoveName.Thunder, "Thunder" },

        // 🥋 Fighting-type
        { MoveName.BulkUp, "Bulk Up" },
        { MoveName.Detect, "Detect" },
        { MoveName.DoubleKick, "Double Kick" },
        { MoveName.SkyUppercut, "Sky Uppercut" },
        { MoveName.BrickBreak, "Brick Break" },
      
        // 🔥 Fire-type
        { MoveName.BlazeKick, "Blaze Kick" },
        { MoveName.Ember, "Ember" },
        { MoveName.FirePunch, "Fire Punch" },
        { MoveName.FireSpin, "Fire Spin" },
        { MoveName.Flamethrower, "Flamethrower" },
        { MoveName.SunnyDay, "Sunny Day" },
        
        // 🛫 Flying-type
        { MoveName.AirCutter, "Air Cutter" },
        { MoveName.Gust, "Gust" },
        { MoveName.MirrorMove, "Mirror Move" },
        { MoveName.Peck, "Peck" },
        { MoveName.WingAttack, "Wing Attack" },
        { MoveName.Fly, "Fly" },
        { MoveName.AerialAce, "Aerial Ace" },
        
        // 🌿 Grass-type
        { MoveName.Absorb, "Absorb" },
        { MoveName.GigaDrain, "Giga Drain" },
        { MoveName.LeafBlade, "Leaf Blade" },
        { MoveName.MegaDrain, "Mega Drain" },
        { MoveName.StunSpore, "Stun Spore" },
        { MoveName.BulletSeed, "Bullet Seed" },
        
        // 🌍 Ground-type
        { MoveName.Dig, "Dig" },
        { MoveName.Earthquake, "Earthquake" },
        { MoveName.Magnitude, "Magnitude" },
        { MoveName.MudSlap, "Mud-Slap" },
        { MoveName.MudShot, "Mud Shot" },
        { MoveName.MudSport, "Mud Sport" },
        { MoveName.SandAttack, "Sand-Attack" },
        { MoveName.SandTomb, "Sand Tomb" },

        // 🪨 Rock-type
        { MoveName.SandStorm, "Sandstorm" },
       
        // 💜 Normal-type
        { MoveName.Attract, "Attract" },
        { MoveName.SonicBoom, "Sonic Boom" },
        { MoveName.Harden, "Harden" },
        { MoveName.BellyDrum, "Belly Drum" },
        { MoveName.Bide, "Bide" },
        { MoveName.Covet, "Covet" },
        { MoveName.DoubleTeam, "Double Team" },
        { MoveName.Endeavor, "Endeavor" },
        { MoveName.Foresight, "Foresight" },
        { MoveName.FocusEnergy, "Focus Energy" },
        { MoveName.FalseSwipe, "False Swipe" },
        { MoveName.Flail, "Flail" },
        { MoveName.FurySwipes, "Fury Swipes" },
        { MoveName.Growl, "Growl" },
        { MoveName.Headbutt, "Headbutt" },
        { MoveName.HyperBeam, "Hyper Beam" },
        { MoveName.Leer, "Leer" },
        { MoveName.MeanLook, "Mean Look" },
        { MoveName.MorningSun, "Morning Sun" },
        { MoveName.MoonLight, "Moonlight" },
        { MoveName.OdorSleuth, "Odor Sleuth" },
        { MoveName.Pound, "Pound" },
        { MoveName.Protect, "Protect" },
        { MoveName.QuickAttack, "Quick Attack" },
        { MoveName.Scratch, "Scratch" },
        { MoveName.Screech, "Screech" },
        { MoveName.Slam, "Slam" },
        { MoveName.Slash, "Slash" },
        { MoveName.Supersonic, "Supersonic" },
        { MoveName.TailWhip, "Tail Whip" },
        { MoveName.Tackle, "Tackle" },
        { MoveName.TakeDown, "Take Down" },
        { MoveName.Whirlwind, "Whirlwind" },

        // 💀 Poison-type
        { MoveName.PoisonFang, "Poison Fang" },
        { MoveName.PoisonSting, "Poison Sting" },
        { MoveName.Toxic, "Toxic" },

        // 🔮 Psychic-type
        { MoveName.Agility, "Agility" },
        { MoveName.Confusion, "Confusion" },
        { MoveName.LightScreen, "Light Screen" },
        { MoveName.Psybeam, "Psybeam" },
        { MoveName.Reflect, "Reflect" },
        { MoveName.Rest, "Rest" },

        // 🌊 Water-type
        { MoveName.HydroPump, "Hydro Pump" },
        { MoveName.MuddyWater, "Muddy Water" },
        { MoveName.WaterGun, "Water Gun" },
        { MoveName.Whirlpool, "Whirlpool" },
        { MoveName.RainDance, "Rain Dance" },
        { MoveName.WaterSport, "Water Sport" },
        { MoveName.Surf, "Surf" },
        
        // 👻 Ghost-type
        { MoveName.Astonish, "Astonish" },
        { MoveName.ConfuseRay, "Confuse Ray" },

        // ❄️ Ice-type
        { MoveName.Haze, "Haze" },
        { MoveName.Hail, "Hail" },
        { MoveName.IceBeam,"Ice Beam" },
        
        // 🌑 Dark-type
        { MoveName.Bite, "Bite" },
        { MoveName.Crunch, "Crunch" },
        { MoveName.FaintAttack, "Faint Attack" },
        { MoveName.Pursuit, "Pursuit" }
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
    public static string GetShortStatName(Stat stat)
    {
        if (stat == Stat.SpecialAttack)
            return "SpAtk";
        if (stat == Stat.SpecialDefense)
            return "SpDef";
        if (stat == Stat.Defense)
            return "Def";
        if (stat == Stat.Attack)
            return "Atk";
        if (stat == Stat.Speed)
            return "Spd";
        if (stat == Stat.Accuracy)
            return "Acc";
        if (stat == Stat.Evasion)
            return "Eva";
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
    SandStorm,
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

//ONLY ADD NEW MOVES AT THE BOTTOM
public enum MoveName
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
    //new
    PinMissile,SunnyDay, Hail,WaterSport,RainDance,SleepPowder
    ,IceBeam,
    BulletSeed,
    BrickBreak,
    AerialAce,
    Surf,
    Fly
}
