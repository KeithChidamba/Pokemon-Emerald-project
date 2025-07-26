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
        { MoveName.Supersonic, "Supersonic" },
        { MoveName.MeanLook, "Mean Look" },
        { MoveName.Tackle, "Tackle" },
        { MoveName.Pound, "Pound" },
        { MoveName.FocusEnergy, "Focus Energy" },
        { MoveName.TailWhip, "Tail Whip" },
        { MoveName.Harden, "Harden" },
        { MoveName.Headbutt, "Headbutt" },
        { MoveName.Protect, "Protect" },
        { MoveName.QuickAttack, "Quick Attack" },
        { MoveName.Scratch, "Scratch" },
        { MoveName.LeechLife, "Leech Life" },
        { MoveName.BugBite, "Bug Bite" },
        { MoveName.StringShot, "String Shot" },
        { MoveName.Bite, "Bite" },
        { MoveName.Crunch, "Crunch" },
        { MoveName.Thundershock, "Thundershock" },
        { MoveName.ThunderWave, "Thunder Wave" },
        { MoveName.Thunderbolt, "Thunderbolt" },
        { MoveName.BrickBreak, "Brick Break" },
        { MoveName.DoubleKick, "Double Kick" },
        { MoveName.Ember, "Ember" },
        { MoveName.Flamethrower, "Flamethrower" },
        { MoveName.WingAttack, "Wing Attack" },
        { MoveName.AirCutter, "Air Cutter" },
        { MoveName.ArialAce, "Arial Ace" },
        { MoveName.Absorb, "Absorb" },
        { MoveName.BulletSeed, "Bullet Seed" },
        { MoveName.MudSlap, "Mud Slap" },
        { MoveName.Magnitude, "Magnitude" },
        { MoveName.Earthquake, "Earthquake" },
        { MoveName.PoisonSting, "Poison Sting" },
        { MoveName.Toxic, "Toxic" },
        { MoveName.PoisonFang, "Poison Fang" },
        { MoveName.LightScreen, "Light Screen" },
        { MoveName.Reflect, "Reflect" },
        { MoveName.Bubble, "Bubble" },
        { MoveName.WaterGun, "Water Gun" },
        { MoveName.Astonish, "Astonish" },
        { MoveName.ConfuseRay, "Confuse Ray" },
        { MoveName.Haze, "Haze" },
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
