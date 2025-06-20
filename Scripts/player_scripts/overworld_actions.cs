using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class overworld_actions : MonoBehaviour
{
    public Animation_manager manager;

    public bool fishing;
    [SerializeField] private bool pokemonBitingPole;
    public bool doingAction;
    public bool usingUI;
    public Encounter_Area fishingArea;
    public static overworld_actions Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Update()
    {

        if (usingUI)
        {
            Player_movement.Instance.canMove = false;
            return;
        }
        if (doingAction)
            Player_movement.Instance.canMove = false;
        
        if (pokemonBitingPole & Input.GetKeyDown(KeyCode.F))
        {
            pokemonBitingPole = false;
            Encounter_handler.Instance.TriggerEncounter(fishingArea);
        }
        if (fishing)
        {
            doingAction = true;
            manager.ChangeAnimationState(manager.fishingIdle);
            if (Input.GetKeyDown(KeyCode.Q))
                ResetFishingAction();
        }
    }

    IEnumerator TryFishing()
    {
        var random = Utility.RandomRange(1, 11);
        yield return new WaitForSeconds(1f);
        if (!fishing) yield break;
        if (random < 5)
        {
            pokemonBitingPole = true;
            Dialogue_handler.Instance.DisplayInfo("Oh!, a Bite!, Press F","Details");
            yield return new WaitForSeconds( (2 * (random/10f) ) + 1f);
            if (pokemonBitingPole)
            {
                Dialogue_handler.Instance.DisplayInfo("It got away","Details");
                ResetFishingAction();
            }
        }
        else
        {
            Dialogue_handler.Instance.DisplayInfo("Dang...nothing","Details");
            ResetFishingAction();
        }
    }
    void StartFishingAction()
    {
        fishing = true;
        StartCoroutine(TryFishing());
    }
    public void ResetFishingAction()
    {
        fishing = false;
        pokemonBitingPole = false;
        Invoke(nameof(ActionReset), 0.8f);
        manager.ChangeAnimationState(manager.fishingEnd);
        Dialogue_handler.Instance.EndDialogue();
    }
    void ActionReset()
    {
        doingAction = false;
        Player_movement.Instance.canMove = true;
    }
}
