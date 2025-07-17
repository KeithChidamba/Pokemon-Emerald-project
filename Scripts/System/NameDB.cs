using System.Collections.Generic;
public static class NameDB
{
    public enum ItemName
    {
        Antidote,
        Awakening,
        BurnHeal,
        Calcium,
        Carbos,
        DireHit,
        EnergyPowder,
        EnergyRoot,
        FullHeal,
        GreatBall,
        GuardSpec,
        HealPowder,
        HpUp,
        HyperPotion,
        IceHeal,
        Iron,
        MaxRevive,
        ParalyzHeal,
        Pokeball,
        Potion,
        PpMax,
        PpUp,
        Protein,
        RevivalHerb,
        Revive,
        SuperPotion,
        XAccuracy,
        XAttack,
        XDefense,
        XSpecialAttack,
        XSpecialDefense,
        XSpeed,
        Zinc
    }
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


    public static string GetItemName(ItemName name)
    {
        return _itemNames[name];
    }
    public static Dictionary<ItemName, string> _itemNames = new()
    {
        { ItemName.Antidote, "Antidote" },
        { ItemName.Awakening, "Awakening" },
        { ItemName.BurnHeal, "Burn Heal" },
        { ItemName.Calcium, "Calcium" },
        { ItemName.Carbos, "Carbos" },
        { ItemName.DireHit, "Dire Hit" },
        { ItemName.EnergyPowder, "Energy Powder" },
        { ItemName.EnergyRoot, "Energy Root" },
        { ItemName.FullHeal, "Full Heal" },
        { ItemName.GreatBall, "Great Ball" },
        { ItemName.GuardSpec, "Guard Spec" },
        { ItemName.HealPowder, "Heal Powder" },
        { ItemName.HpUp, "HP Up" },
        { ItemName.HyperPotion, "Hyper Potion" },
        { ItemName.IceHeal, "Ice Heal" },
        { ItemName.Iron, "Iron" },
        { ItemName.MaxRevive, "Max Revive" },
        { ItemName.ParalyzHeal, "Paralyz Heal" },
        { ItemName.Pokeball, "Pokeball" },
        { ItemName.Potion, "Potion" },
        { ItemName.PpMax, "PP Max" },
        { ItemName.PpUp, "PP Up" },
        { ItemName.Protein, "Protein" },
        { ItemName.RevivalHerb, "Revival Herb" },
        { ItemName.Revive, "Revive" },
        { ItemName.SuperPotion, "Super Potion" },
        { ItemName.XAccuracy, "X Accuracy" },
        { ItemName.XAttack, "X Attack" },
        { ItemName.XDefense, "X Defense" },
        { ItemName.XSpecialAttack, "X Special Attack" },
        { ItemName.XSpecialDefense, "X Special Defense" },
        { ItemName.XSpeed, "X Speed" },
        { ItemName.Zinc, "Zinc" }
    };
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
