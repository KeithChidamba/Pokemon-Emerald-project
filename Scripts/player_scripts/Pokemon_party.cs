
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum PartyUsage
{
    SwapOut,ItemUsage,General
}
public class Pokemon_party : MonoBehaviour,IInjectable
{
    private List<Pokemon> party = new ();
    public IReadOnlyList<Pokemon> Party => party;
    public int selectedMemberIndex;
    [SerializeField]private int memberToMove;
    public readonly int maxNumMembers = 6;
    private int _currentStepCount;
    
    public bool moving;
    public Pokemon_party_member[] memberCards;
    public GameObject partyUI;
    public GameObject memberSelector;
    public GameObject optionSelector;
    public Image cancelButton;

    public GameObject[] partyOptions;
    public GameObject partyOptionsParent;
    public Text partyUsageText;
    public event Action<int> OnMemberSelected;
    public PartyUsage currentUsage;
    
    private Dialogue_handler _dialogueHandler;
    private InputStateHandler _inputStateHandler;
    private Battle_handler _battleHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private pokemon_storage _pokemonStorageHandler;
    private BattleIntro _battleIntroHandler;
    private Player_movement _playerMovementHandler;
    private PokemonPartyInputService _partyInputService;
    private PokemonOperations _pokemonOperationsHandler;
    private Game_Load _gameLoadingHandler;
    private Area_manager _areaHandler;
    private Game_ui_manager _gameUIHandler;
    
    public void Inject(ServiceContainer container)
    {
        _playerMovementHandler = container.Resolve<Player_movement>();
        _partyInputService = container.Resolve<PokemonPartyInputService>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
        _battleIntroHandler = container.Resolve<BattleIntro>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        _gameLoadingHandler = container.Resolve<Game_Load>();
        _areaHandler = container.Resolve<Area_manager>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _playerMovementHandler.OnNewTile += CheckPoisonTickEffect;
    }
    private void CheckPoisonTickEffect()
    {
        _currentStepCount++;
        if (_currentStepCount < 4) return;
        _currentStepCount = 0;
        StartCoroutine(CheckMembers());

        IEnumerator CheckMembers()
        {
            foreach(var member in party)
            {
                if (member.hp == 0) continue;
            
                if (member.statusEffect == StatusEffect.Poison)
                {
                    //no need for hp loss animation since this only happens outside ui
                    member.hp--;
                    if (member.hp == 0)
                    {
                        _dialogueHandler.DisplayDetails($"{member.pokemonDisplayName} has fainted");
                        yield return _dialogueHandler.WaitForDialogueCompletion();
                    }
                    if (GetLivingPokemon().Count == 0)
                    {
                        _dialogueHandler.DisplayDetails("All your Pokemon have fainted");
                        yield return _dialogueHandler.WaitForDialogueCompletion();
                        yield return _gameUIHandler.FadeInBlackScreen();
                        HealPartyPokemon();
                        _areaHandler.TeleportToArea(AreaName.PokeCenter);
                        yield return new WaitForSecondsRealtime(1f);
                        _gameUIHandler.RemoveBlackScreen();
                        break;
                    }
                }
            }
        }
       
    }
    
    public void UpdatePartyUsageMessage(string message)
    {
        partyUsageText.text = message;
    }
    public void ValidatePartyExit()
    {
        if (currentUsage==PartyUsage.SwapOut) return;
        _inputStateHandler.ResetRelevantUi(InputStateName.PokemonPartyNavigation,true);
    }

    public void CheckStateUpdate(InputState currentState)
    {
        if (currentState.stateName != InputStateName.PokemonPartyNavigation 
            && currentState.stateName != InputStateName.PokemonPartyItemUsage)
            return;
        _inputStateHandler.OnSelectionIndexChanged += UpdateCancelButton;
    }
    private void UpdateCancelButton(int currentIndex)
    {
        cancelButton.sprite = currentIndex < party.Count? 
            memberCards[0].pokeballClosedImage.sprite
                :memberCards[0].pokeballOpenImage.sprite;
    }
    public List<Pokemon> GetLivingPokemon()
    {
        return Party.Where(pokemon => pokemon.hp > 0).ToList();
    }

    public void AddMemberFromSystemProcess(Pokemon pokemon)
    {
        party.Add(pokemon);
    }
    private bool IsValidSwap(int memberIndex,bool swappingIn)
    {
        if (_turnBasedCombatHandler.ContainsSwitch(memberIndex))
        {
            _dialogueHandler.DisplayDetails(party[memberIndex].pokemonDisplayName +
                                                     " is already going to be sent out");
            return false;
        }

        var memberSelectionLimit = (int)BattleParticipantKey.PlayerPartner;
        bool atIndexSelectionLimit = memberIndex == 0 ||
                                     _battleHandler.isDoubleBattle &&
                                     memberIndex <= memberSelectionLimit;
       
        if (atIndexSelectionLimit)
        {
            var swapIn = GetParticipantFromIndex(memberIndex);
            _dialogueHandler.DisplayDetails(swapIn.pokemon.pokemonDisplayName +
                                                     " is already in battle");
            return false;
        }
        var currentParticipant = (_battleHandler.isDoubleBattle && swappingIn)
            ?_battleHandler.GetCurrentParticipant() 
            : _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        if (!currentParticipant.canEscape && swappingIn)
        {
            _dialogueHandler.DisplayDetails(currentParticipant.pokemon.pokemonDisplayName +
                                                     " is trapped");
            return false;
        }
        return true;
    }
    public void BeginMemberSwap(int memberIndex)
    {
        if (_battleHandler.BattleInProgress)
        {//cant swap in a member who is already fighting
            var currentParticipant = _battleHandler.GetCurrentParticipant();
            if (!IsValidSwap(memberIndex,true))
            {
               return;
            }
            _battleHandler.SetPlayerTurnUsage(PlayerTurnUsage.SwitchPokemonIn);

            var switchData = new SwitchOutData(_turnBasedCombatHandler.CurrentTurnIndex
                ,memberIndex,currentParticipant);
            _turnBasedCombatHandler.SaveSwitchTurn(switchData);
            
            _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
            selectedMemberIndex = 0;
        }
        else
        {
            if (party.Count > 1)
            {
                UpdatePartyUsageMessage("Select Pokemon to swap with");
                moving = true;
                memberToMove = memberIndex;
                partyOptionsParent.SetActive(false);
                _inputStateHandler.RemoveTopInputLayer(false);
            }
            else
                _dialogueHandler.DisplayDetails("There must be at least 2 Pokemon to swap");
        }
    }
    
    public void SelectMember(int memberIndex)
    {
        var selectedMember = memberCards[memberIndex];
        if (selectedMember.isEmpty) return;
        
        if (currentUsage == PartyUsage.SwapOut && selectedMember.pokemon.hp <= 0)
        {
            return;
        }
        switch (currentUsage)
        {
            case PartyUsage.SwapOut:
                if (!IsValidSwap(memberIndex,false)) return;
                OnMemberSelected?.Invoke(memberIndex);
                break;
            case PartyUsage.General:
                GeneralPartyUsage();
                break;
            case PartyUsage.ItemUsage:
                OnMemberSelected?.Invoke(memberIndex);
                break;
        }

        void GeneralPartyUsage()
        {
            if (selectedMember.isEmpty)
            {
                ClearSelectionUI();
            }
            else
            {
                selectedMemberIndex = memberIndex;
                if (moving)
                {
                    if(party[selectedMemberIndex] != party[memberToMove])
                    {
                        SwapMembers(memberToMove);
                    }
                }
                else
                {
                    ClearSelectionUI();
                    partyOptionsParent.SetActive(true);
                    _partyInputService.PokemonPartyOptions();
                }
            }
        }
    }
    public void ClearSelectionUI()
    {
        moving = false;
        partyOptionsParent.SetActive(false);
    }

    public void ResetPartyState()
    {
        currentUsage = PartyUsage.General;
        _inputStateHandler.OnStateChanged -= CheckStateUpdate;
        _inputStateHandler.OnSelectionIndexChanged -= UpdateCancelButton;
        cancelButton.sprite = memberCards[0].pokeballClosedImage.sprite;
    }

    private Battle_Participant GetParticipantFromIndex(int index)
    {
        var participantKey = index == 0 ? BattleParticipantKey.Player 
            : BattleParticipantKey.PlayerPartner;

        return _battleHandler.GetParticipant(participantKey);
    }
    public IEnumerator SwapMemberWithoutTurnUsage(int partyPosition)
    {
        (party[selectedMemberIndex], party[partyPosition]) = 
            (party[partyPosition], party[selectedMemberIndex]);

        var participant = GetParticipantFromIndex(selectedMemberIndex);
        
        var alivePokemon= GetLivingPokemon();
        
        UpdateUIAfterSwap();
        
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonParty);
        
        //this is called when ui doesnt shift so no need for changes in boolean
        yield return _battleIntroHandler.SwitchInPokemon(participant,alivePokemon[selectedMemberIndex],false);
        
        selectedMemberIndex = 0;
        participant.EndFaintEvent();
    }
    public void UpdateUIAfterSwap()
    {
        RefreshMemberCards();
        memberCards[0].ChangeVisibility(true);
        ClearSelectionUI();
        _partyInputService.UpdateHealthBarColors();
    }
    private void SwapMembers(int partyIndex)
    {
        var swapStore = party[selectedMemberIndex];
        var message = $"You swapped {party[partyIndex].pokemonDisplayName} with {swapStore.pokemonDisplayName}";
        party[selectedMemberIndex] = party[partyIndex];
        party[partyIndex] = swapStore;
        moving = false;
        if (_battleHandler.BattleInProgress)
        {
            var participant = GetParticipantFromIndex(selectedMemberIndex);
            var alivePokemon= GetLivingPokemon();
            StartCoroutine(_battleIntroHandler.SwitchInPokemon(participant,alivePokemon[selectedMemberIndex]));
        }
        else
            _dialogueHandler.DisplayDetails(message);
        UpdatePartyUsageMessage("Choose a pokemon");
        memberToMove = 0;
        selectedMemberIndex = 0;
        UpdateUIAfterSwap();
    }

    public void ClearTestState()
    {
        party.Clear();
    }
    public void AddTestMember(Pokemon pokemon)
    {
        var newPokemon = InstanceFactory.CreatePokemon(pokemon); 
        newPokemon.pokeballName = "Pokeball"; 
        newPokemon.hasTrainer = true;
        newPokemon.nickName = newPokemon.pokemonName;
        Debug.Log($"added test {newPokemon.pokemonName}");
        CompletePokemonAddition(newPokemon);
    }
    public void AddMemberAfterCatch(Pokemon pokemon, string pokeballType)
    {
        var newPokemon = InstanceFactory.CreatePokemon(pokemon); 
        newPokemon.pokeballName = pokeballType; 
        newPokemon.hasTrainer = true;
        CompletePokemonAddition(newPokemon);
    }
    public void AddGiftMember(PokemonGiftInteractoinInfo giftData)
    {
        _pokemonOperationsHandler.CreateSpecificPokemon(GetNewMember,giftData.giftPokemon,giftData.pokemonLevel
            ,giftData.evolutionStageNumber);
        void GetNewMember(Pokemon newPokemon)
        {
            newPokemon.hasTrainer = true;
            newPokemon.pokeballName = "Pokeball"; 
            newPokemon.ChangeFriendshipLevel(120);
            newPokemon.captureInformation.levelCaptured = newPokemon.currentLevel;
            newPokemon.captureInformation.areaName = Utility.GetAreaName(_gameLoadingHandler.playerData.location);
            _pokemonOperationsHandler.SetupPokemonNaming(newPokemon, (result)=>CompletePokemonAddition(newPokemon));
        }
    }
    private void CompletePokemonAddition(Pokemon newPokemon)
    {
        if (party.Count<maxNumMembers)
        {
            party.Add(newPokemon);
        }
        else
            _pokemonStorageHandler.AddPokemonToStorage(newPokemon);
        _dialogueHandler.DisplayDetails("You got a " + newPokemon.pokemonDisplayName);
    }
  
    public void SortByFainted()
    {
        if (party.Count == 1) return;
        for (int i = 0; i < party.Count - 1; i++)
        {
            bool swapped = false;

            for (int j = 0; j < party.Count - i - 1; j++)
            {
                var current = party[j];
                var next = party[j + 1];

                // Swap only if current is fainted and next is not fainted
                if (current.hp <= 0 && next.hp > 0)
                {
                    (party[j], party[j + 1]) = (next, current);
                    swapped = true;
                }
            }

            // Stop early if no swaps occurred
            if (!swapped)
                break;
        }
    }
    public void RefreshMemberCards()
    {
        for (int i=0;i<maxNumMembers;i++)
        {
            if (i < party.Count)
            {
                memberCards[i].pokemon = party[i];
                memberCards[i].ActivateUI();
            }
            else
            {
                memberCards[i].ResetUI();
            }
        }
    }
    public void RemoveMember(int partyPosition)
    {
        party.RemoveAt(partyPosition-1);
    }
    
    public void SwapIndexes(int partyPosition,int memberToSwapWith)
    {
        (party[partyPosition], party[memberToSwapWith]) = (party[memberToSwapWith], party[partyPosition]);
    }
    public void HealPartyPokemon()
    {
        foreach(var member in party)
        {
            member.hp = member.maxHp;
            foreach (var move in member.moveSet)
                move.powerpoints = move.maxPowerpoints;
            member.statusEffect = StatusEffect.None;
        }
    }
}
