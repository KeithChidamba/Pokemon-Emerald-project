using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Interaction_handler : MonoBehaviour
{
    [SerializeField] LayerMask Interactable;
    [SerializeField] Transform interaction_point;
    [SerializeField] float detect_distance=0.15f;
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
        if (hit.transform && !Dialogue_handler.instance.displaying && !overworld_actions.instance.using_ui)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (hit.transform.GetComponent<Overworld_interactable>() != null)
                {
                    Dialogue_handler.instance.Current_interaction = hit.transform.GetComponent<Overworld_interactable>().interaction;
                    Dialogue_handler.instance.Display(Dialogue_handler.instance.Current_interaction);
                }
            }
            if (Input.GetKeyDown(KeyCode.Q) && !overworld_actions.instance.doing_action)
            {
                if (hit.transform.gameObject.CompareTag("Water"))
                {
                   Dialogue_handler.instance.Write_Info("Would you like to fish for pokemon", "Options", "Fish", "fishing...","","Yes","No");
                }
                else
                {
                    Dialogue_handler.instance.Write_Info("Cant fish here", "Info");
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Q) && !hit.transform)
        {
            Dialogue_handler.instance.Write_Info("Cant fish here", "Info");
        }
    }
}
