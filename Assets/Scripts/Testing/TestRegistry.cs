
public class TestRegistry
{
    //tests are ran in this order
    public EndToEndTest[] endToEndTests =
    {
        // new RareCandyTest(),
        // new FriendshipBerryTest(),
        // new EvolutionStoneTest(),
        // new EvVitaminTest(),
        // new ModifyPowerpointsTest(),
        // new HmAndTmTest(),
        // new ReviveOverworldTest(),
        // new EtherTest(),
        // new ReviveBattleTest(),
        // new PotionTest(),
        // new StatusEffectItemTest(),
        // new BerryItemTest(),
        // new HerbItemTest(),
        new XItemTest(),
        new GuardSpecItemTest(),
    };

    public IntegrationTest[] integrationTests = 
    {
        // //Held Items
        new ConsumableHeldItemUsageTest(),
        //  new ChoiceBandTest(),
        // //Special Move Logic
        //  new BideTest(),
        //  new HyperBeamTest(),
        //  new MirrorMoveTest(),
        //  new SilverwindBattleEndTest(),
        //  new SilverwindSwapTest(),
        //  new WhirlwindWildBattleTest(),
        //  new WhirlwindTrainerBattleTest(),
        //  new WhirlwindDoubleBattleTest(),
        //  new ThunderTest(),
        //  new Endeavor(),
        //  new RestTest(),
        //  new BellyDrumTest(),
        //  new CovetTest(),
        //  new FalseSwipeTest(),
        //  new FlailTest(),
        //  new FuryCutter(),
        //  new TakeDownTest(),
        //  new HazeTest(),
        //  new PursuitTest(),
        new BrickBreakTest(),
        // //Abilities
        //  new HealthBasedDamageBuffTest(),
        //  new StatusEffectDamageBuffTest(),
        //  new ShedSkinTest(),
        //  new StaticTest(),
        //  new ArenaTrapTest(),
        //  new LevitateTest(),
        //  new GutsTest(),
        //  new PickupTest(),
        //  new InnerFocusTest(),
        // //Battle system tests
        //  new TrapEffectTest(),
        //  new InfatuationEffectTest(),
        //  new FlinchEffectTest(),
        //  new StruggleTest(),
        //  new StatChangeApplicationTest(),
        //  new StatusEffectTest(),
        //  new WeatherDamageTest(),
        //  new OnFieldDamageModificationTest(),
        // // Move Based Tests
        //  new SpecificMoveDamageTest(),
        //  new SemiInvulnerableSingleBattleTest(),
        //  new SemiInvulnerableDoubleBattleTest(),
        //  new IdentifyTargetMoveTest(),
        //  new MultiTargetDamageTest(),
        new CreateBarrierMoveTest(),
        //  new HealthDrainTest(),
        //  new HealFromWeatherTest(),
        //  new DamageProtectionMoveTest(),
        //  new ConsecutiveMoveTest()
    };
}
