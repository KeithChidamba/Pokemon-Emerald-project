using System.Collections.Generic;
public static class NameDB
{
    public enum LearnSetMove
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
        Dig,                // needs making and add to tm slot
        Earthquake,
        Magnitude,
        MudSlap,
        MudShot,
        MudSport,
        SandAttack,
        SandTomb,
        
        //Rock
        SandStorm,              // needs making with weather needs making and add to tm slot
        
        // 💜 Normal-type
        Attract,                // needs making after affection system
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
        Whirlwind,              //needs making
        
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

    public enum HM
    {
        Surf,
        Fly//needs making
    }

    public enum TM
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
    }
    public static string GetMoveName(LearnSetMove moveName)
    {
        return learnSetMoveNames[moveName];
    }
    public static Dictionary<LearnSetMove, string> learnSetMoveNames = new()
    {
        // 🐞 Bug-type
        { LearnSetMove.FuryCutter, "Fury Cutter" },
        { LearnSetMove.LeechLife, "Leech Life" },
        { LearnSetMove.SilverWind, "Silver Wind" },
        { LearnSetMove.StringShot, "String Shot" },
        { LearnSetMove.PinMissile, "Pin Missile" },
        // 🐉 Dragon-type
        { LearnSetMove.DragonBreath, "Dragon Breath" },

        // ⚡ Electric-type
        { LearnSetMove.Thundershock, "Thundershock" },
        { LearnSetMove.ThunderWave, "Thunder Wave" },
        { LearnSetMove.Thunderbolt, "Thunderbolt" },
        { LearnSetMove.Thunder, "Thunder" },

        // 🥋 Fighting-type
        { LearnSetMove.BulkUp, "Bulk Up" },
        { LearnSetMove.Detect, "Detect" },
        { LearnSetMove.DoubleKick, "Double Kick" },
        { LearnSetMove.SkyUppercut, "Sky Uppercut" },

        // 🔥 Fire-type
        { LearnSetMove.BlazeKick, "Blaze Kick" },
        { LearnSetMove.Ember, "Ember" },
        { LearnSetMove.FirePunch, "Fire Punch" },
        { LearnSetMove.FireSpin, "Fire Spin" },
        { LearnSetMove.Flamethrower, "Flamethrower" },

        // 🛫 Flying-type
        { LearnSetMove.AirCutter, "Air Cutter" },
        { LearnSetMove.Gust, "Gust" },
        { LearnSetMove.MirrorMove, "Mirror Move" },
        { LearnSetMove.Peck, "Peck" },
        { LearnSetMove.WingAttack, "Wing Attack" },

        // 🌿 Grass-type
        { LearnSetMove.Absorb, "Absorb" },
        { LearnSetMove.GigaDrain, "Giga Drain" },
        { LearnSetMove.LeafBlade, "Leaf Blade" },
        { LearnSetMove.MegaDrain, "Mega Drain" },
        { LearnSetMove.StunSpore, "Stun Spore" },

        // 🌍 Ground-type
        { LearnSetMove.Dig, "Dig" },
        { LearnSetMove.Earthquake, "Earthquake" },
        { LearnSetMove.Magnitude, "Magnitude" },
        { LearnSetMove.MudSlap, "Mud-Slap" },
        { LearnSetMove.MudShot, "Mud Shot" },
        { LearnSetMove.MudSport, "Mud Sport" },
        { LearnSetMove.SandAttack, "Sand-Attack" },
        { LearnSetMove.SandTomb, "Sand Tomb" },

        // 🪨 Rock-type
        { LearnSetMove.SandStorm, "Sandstorm" },

        // 💜 Normal-type
        { LearnSetMove.Attract, "Attract" },
        { LearnSetMove.SonicBoom, "Sonic Boom" },
        { LearnSetMove.Harden, "Harden" },
        { LearnSetMove.BellyDrum, "Belly Drum" },
        { LearnSetMove.Bide, "Bide" },
        { LearnSetMove.Covet, "Covet" },
        { LearnSetMove.DoubleTeam, "Double Team" },
        { LearnSetMove.Endeavor, "Endeavor" },
        { LearnSetMove.Foresight, "Foresight" },
        { LearnSetMove.FocusEnergy, "Focus Energy" },
        { LearnSetMove.FalseSwipe, "False Swipe" },
        { LearnSetMove.Flail, "Flail" },
        { LearnSetMove.FurySwipes, "Fury Swipes" },
        { LearnSetMove.Growl, "Growl" },
        { LearnSetMove.Headbutt, "Headbutt" },
        { LearnSetMove.HyperBeam, "Hyper Beam" },
        { LearnSetMove.Leer, "Leer" },
        { LearnSetMove.MeanLook, "Mean Look" },
        { LearnSetMove.MorningSun, "Morning Sun" },
        { LearnSetMove.MoonLight, "Moonlight" },
        { LearnSetMove.OdorSleuth, "Odor Sleuth" },
        { LearnSetMove.Pound, "Pound" },
        { LearnSetMove.Protect, "Protect" },
        { LearnSetMove.QuickAttack, "Quick Attack" },
        { LearnSetMove.Scratch, "Scratch" },
        { LearnSetMove.Screech, "Screech" },
        { LearnSetMove.Slam, "Slam" },
        { LearnSetMove.Slash, "Slash" },
        { LearnSetMove.Supersonic, "Supersonic" },
        { LearnSetMove.TailWhip, "Tail Whip" },
        { LearnSetMove.Tackle, "Tackle" },
        { LearnSetMove.TakeDown, "Take Down" },
        { LearnSetMove.Whirlwind, "Whirlwind" },

        // 💀 Poison-type
        { LearnSetMove.PoisonFang, "Poison Fang" },
        { LearnSetMove.PoisonSting, "Poison Sting" },
        { LearnSetMove.Toxic, "Toxic" },

        // 🔮 Psychic-type
        { LearnSetMove.Agility, "Agility" },
        { LearnSetMove.Confusion, "Confusion" },
        { LearnSetMove.LightScreen, "Light Screen" },
        { LearnSetMove.Psybeam, "Psybeam" },
        { LearnSetMove.Reflect, "Reflect" },
        { LearnSetMove.Rest, "Rest" },

        // 🌊 Water-type
        { LearnSetMove.HydroPump, "Hydro Pump" },
        { LearnSetMove.MuddyWater, "Muddy Water" },
        { LearnSetMove.WaterGun, "Water Gun" },
        { LearnSetMove.Whirlpool, "Whirlpool" },

        // 👻 Ghost-type
        { LearnSetMove.Astonish, "Astonish" },
        { LearnSetMove.ConfuseRay, "Confuse Ray" },

        // ❄️ Ice-type
        { LearnSetMove.Haze, "Haze" },

        // 🌑 Dark-type
        { LearnSetMove.Bite, "Bite" },
        { LearnSetMove.Crunch, "Crunch" },
        { LearnSetMove.FaintAttack, "Faint Attack" },
        { LearnSetMove.Pursuit, "Pursuit" }
    };


    private static Dictionary<Ability, string> _abilityNames = new()
    {
        { Ability.Guts, "Guts" },
        { Ability.PickUp, "Pickup" },
        { Ability.Blaze, "Blaze" },
        { Ability.Levitate, "Levitate" },
        { Ability.Overgrow, "Overgrow" },
        { Ability.Torrent, "Torrent" },
        { Ability.ParalysisCombo, "Paralysis combo" },
        { Ability.ArenaTrap, "Arena Trap" },
        { Ability.Static, "Static" },
        { Ability.ShedSkin, "Shed skin" },
        { Ability.Swarm, "Swarm" },
        { Ability.InnerFocus, "Inner Focus" },
    };
    public enum Ability
    {
        Guts,PickUp,Blaze,Levitate,Overgrow,Torrent,ParalysisCombo,ArenaTrap
        ,Static,ShedSkin,Swarm,InnerFocus
    }
    public static string GetAbility(Ability ability)
    {
        return _abilityNames[ability];
    }
    public enum EvolutionStone
    {
        None,ThunderStone,FireStone,WaterStone,LeafStone
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
    public static string GetStatName(PokemonOperations.Stat stat)
    {
        if (stat == PokemonOperations.Stat.SpecialAttack)
            return "Special Attack";
        if (stat == PokemonOperations.Stat.SpecialDefense)
            return "Special Defense";
        return stat.ToString();
    }
}
