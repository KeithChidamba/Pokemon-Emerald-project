using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask Interactable;
    [SerializeField] Transform interaction_point;
    [SerializeField] Dialogue_handler dialogue;
    [SerializeField] float detect_distance=0.15f;
    [SerializeField] overworld_actions actions;
    private void Start()
    {
        Interactable = 1 << LayerMask.NameToLayer("Interactable");
    }
    void Update()
    {
        CheckFront();
    }
    void CheckFront()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, interaction_point.forward, detect_distance, Interactable);
        if (hit.transform && !dialogue.displaying && !actions.using_ui)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (hit.transform.GetComponent<Overworld_interactable>() != null)
                {
                    dialogue.Current_interaction = hit.transform.GetComponent<Overworld_interactable>().interaction;
                    dialogue.Display(dialogue.Current_interaction);
                }
            }
            if (Input.GetKeyDown(KeyCode.Q) && !actions.doing_action)
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                {
                   dialogue.Write_Info("Would you like to fish for pokemon", "Options", "Fish", "fishing...","","Yes","No");
                }
                else
                {
                    dialogue.Write_Info("Cant fish here", "Info");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Q) && !hit.transform)
        {
            dialogue.Write_Info("Cant fish here", "Info");
        }
    }
}
