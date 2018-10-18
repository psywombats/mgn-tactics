﻿using System.Collections;
using System.Collections.Generic;
using Tiled2Unity;
using UnityEngine;
using System;

/**
 * For our purposes, a CharaEvent is anything that's going to be moving around the map
 * or has a physical appearance. For parallel process or whatevers, they won't have this.
 */
[RequireComponent(typeof(MapEvent))]
[DisallowMultipleComponent]
public class CharaEvent : MonoBehaviour {

    public static readonly string FaceEvent = "eventFace";

    private static readonly string PropertySprite = "sprite";
    private static readonly string PropertyFacing = "face";

    // Editor
    public OrthoDir initialFacing;
    public GameObject doll;

    // Public
    public Map parent { get { return GetComponent<MapEvent>().Parent; } }
    public ObjectLayer layer { get { return GetComponent<MapEvent>().Layer; } }

    private OrthoDir internalFacing;
    public OrthoDir facing {
        get {
            return internalFacing;
        }
        set {
            if (internalFacing != value) {
                internalFacing = value;
                GetComponent<Dispatch>().Signal(FaceEvent, value);
            }
        }
    }

    public void Start() {
        facing = initialFacing;
        GetComponent<Dispatch>().RegisterListener(MapEvent.EventMove, (object payload) => {
            facing = (OrthoDir)payload;
        });
    }

    public void Populate(IDictionary<string, string> properties) {
        if (properties.ContainsKey(PropertyFacing)) {
            initialFacing = OrthoDirExtensions.Parse(properties[PropertyFacing]);
            facing = initialFacing;
        }
        if (properties.ContainsKey(PropertySprite)) {
            if (GetComponent<MapEvent3D>() != null) {
                doll = new GameObject("Doll");
                doll.transform.parent = gameObject.transform;
                doll.transform.localPosition = new Vector3(0.125f, 0.0f, -0.65f);
                CharaAnimator animator = doll.AddComponent<CharaAnimator>();
                animator.ParentEvent = GetComponent<MapEvent>();
                animator.Populate(properties[PropertySprite]);
            } else {
                gameObject.AddComponent<CharaAnimator>().Populate(properties[PropertySprite]);
            }

        }
        GetComponent<MapEvent>().Passable = false;
    }

    // checks if the given location is passable for this character
    // takes into account both chip and event
    public bool CanPassAt(IntVector2 loc) {
        if (!GetComponent<MapEvent>().SwitchEnabled) {
            return true;
        }

        foreach (MapEvent mapEvent in parent.GetEventsAt(layer, loc)) {
            if (!mapEvent.IsPassableBy(this)) {
                return false;
            }
        }

        return GetComponent<MapEvent>().CanPassAt(loc);
    }

    public IEnumerator PathToRoutine(IntVector2 location) {
        List<IntVector2> path = parent.FindPath(this, location);
        if (path == null) {
            yield break;
        }
        foreach (IntVector2 target in path) {
            OrthoDir dir = OrthoDirExtensions.DirectionOf(target - GetComponent<MapEvent>().Position);
            yield return StartCoroutine(GetComponent<MapEvent>().StepRoutine(dir));
        }
    }
}
