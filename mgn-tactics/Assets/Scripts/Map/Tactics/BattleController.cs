﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/**
 * Responsible for user input and rendering during a battle. Control flow is actually handled by
 * the Battle class.
 */
[RequireComponent(typeof(Map))]
public class BattleController : MonoBehaviour {

    private const string ListenerId = "BattleControllerListenerId";

    // properties required upon initializion
    public Battle battle;

    // all of these behaviors read + managed internally
    public Map map { get { return GetComponent<Map>(); } }
    public Cursor cursor { get; private set; }
    public DirectionCursor dirCursor { get; private set; }

    // internal state
    private Dictionary<BattleUnit, BattleEvent> dolls;

    // === INITIALIZATION ==========================================================================

    public void Start() {
        AddUnitsFromMap();

        cursor = Cursor.GetInstance();
        cursor.gameObject.transform.SetParent(GetComponent<Map>().objectLayer.transform);
        cursor.gameObject.SetActive(false);

        dirCursor = DirectionCursor.GetInstance();
        dirCursor.gameObject.transform.SetParent(GetComponent<Map>().objectLayer.transform);
        dirCursor.gameObject.SetActive(false);
    }

    private void AddUnitsFromMap() {
        dolls = new Dictionary<BattleUnit, BattleEvent>();
        foreach (BattleEvent battler in map.GetEvents<BattleEvent>()) {
            BattleUnit unit = battle.AddUnitFromSerializedUnit(battler.unitData, 
                battler.GetComponent<MapEvent>().position);
            battler.Setup(this, unit);
            dolls[unit] = battler;
        }
    }

    // === GETTERS AND BOOKKEEPING =================================================================

    public BattleEvent GetDollForUnit(BattleUnit unit) {
        return dolls[unit];
    }

    public BattleUnit GetUnitAt(Vector2Int position) {
        foreach (MapEvent mapEvent in map.GetEventsAt(position)) {
            if (mapEvent.GetComponent<BattleEvent>() != null) {
                return mapEvent.GetComponent<BattleEvent>().unit;
            }
        }
        return null;
    }

    // === STATE MACHINE ===========================================================================

    public IEnumerator TurnBeginAnimationRoutine(Alignment align) {
        yield break;
    }

    public IEnumerator TurnEndAnimationRoutine(Alignment align) {
        List<IEnumerator> routinesToRun = new List<IEnumerator>();
        foreach (BattleUnit unit in battle.UnitsByAlignment(align)) {
            routinesToRun.Add(unit.battler.PostTurnRoutine());
        }
        yield return CoUtils.RunParallel(routinesToRun.ToArray(), this);
    }
    
    // cancelable, awaits user selecting a unit that matches the rule
    public IEnumerator SelectUnitRoutine(Result<BattleUnit> result, 
            Func<BattleUnit, bool> rule, 
            bool allowCancel=true) {
        cursor.gameObject.SetActive(true);
        while (!result.finished) {
            Result<Vector2Int> locResult = new Result<Vector2Int>();
            yield return cursor.AwaitSelectionRoutine(locResult);
            if (locResult.canceled && allowCancel) {
                result.Cancel();
                break;
            }
            BattleUnit unit = GetUnitAt(locResult.value);
            if (unit != null && rule(unit)) {
                result.value = unit;
            }
        }
        cursor.gameObject.SetActive(false);
    }

    // selects an adjacent unit to the actor (provided they meet the rule), cancelable
    public IEnumerator SelectAdjacentUnitRoutine(Result<BattleUnit> result,
                BattleUnit actingUnit,
                Func<BattleUnit, bool> rule,
                bool canCancel = true) {
        yield return dirCursor.SelectAdjacentUnitRoutine(result, actingUnit, rule, canCancel);
    }

    // === GAMEBOARD AND GRAPHICAL INTERACTION =====================================================

    public Cursor SpawnCursor(Vector2Int location) {
        cursor.gameObject.SetActive(true);
        cursor.GetComponent<MapEvent>().SetLocation(location);
        return cursor;
    }

    public void DespawnCursor() {
        cursor.gameObject.SetActive(false);
    }

    public void TargetCameraToLocation(Vector2Int loc) {
        TacticsCam.Instance().SetTargetLocation(loc, map.terrain.HeightAt(loc));
    }

    public SelectionGrid SpawnSelectionGrid() {
        SelectionGrid grid = SelectionGrid.GetInstance();
        grid.gameObject.transform.SetParent(GetComponent<Map>().transform);
        return grid;
    }

    public void MoveCursorToDefaultUnit() {
        BattleUnit defaultHero = battle.GetFaction(Alignment.Hero).NextMoveableUnit();
        cursor.GetComponent<MapEvent>().SetLocation(defaultHero.location);
    }
}