using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BerryItemTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        //oran
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].maxHp = 30;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
        //cherri
        AddTestCaseScenario(()=>
        {
            player.statusHandler.GetStatusEffect(StatusEffect.Paralysis);
        });
        //persim
        AddTestCaseScenario(()=>
        {
            player.isConfused = true;
        });
        //lum[full heal]
        AddTestCaseScenario(()=>
        {
            player.statusHandler.GetStatusEffect(StatusEffect.Poison);
            player.isConfused = true;
        });
        //leppa
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].moveSet[0].powerpoints = 0;
        });
        
        AddTestCase("oran berry must healed hp to 6",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 6);
        
        AddTestCase("Paralysis was healed with cherri berry",
            () => _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None);
        
        AddTestCase("Persim berry must heal confusion", () => !player.isConfused);
        
        AddTestCase(new List<TestCaseCondition>{
            new("Lum berry must heal confusion", () => !player.isConfused),
            new ("Lum berry must heal poison", 
                ()=> _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None)
        });
        
        AddTestCase("Leppa berry must restore powerpoints",
            () => _pokemonPartyHandler.Party[0].moveSet[0].powerpoints == 10);
        
        this.StartBattle(battleTestData,this);
        yield return null;
    }
    protected override void EndTest()
    { 
        this.EndBattle();
    }
    public override void Inject(ServiceContainer container)
    {
        serviceContainer = container;
        testName = "Berry Item Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
    }
}