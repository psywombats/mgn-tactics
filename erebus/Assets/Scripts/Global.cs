﻿using UnityEngine;
using System.Collections;

public class Global : MonoBehaviour {

    private static Global instance;
    
    public InputManager Input { get; private set; }
    public LuaInterpreter Lua { get; private set; }
    public MapManager Maps { get; private set; }
    public MemoryManager Memory { get; private set; }
    public AudioManager Audio { get; private set; }
    public SettingsCollection Settings { get; private set; }
    public ScenePlayer ScenePlayer { get; private set; }

    private GlobalConfig config;
    public GlobalConfig Config {
        get {
            if (config == null) {
                config = GlobalConfig.GetInstance();
            }
            return config;
        }
    }

    public static Global Instance() {
        if (instance == null) {
            GameObject globalObject = new GameObject();
            // debug-ish and we don't serialize scenes
            // globalObject.hideFlags = HideFlags.HideAndDontSave;
            instance = globalObject.AddComponent<Global>();
            instance.InstantiateManagers();
        }

        // this should be the only game/engine binding
        GGlobal.Instance();

        return instance;
    }

    public void Update() {
        SetFullscreenMode();
    }

    public void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    private void InstantiateManagers() {
        Settings = gameObject.AddComponent<SettingsCollection>();
        Input = gameObject.AddComponent<InputManager>();
        Lua = gameObject.AddComponent<LuaInterpreter>();
        Maps = gameObject.AddComponent<MapManager>();
        Memory = gameObject.AddComponent<MemoryManager>();
        Audio = gameObject.AddComponent<AudioManager>();
        ScenePlayer = gameObject.AddComponent<ScenePlayer>();
    }

    private void SetFullscreenMode() {
        // not sure if this "check" is necessary
        // actually performing this here is kind of a hack
        if (Settings != null && Screen.fullScreen != Settings.GetBoolSetting(SettingsConstants.Fullscreen).Value) {
            Screen.fullScreen = Settings.GetBoolSetting(SettingsConstants.Fullscreen).Value;
        }
    }
}
