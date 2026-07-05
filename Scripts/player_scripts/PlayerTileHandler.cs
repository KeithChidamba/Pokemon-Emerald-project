using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class PlayerTileHandler : MonoBehaviour,IInjectable
{
    [SerializeField] private Transform interactionPoint;
    public Tilemap encounterTilemap;
    public Tilemap areaSwitchTilemap;
    [SerializeField]private bool _repellingPokemon;
    [SerializeField]private int _repelDuration;
    
    public SpriteRenderer grassRenderer;
    [SerializeField] private Sprite[] grassSprites;
    
    private Player_movement _playerMovementHandler;
    private Encounter_handler  _encounterHandler;
    private Area_manager  _areaHandler;
    
    public void Inject(ServiceContainer container)
    {
        _encounterHandler = container.Resolve<Encounter_handler>();
        _playerMovementHandler = container.Resolve<Player_movement>();
        _areaHandler = container.Resolve<Area_manager>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _playerMovementHandler.OnNewTile += CheckGrass;
        _playerMovementHandler.OnNewTile += UseRepel;
        _playerMovementHandler.OnNewTile += SwitchArea;
    }

    public void ActivateRepel(int numSteps)
    {
        _repelDuration = numSteps;
        _repellingPokemon = true;
    }

    private void UseRepel()
    {
        if (_repellingPokemon)
        {
            _repelDuration--;
            _repellingPokemon = _repelDuration > 0;
        }
    }
    private void SwitchArea()
    {
        var tile = FindTileAtPosition<AreaSwitchTile>(areaSwitchTilemap,transform.position);
        if (tile == null) return;
        _areaHandler.SwitchToArea(tile.areaTransitionData.areaName);
    }
    
    private IEnumerator AnimateGrass()
    {
        _playerMovementHandler.characterSpriteMaskRenderer.gameObject.SetActive(true);
        grassRenderer.gameObject.SetActive(true);
        grassRenderer.sprite = grassSprites[0];
        yield return new WaitForSecondsRealtime(0.05f);
        grassRenderer.sprite = grassSprites[1];
        yield return new WaitForSecondsRealtime(0.05f);
        grassRenderer.sprite = grassSprites[2];
        yield return new WaitForSecondsRealtime(0.05f);
        grassRenderer.sprite = grassSprites[3];
        yield return new WaitForSecondsRealtime(0.05f);
    }

    private void CheckGrass()
    {
        grassRenderer.gameObject.SetActive(false);
        _playerMovementHandler.characterSpriteMaskRenderer.gameObject.SetActive(false);
        var tile = FindTileAtPosition<EncounterTile>(encounterTilemap, transform.position);
        if (tile == null) return;
        
        var playerPosition = _playerMovementHandler.GetPlayerPosition();
        grassRenderer.transform.position = new Vector3(playerPosition.x + .5f, playerPosition.y + .35f);//accounts for sprite offset
        StartCoroutine(AnimateGrass());
        
        if (_repellingPokemon) return;

        var encounterTable = (NormalEncounteArea)tile.table;
        
        var encounterChance = _playerMovementHandler.usingBike ? 1.5f * encounterTable.encounterChance : encounterTable.encounterChance;
    
        var randomNumber = Random.Range(1, 101);
    
        if (randomNumber < encounterChance)
        {
            _playerMovementHandler.RestrictPlayerMovement(MovementRestrictor.Battle);
            _encounterHandler.TriggerEncounter((NormalEncounteArea)tile.table);
        }
    }
    public static T FindTileAtPosition<T>(Tilemap tilemap,Vector3 triggerPos) where T : TileBase
    {
        var worldPos = triggerPos;
        var cellPos = tilemap.WorldToCell(worldPos);
        return tilemap.GetTile<T>(cellPos);
    }
}

