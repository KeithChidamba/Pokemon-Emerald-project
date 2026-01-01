

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DestinationPointer : MonoBehaviour
{
    private Image pointerUIImage;
    [SerializeField] float edgePadding = 20f;

    private Transform player;
    private Camera cam;
    private RectTransform canvasRect;

    public void LoadPointer()
    {
        player = Player_movement.Instance.playerObject.transform;
        cam = Camera.main; 
        pointerUIImage = Game_ui_manager.Instance.destinationPointerUI;
        canvasRect = pointerUIImage.canvas.GetComponent<RectTransform>();
    }

    public void RemovePointer()
    {
        pointerUIImage.gameObject.SetActive(false);
    }
    void Update()
    {
        float deltaY = transform.position.y - player.position.y;
        float absY = Mathf.Abs(deltaY);

        float camHalfHeight = cam.orthographicSize;

        // Hide when within view
        if (absY <= camHalfHeight)
        {
            pointerUIImage.gameObject.SetActive(false);
            Debug.Log("hidden");
            return;
        }

        pointerUIImage.gameObject.SetActive(true);

        bool isAbove = deltaY > 0f;

        float canvasHalfHeight = canvasRect.rect.height / 2f;

        float yPos = isAbove
            ? canvasHalfHeight - edgePadding
            : -canvasHalfHeight + edgePadding;

        var rt = pointerUIImage.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPos);
    }
}
