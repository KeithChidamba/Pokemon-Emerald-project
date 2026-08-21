
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public ControlEvent controlEvent;

    public void OnPointerDown(PointerEventData eventData)
    {
        InputSourceHandler.TriggerPress(controlEvent);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputSourceHandler.TriggerRelease(controlEvent);
    }
}
