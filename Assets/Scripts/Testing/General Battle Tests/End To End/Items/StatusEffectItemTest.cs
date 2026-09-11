using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectItemTest: EndToEndTest,IItemTestable,IBattleTestable
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    
    public override IEnumerator BeginTest(EndToEndTestData testData)
    {
        var battleTestData = (BattleItemUsageEndToEndTestData)testData;
        
        this.LoadItems(battleTestData.testItems);
        
        _pokemonPartyHandler.Party[0].statusEffect = StatusEffect.Burn;
        
        AddTestCaseScenario(1,()=>
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.statusHandler.GetStatusEffect(StatusEffect.Poison);
        });
        
        AddTestCase("Burn was healed with burn heal",
            () => _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None);

        AddTestCase("Poison was healed with full heal",
            () => _pokemonPartyHandler.Party[0].statusEffect == StatusEffect.None);
        
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
        testName = "Status Effect Item Test";
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
    }
}