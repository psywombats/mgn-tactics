﻿using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridSelection))]
public class GridSelectionEditor : Editor {

    private static readonly string PrefabPath = "Assets/Resources/Prefabs/MapEvent2D.prefab";

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GridSelection selection = (GridSelection)target;
        Transform parent = GridSelection.grid.transform.parent;
        if (parent != null && parent.GetComponent<Map>()) {
            if (GUILayout.Button("Create MapEvent2D")) {
                MapEvent2D mapEvent = Instantiate(AssetDatabase.LoadAssetAtPath<MapEvent2D>(PrefabPath)).GetComponent<MapEvent2D>();
                mapEvent.name = "Event" + Random.Range(1000000, 9999999);
                Map map = parent.GetComponent<Map>();
                GameObjectUtility.SetParentAndAlign(mapEvent.gameObject, map.objectLayer.gameObject);
                Undo.RegisterCreatedObjectUndo(mapEvent, "Create " + mapEvent.name);
                mapEvent.SetPosition(GridLocationTileCoords(GridSelection.position));
                Selection.activeObject = mapEvent.gameObject;
            }
        }

    }

    private static Vector2Int GridLocationTileCoords(BoundsInt gridPosition) {
        return new Vector2Int(gridPosition.x, -1 * (gridPosition.y + 1));
    }
}
