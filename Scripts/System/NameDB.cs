using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;

public static class NameDB
{

    public enum MoveName
    {
        Supersonic,
        MeanLook,
        Tackle,
        Pound,
        FocusEnergy,
        TailWhip,
        Harden,
        Headbutt,
        Protect,
        QuickAttack,
        Scratch,
        LeechLife,
        BugBite,
        StringShot,
        Bite,
        Crunch,
        Thundershock,
        ThunderWave,
        Thunderbolt,
        BrickBreak,
        DoubleKick,
        Ember,
        Flamethrower,
        ArialAce,
        WingAttack,
        AirCutter,
        Absorb,
        BulletSeed,
        MudSlap,
        Magnitude,
        Earthquake,
        PoisonSting,
        Toxic,
        PoisonFang,
        LightScreen,
        Reflect,
        Bubble,
        WaterGun,
        Surf,
        Astonish,
        ConfuseRay,
        Haze
    }
    public static string GetMoveName(MoveName name)
    {
        return _moveNames[name];
    }
    public static Dictionary<MoveName, string> _moveNames = new()
    {
        { MoveName.Tackle, "Tackle" },
        { MoveName.Pound, "Pound" },
        { MoveName.FocusEnergy, "Focus Energy" },
        { MoveName.TailWhip, "Tail Whip" },
        { MoveName.Harden, "Harden" },
        { MoveName.Headbutt, "Headbutt" },
        { MoveName.Protect, "Protect" },
        { MoveName.QuickAttack, "Quick Attack" },
        { MoveName.Scratch, "Scratch" },
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
        { MoveName.ArialAce, "Arial Ace" },
        { MoveName.Absorb, "Absorb" },
        { MoveName.BulletSeed, "Bullet Seed" },
        { MoveName.MudSlap, "Mud Slap" },
        { MoveName.Magnitude, "Magnitude" },
        { MoveName.Earthquake, "Earthquake" },
        { MoveName.PoisonSting, "Poison Sting" },
        { MoveName.Toxic, "Toxic" },
        { MoveName.LightScreen, "Light Screen" },
        { MoveName.Reflect, "Reflect" },
        { MoveName.Bubble, "Bubble" },
        { MoveName.WaterGun, "Water Gun" },
        { MoveName.Surf, "Surf" }
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
