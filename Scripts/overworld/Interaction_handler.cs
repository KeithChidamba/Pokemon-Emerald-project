using System.Collections;
using UnityEngine;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask interactable;
    [SerializeField] Transform interactionPoint;
    [SerializeField] float detectDistance=0.15f;
    private bool _canCheckForInteraction;
    private bool _stopInteractions;
    private bool _interactionCooldown;
    public static Interaction_handler Instance;
    private void Awake()
    {
        interactable = 1 << LayerMask.NameToLayer("Interactable");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _stopInteractions = false;
        _canCheckForInteraction = true;
    }
    void Update()
    {
        if(!Dialogue_handler.Instance.displaying && !_stopInteractions)
        {
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Q))
                if(_canCheckForInteraction)
                    RaycastForInteraction();
        }

        if ((Input.GetKeyUp(KeyCode.F) || Input.GetKeyUp(KeyCode.Q)) && !_canCheckForInteraction)
        {
            _canCheckForInteraction = true;
        }
    }
    public void DisableInteraction()
    {
        _stopInteractions = true;
    }
    public void AllowInteraction()
    {
        if (_interactionCooldown) return;
        _interactionCooldown = true;
        StartCoroutine(SetInteractionAllowed());
    }

    private IEnumerator SetInteractionAllowed()
    {
        yield return new WaitForSeconds(0.5f);
        _stopInteractions = false;
        _interactionCooldown = false;
    }
    void RaycastForInteraction()
    {
        _canCheckForInteraction = false;
        var hit = Physics2D.Raycast(interactionPoint.position, interactionPoint.forward, detectDistance, interactable);
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
                    Dialogue_handler.Instance.DisplayDetails("Cant fish here");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Q) && !hit.transform)
        {
            Dialogue_handler.Instance.DisplayDetails("Cant fish here");
        }
    }
}
