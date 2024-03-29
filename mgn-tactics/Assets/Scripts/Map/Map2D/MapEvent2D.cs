﻿using UnityEngine;

public class MapEvent2D : MapEvent {

    public static Vector2Int WorldToTile(Vector3 pos) {
        return new Vector2Int(
            Mathf.RoundToInt(pos.x / Map.TileSizePx) * OrthoDir.East.Px2DX(),
            Mathf.RoundToInt(pos.y / Map.TileSizePx) * OrthoDir.North.Px2DY());
    }

    public static Vector3 TileToWorld(Vector2Int position) {
        return new Vector3(
            position.x * Map.TileSizePx / Map.UnityUnitScale * OrthoDir.East.Px2DX(),
            position.y * Map.TileSizePx / Map.UnityUnitScale * OrthoDir.North.Px2DY(),
            0);
    }

    public override Vector2Int OwnWorldToTile(Vector3 pos) {
        return WorldToTile(pos);
    }

    public override Vector3 OwnTileToWorld(Vector2Int position) {
        Vector3 baseCoords = TileToWorld(position);
        return new Vector3(baseCoords.x, baseCoords.y, DepthForPositionPx(baseCoords.y));
    }

    public override void SetTilePositionToMatchScreenPosition() {
        SetPosition(OwnWorldToTile(transform.localPosition));
        Vector2 sizeDelta = GetComponent<RectTransform>().sizeDelta;
    }

    public override OrthoDir DirectionTo(Vector2Int position) {
        return OrthoDirExtensions.DirectionOf2D(position - this.position);
    }

    public override Vector2Int OffsetForTiles(OrthoDir dir) {
        return dir.XY2D();
    }

    public override void SetScreenPositionToMatchTilePosition() {
        Vector2 transform = new Vector2(Map.TileSizePx, Map.TileSizePx);
        transform.x = transform.x * OrthoDir.East.Px2DX();
        transform.y = transform.y * OrthoDir.North.Px2DY();
        this.transform.localPosition = Vector2.Scale(position, transform);
        GetComponent<RectTransform>().sizeDelta = new Vector2(
            size.x * Map.TileSizePx / Map.UnityUnitScale, 
            size.y * Map.TileSizePx / Map.UnityUnitScale);
        GetComponent<RectTransform>().pivot = new Vector2(0.0f, 1.0f);
    }

    public override Vector3 InternalPositionToDisplayPosition(Vector3 position) {
        return new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), position.z);
    }

    public override void SetDepth() {
        if (parent != null) {
            gameObject.transform.localPosition = new Vector3(
                gameObject.transform.localPosition.x,
                gameObject.transform.localPosition.y,
                DepthForPositionPx(gameObject.transform.localPosition.y));
        }
    }

    public override Vector3 GetHandlePosition() {
        return transform.position + new Vector3(0, -16, 0);
    }

    protected override void DrawGizmoSelf() {
        if (GetComponent<CharaEvent>() == null || GetComponent<CharaEvent>().spritesheet == null) {
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
            Gizmos.DrawCube(new Vector3(
                    transform.position.x + size.x * Map.TileSizePx * OrthoDir.East.Px2DX() / 2.0f,
                    transform.position.y + size.y * Map.TileSizePx * OrthoDir.North.Px2DY() / 2.0f,
                    transform.position.z - 0.001f),
                new Vector3((size.x - 0.1f) * Map.TileSizePx, (size.y - 0.1f) * Map.TileSizePx, 0.002f));
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(new Vector3(
                    transform.position.x + size.x * Map.TileSizePx * OrthoDir.East.Px2DX() / 2.0f,
                    transform.position.y + size.y * Map.TileSizePx * OrthoDir.North.Px2DY() / 2.0f,
                    transform.position.z - 0.001f),
                new Vector3((size.x - 0.1f) * Map.TileSizePx, (size.y - 0.1f) * Map.TileSizePx, 0.002f));
        }
    }

    protected override bool UsesSnap() {
        return true;
    }

    private float DepthForPositionPx(float y) {
        return (y / (parent.size.y * Map.TileSizePx)) * 0.1f;
    }
}
