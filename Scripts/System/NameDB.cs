using System.Collections.Generic;
public static class NameDB
{
    public enum MoveName
    {
        // 🐞 Bug-type
        FuryCutter,// needs making
        LeechLife,
        SilverWind,// needs making
        StringShot,
        
        // 🐉 Dragon-type
        DragonBreath,

        // ⚡ Electric-type
        Thundershock,
        ThunderWave,
        Thunderbolt,
        Thunder,
        
        // 🥋 Fighting-type
        BrickBreak,//TM
        BulkUp,
        Detect,// needs making
        DoubleKick,
        SkyUppercut,
        
        // 🔥 Fire-type
        BlazeKick,
        Ember,
        FirePunch,// needs making
        FireSpin,
        Flamethrower,
        
        // 🛫 Flying-type
        AirCutter,
        ArialAce,//TM
        Gust,// needs making
        MirrorMove,// needs making
        Peck,
        WingAttack,

        // 🌿 Grass-type
        Absorb,
        BulletSeed,//TM
        GigaDrain,
        LeafBlade,
        MegaDrain,
        StunSpore,// needs making
        
        // 🌍 Ground-type
        Dig,// needs making
        Earthquake,
        Magnitude,
        MudSlap,
        MudShot,
        MudSport,
        SandAttack,
        SandTomb,
        
        //Rock
        SandStorm,// needs making
        
        // 💜 Normal-type
        Attract,// needs making
        SonicBoom,
        Harden,
        BellyDrum,// needs making
        Bide,
        Covet,// needs making
        DoubleTeam,
        Endeavor,
        FocusEnergy,
        Foresight,
        FalseSwipe,// needs making
        Flail,// needs making
        FurySwipes,// needs making
        Growl,
        Headbutt,
        HyperBeam,
        Leer, 
        MeanLook,
        MorningSun,// needs making
        MoonLight,// needs making       
        OdorSleuth,// needs making
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
        Whirlwind, //needs making
        
        // 💀 Poison-type
        PoisonFang,
        PoisonSting,
        Toxic,

        // 🔮 Psychic-type
        Agility,
        Confusion,// needs making
        LightScreen,
        Psybeam,// needs making
        Reflect,
        Rest,
        
        // 🌊 Water-type
        HydroPump,
        MuddyWater,
        Surf,//HM
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
        Pursuit
    }
    public static string GetMoveName(MoveName name)
    {
        return _moveNames[name];
    }

    public static Dictionary<MoveName, string> _moveNames = new()
    {
        // 🐞 Bug-type
        { MoveName.FuryCutter, "Fury Cutter" },
        { MoveName.LeechLife, "Leech Life" },
        { MoveName.SilverWind, "Silver Wind" },
        { MoveName.StringShot, "String Shot" },

        // 🐉 Dragon-type
        { MoveName.DragonBreath, "Dragon Breath" },

        // ⚡ Electric-type
        { MoveName.Thundershock, "Thundershock" },
        { MoveName.ThunderWave, "Thunder Wave" },
        { MoveName.Thunderbolt, "Thunderbolt" },
        { MoveName.Thunder, "Thunder" },

        // 🥋 Fighting-type
        { MoveName.BrickBreak, "Brick Break" },
        { MoveName.BulkUp, "Bulk Up" },
        { MoveName.Detect, "Detect" },
        { MoveName.DoubleKick, "Double Kick" },
        { MoveName.SkyUppercut, "Sky Uppercut" },

        // 🔥 Fire-type
        { MoveName.BlazeKick, "Blaze Kick" },
        { MoveName.Ember, "Ember" },
        { MoveName.FirePunch, "Fire Punch" },
        { MoveName.FireSpin, "Fire Spin" },
        { MoveName.Flamethrower, "Flamethrower" },

        // 🛫 Flying-type
        { MoveName.AirCutter, "Air Cutter" },
        { MoveName.ArialAce, "Aerial Ace" },
        { MoveName.Gust, "Gust" },
        { MoveName.MirrorMove, "Mirror Move" },
        { MoveName.Peck, "Peck" },
        { MoveName.WingAttack, "Wing Attack" },

        // 🌿 Grass-type
        { MoveName.Absorb, "Absorb" },
        { MoveName.BulletSeed, "Bullet Seed" },
        { MoveName.GigaDrain, "Giga Drain" },
        { MoveName.LeafBlade, "Leaf Blade" },
        { MoveName.MegaDrain, "Mega Drain" },
        { MoveName.StunSpore, "Stun Spore" },

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
        { MoveName.Surf, "Surf" },
        { MoveName.WaterGun, "Water Gun" },
        { MoveName.Whirlpool, "Whirlpool" },

        // 👻 Ghost-type
        { MoveName.Astonish, "Astonish" },
        { MoveName.ConfuseRay, "Confuse Ray" },

        // ❄️ Ice-type
        { MoveName.Haze, "Haze" },

        // 🌑 Dark-type
        { MoveName.Bite, "Bite" },
        { MoveName.Crunch, "Crunch" },
        { MoveName.FaintAttack, "Faint Attack" },
        { MoveName.Pursuit, "Pursuit" }
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
}
