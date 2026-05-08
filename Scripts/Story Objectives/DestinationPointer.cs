
using UnityEngine;
using UnityEngine.UI;

public class DestinationPointer : MonoBehaviour,IInjectable
{
    private Image pointerUIImage;
    [SerializeField] float edgePadding = 20f;
    
    private RectTransform canvasRect;
    public DestinationObjective objectiveData;
 
    public bool displaying;

    private OverworldState _overworldStateHandler;
    private Game_ui_manager _gameUIHandler;
    private Player_movement _playerMovementHandler;
    
    public void Inject(ServiceContainer container)
    {
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _overworldStateHandler = container.Resolve<OverworldState>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        gameObject.SetActive(true);
        OnInject();
    }

    private void OnInject()
    {
        _overworldStateHandler.OnObjectivesLoaded += CheckForRequiredObjective;
    }
    
    private void CheckForRequiredObjective()
    {
        if (_overworldStateHandler.HasObjective(objectiveData.name))
        {
            objectiveData.OnLoad += LoadPointer;
        }
    }
    private void LoadPointer()
    {
        objectiveData.OnLoad -= LoadPointer;
        displaying = true;
        pointerUIImage = _gameUIHandler.destinationPointerUI;
        canvasRect = pointerUIImage.canvas.GetComponent<RectTransform>();
        _playerMovementHandler.OnNewTile += ConfirmDestination;
        
    }

    private void ConfirmDestination()
    {
        var playerPos = _playerMovementHandler.GetPlayerPosition();
        if(playerPos == objectiveData.destinationPosition)
        {
            _playerMovementHandler.OnNewTile -= ConfirmDestination;
            objectiveData.ClearObjective();
            pointerUIImage.gameObject.SetActive(false);
            displaying = false;
        }
    }
    void Update()
    {
        if(!displaying)return;
        Vector3 viewportPos = _playerMovementHandler.playerCamera.WorldToViewportPoint(transform.position);

// Hide if inside view
        bool isInsideView =
            viewportPos.x >= 0f && viewportPos.x <= 1f &&
            viewportPos.y >= 0f && viewportPos.y <= 1f &&
            viewportPos.z > 0f;

        if (isInsideView)
        {
            pointerUIImage.gameObject.SetActive(false);
            return;
        }
        pointerUIImage.gameObject.SetActive(true);

// Clamp viewport position to screen edge
        float margin = 0.05f;

        float clampedX = Mathf.Clamp(viewportPos.x, margin, 1f - margin);
        float clampedY = Mathf.Clamp(viewportPos.y, margin, 1f - margin);

// Convert viewport → canvas space
        float canvasWidth  = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float xPos = (clampedX - 0.5f) * canvasWidth;
        float yPos = (clampedY - 0.5f) * canvasHeight;

// Apply padding toward center
        Vector2 dir = new Vector2(xPos, yPos).normalized;
        Vector2 paddedPos = new Vector2(xPos, yPos) - dir * edgePadding;

        pointerUIImage.rectTransform.anchoredPosition = paddedPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pointerUIImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
