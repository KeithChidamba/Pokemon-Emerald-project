using System.Collections.Generic;
public static class NameDB
{
    public enum MoveName
    {
        Tackle,
        Pound,
        FocusEnergy,
        TailWhip,
        Harden,
        Headbutt,
        Protect,
        QuickAttack,
        Scratch,
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
        Absorb,
        BulletSeed,
        MudSlap,
        Magnitude,
        Earthquake,
        PoisonSting,
        Toxic,
        LightScreen,
        Reflect,
        Bubble,
        WaterGun,
        Surf
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

}
