using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;

public static class NameDB
{
    public enum MoveName
    {
        // 🐞 Bug-type
        BugBite,
        LeechLife,
        SilverWind,
        StringShot,

        // 🐉 Dragon-type
        DragonBreath,

        // ⚡ Electric-type
        Thundershock,
        ThunderWave,
        Thunderbolt,

        // 🥋 Fighting-type
        BrickBreak,
        DoubleKick,

        // 🔥 Fire-type
        Ember,
        Flamethrower,

        // 🛫 Flying-type
        AirCutter,
        ArialAce,
        Gust,
        WingAttack,

        // 🌿 Grass-type
        Absorb,
        BulletSeed,

        // 🌍 Ground-type
        Earthquake,
        Fissure,         // Flygon
        Magnitude,
        MudSlap,
        SandAttack,      // Trapinch
        SandTomb,        // Trapinch/Vibrava/Flygon

        // 💜 Normal-type
        Assist,
        Harden,
        Attract,         // Beautifly
        BellyDrum,       // Zigzagoon
        Bide,            // Silcoon/Cascoon
        Covet,           // Linoone
        DoubleTeam,
        Flail,           // Trapinch
        FocusEnergy,
        FurySwipes,
        Growl,
        Headbutt,
        HyperBeam,
        Leer,
        OdorSleuth,
        Pound,
        Protect,
        QuickAttack,
        Rest,
        Roar,            // Linoone
        Scratch,
        Screech,
        Slam,            // Dustox
        Slash,           // Flygon
        Supersonic,
        TailWhip,
        Tackle,
        Uproar,          // Beautifly, Linoone

        // 💀 Poison-type
        PoisonFang,      // Dustox
        PoisonSting,
        Toxic,

        // 🔮 Psychic-type
        Confusion,
        LightScreen,
        Reflect,

        // 🌊 Water-type
        Bubble,
        Surf,
        WaterGun,

        // 👻 Ghost-type
        Astonish,
        ConfuseRay,
        Haze,

        // 🌑 Dark-type
        Bite,
        Crunch,
        FaintAttack,
        MeanLook         // Zubat
    }
    public static string GetMoveName(MoveName name)
    {
        return _moveNames[name];
    }
public static Dictionary<MoveName, string> _moveNames = new()
{
    // Bug-type
    { MoveName.BugBite, "Bug Bite" },
    { MoveName.LeechLife, "Leech Life" },
    { MoveName.SilverWind, "Silver Wind" },
    { MoveName.StringShot, "String Shot" },

    // Dragon-type
    { MoveName.DragonBreath, "DragonBreath" },

    // Electric-type
    { MoveName.Thundershock, "ThunderShock" },
    { MoveName.ThunderWave, "Thunder Wave" },
    { MoveName.Thunderbolt, "Thunderbolt" },

    // Fighting-type
    { MoveName.BrickBreak, "Brick Break" },
    { MoveName.DoubleKick, "Double Kick" },

    // Fire-type
    { MoveName.Ember, "Ember" },
    { MoveName.Flamethrower, "Flamethrower" },

    // Flying-type
    { MoveName.AirCutter, "Air Cutter" },
    { MoveName.ArialAce, "Aerial Ace" },
    { MoveName.Gust, "Gust" },
    { MoveName.WingAttack, "Wing Attack" },

    // Grass-type
    { MoveName.Absorb, "Absorb" },
    { MoveName.BulletSeed, "Bullet Seed" },

    // Ground-type
    { MoveName.Earthquake, "Earthquake" },
    { MoveName.Fissure, "Fissure" },
    { MoveName.Magnitude, "Magnitude" },
    { MoveName.MudSlap, "Mud-Slap" },
    { MoveName.SandAttack, "Sand-Attack" },
    { MoveName.SandTomb, "Sand Tomb" },

    // Normal-type
    { MoveName.Assist, "Assist" },
    { MoveName.Attract, "Attract" },
    { MoveName.BellyDrum, "Belly Drum" },
    { MoveName.Bide, "Bide" },
    { MoveName.Covet, "Covet" },
    { MoveName.DoubleTeam, "Double Team" },
    { MoveName.Flail, "Flail" },
    { MoveName.FocusEnergy, "Focus Energy" },
    { MoveName.FurySwipes, "Fury Swipes" },
    { MoveName.Growl, "Growl" },
    { MoveName.Headbutt, "Headbutt" },
    { MoveName.HyperBeam, "Hyper Beam" },
    { MoveName.Leer, "Leer" },
    { MoveName.OdorSleuth, "Odor Sleuth" },
    { MoveName.Pound, "Pound" },
    { MoveName.Protect, "Protect" },
    { MoveName.QuickAttack, "Quick Attack" },
    { MoveName.Rest, "Rest" },
    { MoveName.Roar, "Roar" },
    { MoveName.Scratch, "Scratch" },
    { MoveName.Screech, "Screech" },
    { MoveName.Slam, "Slam" },
    { MoveName.Slash, "Slash" },
    { MoveName.Supersonic, "Supersonic" },
    { MoveName.TailWhip, "Tail Whip" },
    { MoveName.Tackle, "Tackle" },
    { MoveName.Uproar, "Uproar" },

    // Poison-type
    { MoveName.PoisonFang, "Poison Fang" },
    { MoveName.PoisonSting, "Poison Sting" },
    { MoveName.Toxic, "Toxic" },

    // Psychic-type
    { MoveName.Confusion, "Confusion" },
    { MoveName.LightScreen, "Light Screen" },
    { MoveName.Reflect, "Reflect" },

    // Water-type
    { MoveName.Bubble, "Bubble" },
    { MoveName.Surf, "Surf" },
    { MoveName.WaterGun, "Water Gun" },

    // Ghost-type
    { MoveName.Astonish, "Astonish" },
    { MoveName.ConfuseRay, "Confuse Ray" },
    { MoveName.Haze, "Haze" },

    // Dark-type
    { MoveName.Bite, "Bite" },
    { MoveName.Crunch, "Crunch" },
    { MoveName.FaintAttack, "Faint Attack" },
    { MoveName.MeanLook, "Mean Look" }
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
