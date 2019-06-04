﻿using UnityEngine;

public class Global : MonoBehaviour {

    private static Global instance;
    private static bool destructing;
    
    public InputManager Input { get; private set; }
    public MapManager Maps { get; private set; }
    public MemoryManager Memory { get; private set; }
    public AudioManager Audio { get; private set; }
    public SettingsCollection Settings { get; private set; }
    public PartyManager Party { get; private set; }

    private IndexDatabase database;
    public IndexDatabase Database {
        get {
            if (database == null && !destructing) {
                database = IndexDatabase.Instance();
            }
            return database;
        }
    }

    public static Global Instance() {
        if (instance == null) {
            GameObject globalObject = new GameObject("Globals");
            instance = globalObject.AddComponent<Global>();
            instance.InstantiateManagers();
        }

        return instance;
    }

    public void Update() {
        SetFullscreenMode();
    }

    public void Awake() {
        DontDestroyOnLoad(gameObject);
        MoonSharp.Interpreter.UserData.RegisterAssembly();
    }

    public void OnDestroy() {
        destructing = true;
    }

    private void InstantiateManagers() {
        gameObject.AddComponent<LuaCutsceneContext>();

        Settings = gameObject.AddComponent<SettingsCollection>();
        Input = gameObject.AddComponent<InputManager>();
        Maps = gameObject.AddComponent<MapManager>();
        Memory = gameObject.AddComponent<MemoryManager>();
        Audio = gameObject.AddComponent<AudioManager>();
        Party = gameObject.AddComponent<PartyManager>();
    }

    private void SetFullscreenMode() {
        // not sure if this "check" is necessary
        // actually performing this here is kind of a hack
        if (Settings != null && Screen.fullScreen != Settings.GetBoolSetting(SettingsConstants.Fullscreen).Value) {
            Screen.fullScreen = Settings.GetBoolSetting(SettingsConstants.Fullscreen).Value;
        }
    }
}
