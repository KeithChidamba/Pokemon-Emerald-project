using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);
       
        _pokemonPartyHandler.Party[0].statusEffect = StatusEffect.Paralysis;
        
        //reset hp to measure heal effect
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].maxHp = 30;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].maxHp = 300;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].maxHp = 300;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
        
        AddTestCase("base Potion healed hp to 21",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 21);
        
        AddTestCase("Max Potion healed hp to max",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 300);

        AddTestCase(new List<TestCaseCondition>{
            new("Full Restore must heal hp to max", 
                () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 300),
            new ("And Pokemon must be healed of paralysis", 
                ()=> _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None)
            });
        
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
        testName = "Potion Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }
}