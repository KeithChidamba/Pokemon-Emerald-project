using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class PlayerCollisionHandler : MonoBehaviour,IInjectable
{
    [SerializeField] private Transform interactionPoint;
    public Tilemap encounterTilemap;
    public Tilemap areaSwitchTilemap;
    [SerializeField]private bool _repellingPokemon;
    [SerializeField]private int _repelDuration;
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
        _playerMovementHandler.OnNewTile += SwitchArea;
    }

    public void ActivateRepel(int numSteps)
    {
        _repelDuration = numSteps;
        _repellingPokemon = true;
        
    }
    private void SwitchArea()
    {
        var tile = FindTileAtPosition<AreaSwitchTile>(areaSwitchTilemap,transform.position);
        if (tile == null) return;
        _areaHandler.SwitchToArea(tile.areaTransitionData.areaName);
    }
    private void CheckGrass()
    {
        if (_repellingPokemon)
        {
            _repelDuration--;
            _repellingPokemon = _repelDuration > 0;
            return;
        }
        var tile = FindTileAtPosition<EncounterTile>(encounterTilemap,transform.position);
        if (tile == null) return;
        
        var encounterChance = _playerMovementHandler.runningInput ? 5 : 2;
        
        var randomNumber = Random.Range(1, 11);
        
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

