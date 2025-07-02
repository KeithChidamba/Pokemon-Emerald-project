using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask interactable;
    [SerializeField] Transform interactionPoint;
    [SerializeField] float detectDistance=0.15f;
    private bool _canCheckForInteraction;
    private bool _stopInteractions;
    public static Interaction_handler Instance;
    private void Start()
    {
        interactable = 1 << LayerMask.NameToLayer("Interactable");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Update()
    {
        if(!Dialogue_handler.Instance.displaying && !_stopInteractions)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Q))
                if (!_canCheckForInteraction)
                    StartCoroutine(DelayRayCast());
        }
        if(_canCheckForInteraction)
            RaycastForInteraction();
    }

    public void DisableInteraction()
    {
        _stopInteractions = true;
    }
    public void AllowInteraction()
    {
        //prevent this from being called while its waiting, replace with a coroutine
        Invoke(nameof(SetInteractionAllowed), 1f);
    }

    private void SetInteractionAllowed()
    {
        _stopInteractions = false;
    }
    IEnumerator DelayRayCast()
    {
        _canCheckForInteraction = true;
        yield return new WaitForSeconds(1f);
        _canCheckForInteraction = false;
    }
    void RaycastForInteraction()
    {
        var hit = Physics2D.Raycast(transform.position, interactionPoint.forward, detectDistance, interactable);
        if (hit.transform && !Dialogue_handler.Instance.displaying && !overworld_actions.Instance.usingUI)
        {
            var interactableObject = hit.transform.GetComponent<Overworld_interactable>();
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (interactableObject.interaction != null)
                    Dialogue_handler.Instance.StartInteraction(interactableObject);
            }
            if (Input.GetKeyDown(KeyCode.Q) && !overworld_actions.Instance.doingAction)
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                { 
                   overworld_actions.Instance.fishingArea = interactableObject.area;
                   Dialogue_handler.Instance.DisplayList("Would you like to fish for pokemon"
                       , "fishing...", new[]{ "Fish","" }, new[]{"Yes", "No"});
                }
                else
                {
                    Dialogue_handler.Instance.DisplayInfo("Cant fish here", "Info");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Q) && !hit.transform)
        {
            Dialogue_handler.Instance.DisplayInfo("Cant fish here", "Info");
        }
    }
}
