﻿using UnityEngine;
using System.Collections;
using System;

public class WalkRouteTargeter : Targeter {

    private Vector2Int targetLocation;

    protected override IEnumerator InternalExecuteRoutine(SkillResult result) {
        FreeCursor cursor = controller.SpawnCursor(actor.position);
        SelectionGrid grid = controller.SpawnSelectionGrid();
        int range = (int)actor.Get(StatTag.MOVE) - actor.stepsMovedThisTurn;
        Func<Vector2Int, bool> rule = (Vector2Int loc) => {
            if (loc == actor.position) {
                return false;
            }
            return map.FindPath(mapEvent, loc, range + 1) != null;
        };
        Vector2Int origin = new Vector2Int(
            (int)mapEvent.positionPx.x - range,
            (int)mapEvent.positionPx.z - range);
        grid.ConfigureNewGrid(actor.position, range, map.terrain, rule);

        Result<Vector2Int> locResult = new Result<Vector2Int>();
        while (!locResult.finished) {
            yield return controller.cursor.AwaitSelectionRoutine(locResult);
            if (!locResult.canceled) {
                if (!rule(locResult.value)) {
                    Global.Instance().Audio.PlaySFX(SFX.error);
                    locResult.Reset();
                }
            }
        }

        cursor.Disable();
        Destroy(grid.gameObject);

        if (locResult.canceled) {
            result.Cancel();
        } else {
            yield return skill.currentEffect.ExecuteSingleCellRoutine(result, locResult.value);
        }
    }
}
