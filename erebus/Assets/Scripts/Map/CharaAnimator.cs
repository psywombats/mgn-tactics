﻿using System;
using System.Collections;
using System.Collections.Generic;
using Tiled2Unity;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class CharaAnimator : MonoBehaviour {

    private const string DefaultMaterialPath = "Materials/SpriteDefault";
    private const string AlwaysAnimatesProperty = "step";

    public MapEvent parentEvent = null;
    public bool alwaysAnimates = false;
    public bool dynamicFacing = false;
    public string spriteName = "";

    private Vector2 lastPosition;

    public void Start() {
        lastPosition = gameObject.transform.position;

        if (Parent().GetComponent<CharaEvent>() != null) {
            Parent().GetComponent<Dispatch>().RegisterListener(MapEvent.EventEnabled, (object payload) => {
                bool enabled = (bool)payload;
                GetComponent<SpriteRenderer>().enabled = enabled;
            });
        }
    }

    public void Update() {
        if (Parent().GetComponent<CharaEvent>() != null) {
            Vector2 position = Parent().transform.position;
            Vector2 delta = position - lastPosition;

            bool stepping = alwaysAnimates || delta.sqrMagnitude > 0 || Parent().GetComponent<MapEvent>().tracking;
            GetComponent<Animator>().SetBool("stepping", stepping);
            GetComponent<Animator>().SetInteger("dir", CalculateDirection().Ordinal());

            lastPosition = position;
        } else {
            GetComponent<Animator>().SetBool("stepping", alwaysAnimates);
            GetComponent<Animator>().SetInteger("dir", OrthoDir.South.Ordinal());
        }
    }

    public void SetSpriteByKey(string spriteName) {
        this.spriteName = spriteName;
        string controllerPath = "Animations/Charas/Instances/" + spriteName;
        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(controllerPath);
        GetComponent<Animator>().runtimeAnimatorController = controller;

        GetComponent<SpriteRenderer>().material = Resources.Load<Material>(DefaultMaterialPath);

        string spritePath = "Sprites/Charas/" + spriteName;
        Sprite[] sprites = Resources.LoadAll<Sprite>(spritePath);
        foreach (Sprite sprite in sprites) {
            if (sprite.name == spriteName + parentEvent.GetComponent<CharaEvent>().facing.DirectionName() + "Center") {
                GetComponent<SpriteRenderer>().sprite = sprite;
                break;
            }
        }
    }

    public void Populate(IDictionary<string, string> properties) {
        if (properties.ContainsKey(AlwaysAnimatesProperty)) {
            alwaysAnimates = true;
        }
    }

    private GameObject Parent() {
        return parentEvent == null ? transform.parent.gameObject : parentEvent.gameObject;
    }

    private void UpdatePositionMemory() {
        lastPosition.x = gameObject.transform.position.x;
        lastPosition.y = gameObject.transform.position.y;
    }

    private OrthoDir CalculateDirection() {
        OrthoDir normalDir = Parent().GetComponent<CharaEvent>().facing;
        MapCamera cam = Application.isPlaying ? Global.Instance().Maps.Camera : FindObjectOfType<MapCamera>();
        if (!cam || !dynamicFacing && !cam.dynamicFacing) {
            return normalDir;
        }

        Vector3 ourScreen = cam.GetCameraComponent().WorldToScreenPoint(parentEvent.transform.position);
        Vector3 targetWorld = MapEvent3D.TileToWorldCoords(parentEvent.Position + normalDir.XY());
        Vector3 targetScreen = cam.GetCameraComponent().WorldToScreenPoint(targetWorld);
        Vector3 delta = targetScreen - ourScreen;
        return OrthoDirExtensions.DirectionOf(new Vector2(delta.x, -delta.y));
    }
}
