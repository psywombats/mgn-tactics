﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A stat set is a collection of stats of different types.
 * It can represent a modifier set (+3 str sword) or a base set (Alex, 10 str)
 * */
[System.Serializable]
public class StatSet : ISerializationCallbackReceiver {

    public StatDictionary serializedStats;
    private Dictionary<StatTag, float> stats;

    public StatSet() {
        InitNewSet();
    }

    private StatSet(StatDictionary stats) {
        Dictionary<string, float> statStrings = stats.ToDictionary();
        this.stats = new Dictionary<StatTag, float>();
        foreach (var stat in statStrings) {
            StatTag result;
            if (Enum.TryParse(stat.Key, true, out result)) {
                this.stats[result] = stat.Value;
            }
        }
    }

    private void InitNewSet() {
        stats = new Dictionary<StatTag, float>();
        foreach (StatTag tag in Enum.GetValues(typeof(StatTag))) {
            if (tag != StatTag.None) {
                Stat stat = Stat.Get(tag);
                if (stat == null) {
                    continue;
                }
                stats[tag] = stat.combinator.Identity();
            }
        }
    }

    // === ACCESSORS ===

    public float Get(StatTag tag) {
        return stats[tag];
    }

    public bool Is(StatTag tag) {
        return stats[tag] > 0.0f;
    }

    public void Set(StatTag tag, float value) {
        stats[tag] = value;
    }

    public float this[StatTag tag] {
        get { return stats[tag]; }
        set { stats[tag] = value; }
    }

    // === OPERATIONS ===

    public void Add(StatTag tag, float value) {
        stats[tag] += value;
    }

    public void Sub(StatTag tag, float value) {
        Add(tag, -value);
    }

    public void AddSet(StatSet other) {
        foreach (StatTag tag in Enum.GetValues(typeof(StatTag))) {
            if (other.stats.ContainsKey(tag)) {
                stats[tag] = Stat.Get(tag).combinator.Combine(stats[tag], other.stats[tag]);
            }
        }
    }

    public void RemoveSet(StatSet other) {
        foreach (StatTag tag in Enum.GetValues(typeof(StatTag))) {
            stats[tag] = Stat.Get(tag).combinator.Decombine(stats[tag], other.stats[tag]);
        }
    }

    // === SERIALIZATION ===

    public void OnBeforeSerialize() {
        Dictionary<string, float> statStrings = new Dictionary<string, float>();
        foreach (var stat in stats) {
            statStrings[stat.Key.ToString()] = stat.Value;
        }
        serializedStats = new StatDictionary(statStrings);
    }

    public void OnAfterDeserialize() {
        InitNewSet();
        if (serializedStats != null) {
            AddSet(new StatSet(serializedStats));
        }
    }

    public static implicit operator StatSet(UnityEngine.Object v) {
        throw new NotImplementedException();
    }

    [Serializable]
    public class StatDictionary : SerialDictionary<string, float> {
        public StatDictionary(Dictionary<string, float> dictionary) : base(dictionary) {

        }
    }
}
