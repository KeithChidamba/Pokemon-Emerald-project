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
            _pokemonPartyHandler.Party[0].friendshipLevel = 200;
            _pokemonPartyHandler.Party[0].maxHp = 80;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
        //energy root
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].friendshipLevel = 200;
            _pokemonPartyHandler.Party[0].maxHp = 300;
            _pokemonPartyHandler.Party[0].hp = 1;
        });
       //heal powder
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[0].friendshipLevel = 200;
            player.statusHandler.GetStatusEffect(StatusEffect.Paralysis);
        });
        //revival herb
        AddTestCaseScenario(()=>
        {
            _pokemonPartyHandler.Party[1].friendshipLevel = 200;
            _pokemonPartyHandler.Party[1].maxHp = 300;
            _pokemonPartyHandler.Party[1].hp = 0;
        });
                
        AddTestCase(new List<TestCaseCondition>{
            new("Energy powder must healed hp to 51",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 51),
            new("Energy powder must reduce friendship by 5, expected [195]",
                ()=>_pokemonPartyHandler.Party[0].friendshipLevel == 195)
        });
        
        AddTestCase(new List<TestCaseCondition>{
            new("Energy Root must healed hp to 201",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[0].hp) == 201),
        new("Energy Root must reduce friendship by 10, expected [190]",
            ()=>_pokemonPartyHandler.Party[0].friendshipLevel == 190)
        });
                
        AddTestCase(new List<TestCaseCondition>{
            new("Paralysis was healed heal powder",
            () => _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None),
            new("Heal powder must reduce friendship by 5, expected [195]",
                ()=>_pokemonPartyHandler.Party[0].friendshipLevel == 195)
        });
        
        AddTestCase(new List<TestCaseCondition>{
            new("Revival herb must revive partner to max health",
            () => Mathf.FloorToInt(_pokemonPartyHandler.Party[1].hp) == 300),
            new("Revival herb must reduce friendship by 15, expected [185]",
                ()=>_pokemonPartyHandler.Party[1].friendshipLevel == 185)
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
        testName = "Herb Item Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
    }
}