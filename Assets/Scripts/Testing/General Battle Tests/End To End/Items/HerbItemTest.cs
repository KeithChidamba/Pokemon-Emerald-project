using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HerbItemTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);

        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //energy powder
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].maxHp = 80;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
        //energy root
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].maxHp = 300;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
       //heal powder
        AddTestCaseScenario(()=>
        {
            player.statusHandler.GetStatusEffect(StatusEffect.Paralysis);
        });
        //revival herb
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[1].maxHp = 300;
            _pokemonPartyHandler.Party[1].hp = 0;
        });
                
        AddTestCase("Energy powder must healed hp to 51",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 51);
        
        AddTestCase("Energy Root must healed hp to 201",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 201);
                
        AddTestCase("Paralysis was healed heal powder",
            () => _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None);
        
        AddTestCase("Revival herb must revive partner to max health",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[1].hp) == 300);
        
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
        testName = "Herb Item Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
    }
}