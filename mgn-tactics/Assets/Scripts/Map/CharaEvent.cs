﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

/**
 * For our purposes, a CharaEvent is anything that's going to be moving around the map
 * or has a physical appearance. For parallel process or whatevers, they won't have this.
 */
[RequireComponent(typeof(MapEvent))]
[DisallowMultipleComponent]
public class CharaEvent : MonoBehaviour {

    private const float Gravity = -20.0f;
    private const float JumpHeightUpMult = 1.2f;
    private const float JumpHeightDownMult = 1.6f;
    private const string DefaultMaterial2DPath = "Materials/Sprite2D";
    private const string DefaultMaterial3DPath = "Materials/Sprite3D";
    private const float DesaturationDuration = 0.5f;
    private const float StepsPerSecond = 4.0f;
    private const float JumpStepsPerSecond = 8.0f;

    public GameObject doll;
    public SpriteRenderer mainLayer;
    public SpriteRenderer armsLayer;
    public SpriteRenderer itemLayer;
    public bool alwaysAnimates = false;
    public bool cameraRelativeFacing = false;

    private Dictionary<string, Sprite> sprites;
    private Vector2 lastPosition;
    private bool wasSteppingLastFrame;
    private List<KeyValuePair<float, Vector3>> afterimageHistory;
    private Vector3 targetPx;
    private float moveTime;

    public MapEvent parent { get { return GetComponent<MapEvent>(); } }
    public Map map { get { return parent.parent; } }
    public Sprite overrideBodySprite { get; set; }
    public Sprite itemSprite { get; set; }
    public ArmMode armMode { get; set; }
    public ItemMode itemMode { get; set; }
    public bool jumping { get; set; }
    public bool stepping { get; set; }
    public float desaturation { get; set; }

    [SerializeField]
    [HideInInspector]
    private Texture2D _spritesheet;
    public Texture2D spritesheet {
        get { return _spritesheet; }
        set {
            _spritesheet = value;
            LoadSpritesheetData();
            UpdateAppearance();
        }
    }

    [SerializeField]
    [HideInInspector]
    private OrthoDir _facing = OrthoDir.South;
    public OrthoDir facing {
        get { return _facing; }
        set {
            _facing = value;
            if (armsLayer != null) {
                if (facing == OrthoDir.North) {
                    armsLayer.sortingOrder = -2;
                } else {
                    armsLayer.sortingOrder = 2;
                }
            }
            UpdateAppearance();
        }
    }

    private List<SpriteRenderer> _renderers;
    protected List<SpriteRenderer> renderers {
        get {
            if (_renderers == null) {
                _renderers = new List<SpriteRenderer>();
                Debug.Assert(mainLayer != null);
                _renderers.Add(mainLayer);
                if (armsLayer != null) _renderers.Add(armsLayer);
                if (itemLayer != null) _renderers.Add(itemLayer);
            }
            return _renderers;
        }
    }

    public static string NameForFrame(string sheetName, int x, int y) {
        return sheetName + "_" + x + "_" + y;
    }

    public void Start() {
        CopyShaderValues();
        GetComponent<Dispatch>().RegisterListener(MapEvent.EventMove, (object payload) => {
            facing = (OrthoDir)payload;
        });
        GetComponent<Dispatch>().RegisterListener(MapEvent.EventEnabled, (object payload) => {
            bool enabled = (bool)payload;
            foreach (SpriteRenderer renderer in renderers) {
                renderer.enabled = enabled;
            }
        });
        GetComponent<Dispatch>().RegisterListener(MapEvent.EventInteract, (object payload) => {
            facing = parent.DirectionTo(Global.Instance().Maps.avatar.GetComponent<MapEvent>());
        });
    }

    public void Update() {
        CopyShaderValues();

        bool steppingThisFrame = IsSteppingThisFrame();
        stepping = steppingThisFrame || wasSteppingLastFrame;
        if (!steppingThisFrame && !wasSteppingLastFrame) {
            moveTime = 0.0f;
        } else {
            moveTime += Time.deltaTime;
        }
        wasSteppingLastFrame = steppingThisFrame;
        lastPosition = transform.position;

        UpdateAppearance();
    }

    public void UpdateAppearance() {
        if (spritesheet != null) {
            if (sprites == null || sprites.Count == 0) {
                LoadSpritesheetData();
            }
            mainLayer.sprite = SpriteForMain();
            if (armsLayer != null) armsLayer.sprite = SpriteForArms();
            if (itemLayer != null) itemLayer.sprite = SpriteForItem();

            if (itemLayer != null && itemLayer.sprite != null) {
                itemLayer.transform.localPosition = new Vector3(
                    (float)armMode.ItemAnchor().x / Map.TileSizePx,
                    (float)armMode.ItemAnchor().y / Map.TileSizePx, 
                    itemLayer.transform.localPosition.z);
                itemLayer.transform.localEulerAngles = new Vector3(0.0f, 0.0f, itemMode.Rotation());
            }
        }
    }

    public void FaceToward(MapEvent other) {
        facing = parent.DirectionTo(other);
    }

    public Sprite FrameBySlot(int x) {
        return FrameByExplicitSlot(x, DirectionRelativeToCamera().Ordinal());
    }
    public Sprite FrameByExplicitSlot(int x, int y) {
        string name = NameForFrame(spritesheet.name, x, y);
        if (!sprites.ContainsKey(name)) {
            Debug.LogError(this + " doesn't contain frame " + name);
        }
        return sprites[name];
    }

    private void CopyShaderValues() {
        foreach (SpriteRenderer renderer in renderers) {
            Material material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            if (material != null) {
                material.SetFloat("_Desaturation", desaturation);
            } 
        }
    }

    public IEnumerator StepRoutine(OrthoDir dir) {
        facing = dir;
        Vector2Int offset = parent.OffsetForTiles(dir);
        Vector3 startPx = parent.positionPx;
        targetPx = parent.OwnTileToWorld(parent.position);
        if (targetPx.y == startPx.y || GetComponent<MapEvent3D>() == null) {
            yield return parent.LinearStepRoutine(dir);
        } else if (targetPx.y > startPx.y) {
            // jump up routine routine
            float duration = (targetPx - startPx).magnitude / parent.tilesPerSecond / 2.0f * JumpHeightUpMult;
            yield return JumpRoutine(startPx, targetPx, duration);
            overrideBodySprite = FrameBySlot(0); // "prone" frame
            yield return CoUtils.Wait(1.0f / parent.tilesPerSecond / 2.0f);
            overrideBodySprite = null;
        } else {
            // jump down routine
            float elapsed = 0.0f;
            float walkRatio = 0.65f;
            float walkDuration = walkRatio / parent.tilesPerSecond;
            while (true) {
                float t = elapsed / walkDuration;
                elapsed += Time.deltaTime;
                parent.transform.position = new Vector3(
                    startPx.x + t * (targetPx.x - startPx.x) * walkRatio,
                    startPx.y,
                    startPx.z + t * (targetPx.z - startPx.z) * walkRatio);
                if (elapsed >= walkDuration) {
                    break;
                }
                yield return null;
            }
            float dy = targetPx.y - startPx.y;
            float jumpDuration = Mathf.Sqrt(dy / Gravity) * JumpHeightDownMult;
            bool isBigDrop = dy <= -1.0f;
            yield return JumpRoutine(parent.transform.position, targetPx, jumpDuration, isBigDrop);
            if (isBigDrop) {
                overrideBodySprite = FrameBySlot(0); // "prone" frame
                yield return CoUtils.Wait(JumpHeightDownMult / parent.tilesPerSecond / 2.0f);
                overrideBodySprite = null;
            }
        }
    }

    public IEnumerator DesaturateRoutine(float targetDesat) {
        float oldDesat = desaturation;
        float elapsed = 0.0f;
        while (desaturation != targetDesat) {
            elapsed += Time.deltaTime;
            desaturation = Mathf.Lerp(oldDesat, targetDesat, elapsed / DesaturationDuration);
            yield return null;
        }
    }

    private IEnumerator JumpRoutine(Vector3 startPx, Vector3 targetPx, float duration, bool useJumpFrames = true) {
        jumping = useJumpFrames;
        float elapsed = 0.0f;
        
        float dy = (targetPx.y - startPx.y);
        float b = (dy - Gravity * (duration * duration)) / duration;
        while (true) {
            float t = elapsed / duration;
            elapsed += Time.deltaTime;
            parent.transform.position = new Vector3(
                startPx.x + t * (targetPx.x - startPx.x),
                startPx.y + Gravity * (elapsed * elapsed) + b * elapsed,
                startPx.z + t * (targetPx.z - startPx.z));
            if (elapsed >= duration) {
                break;
            }
            yield return null;
        }
        jumping = false;
        parent.SetScreenPositionToMatchTilePosition();
    }

    private bool IsSteppingThisFrame() {
        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        return alwaysAnimates || (delta.sqrMagnitude > 0 && delta.sqrMagnitude < Map.TileSizePx) || parent.tracking ||
            (GetComponent<AvatarEvent>() && GetComponent<AvatarEvent>().wantsToTrack);
    }

    private void LoadSpritesheetData() {
        string path = GetComponent<MapEvent3D>() == null ? DefaultMaterial2DPath : DefaultMaterial3DPath;
        foreach (SpriteRenderer renderer in renderers) {
            if (renderer.material == null) {
                renderer.material = Resources.Load<Material>(path);
            }
        }

        sprites = new Dictionary<string, Sprite>();
        path = AssetDatabase.GetAssetPath(spritesheet);
        if (path.StartsWith("Assets/Resources/")) {
            path = path.Substring("Assets/Resources/".Length);
        }
        if (path.EndsWith(".png")) {
            path = path.Substring(0, path.Length - ".png".Length);
        }
        foreach (Sprite sprite in Resources.LoadAll<Sprite>(path)) {
            sprites[sprite.name] = sprite;
        }
    }

    private OrthoDir DirectionRelativeToCamera() {
        Camera cam = Application.isPlaying ? parent.parent.camera : FindObjectOfType<Camera>();
        if (!cam || !cameraRelativeFacing) {
            return facing;
        }

        Vector3 ourScreen = cam.WorldToScreenPoint(transform.position);
        Vector3 targetWorld = ((MapEvent3D)parent).OwnTileToWorld(parent.position + facing.XY3D());
        targetWorld.y = parent.transform.position.y;
        Vector3 targetScreen = cam.WorldToScreenPoint(targetWorld);
        Vector3 delta = targetScreen - ourScreen;
        return OrthoDirExtensions.DirectionOf2D(new Vector2(delta.x, -delta.y));
    }

    private Sprite SpriteForMain() {
        if (overrideBodySprite != null) {
            return overrideBodySprite;
        }

        int x;
        if (jumping) {
            x = Mathf.FloorToInt(moveTime * JumpStepsPerSecond) % 2 + 3;
        } else {
            x = Mathf.FloorToInt(moveTime * StepsPerSecond) % 4;
            if (x == 3) x = 1;
            if (!stepping) x = 1;
        }
        return FrameBySlot(x);
    }

    private Sprite SpriteForArms() {
        if (armMode == ArmMode.Disabled && jumping) {
            return FrameBySlot(ArmMode.Raised.FrameIndex());
        }
        if (armMode.Show()) {
            return FrameBySlot(armMode.FrameIndex());
        } else {
            return null;
        }
    }

    private Sprite SpriteForItem() {
        if (itemMode.Show()) {
            return itemSprite;
        } else {
            return null;
        }
    }
}
