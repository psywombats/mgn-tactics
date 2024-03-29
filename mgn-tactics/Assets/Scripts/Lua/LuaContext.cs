﻿using UnityEngine;
using System.Collections;
using MoonSharp.Interpreter;
using Coroutine = MoonSharp.Interpreter.Coroutine;
using System;
using UnityEngine.Assertions;
using System.IO;

/**
 * A wrapper around Script that represents an environment where a script can execute. Only one
 * script can execute within a given context at a time.
 */
public class LuaContext : MonoBehaviour {
    
    private static readonly string DefinesPath = "Lua/Defines/GlobalDefines";
    private static string defines;

    private Script _lua;
    public Script lua {
        get {
            if (_lua == null) {
                _lua = new Script();
            }
            return _lua;
        }
    }

    private LuaScript activeScript;
    private int blockingRoutines;
    private bool forceKilled;

    public virtual void Awake() {
        LoadDefines(DefinesPath);
        AssignGlobals();
    }

    // make sure the luaobject has been registered via [MoonSharpUserData]
    public void SetGlobal(string key, object luaObject) {
        lua.Globals[key] = luaObject;
    }

    public bool IsRunning() {
        return activeScript != null;
    }

    public DynValue CreateObject() {
        return lua.DoString("return {}");
    }

    public DynValue Marshal(object toMarshal) {
        return DynValue.FromObject(lua, toMarshal);
    }

    public Coroutine CreateScript(string fullScript) {
        try {
            DynValue scriptFunction = lua.DoString(fullScript);
            return lua.CreateCoroutine(scriptFunction).Coroutine;
        } catch (SyntaxErrorException e) {
            Debug.LogError("bad script: " + fullScript + "\n\nerror:\n" + e.DecoratedMessage);
            throw e;
        }
    }

    // all coroutines that are meant to block execution of the script should go through here
    public virtual void RunRoutineFromLua(IEnumerator routine) {
        if (forceKilled) {
            // leave the old instance infinitely suspended
            return;
        }
        blockingRoutines += 1;
        Global.Instance().StartCoroutine(CoUtils.RunWithCallback(routine, () => {
            blockingRoutines -= 1;
            if (blockingRoutines <= 0) {
                blockingRoutines = 0;
                if (activeScript != null && !forceKilled) {
                    activeScript.scriptRoutine.Resume();
                }
            }
        }));
    }

    // meant to be evaluated synchronously
    public LuaCondition CreateCondition(string luaScript) {
        return new LuaCondition(this, lua.LoadString(luaScript));
    }

    // evaluates a lua function in the global context
    public DynValue Evaluate(DynValue function) {
        return lua.Call(function);
    }

    // hang on to a chunk of lua to run later
    public DynValue Load(string luaChunk) {
        return lua.LoadString(luaChunk);
    }

    // kills the current script, useful for debug only
    public void ForceTerminate() {
        blockingRoutines = 0;
        forceKilled = true;
    }

    public IEnumerator RunRoutine(string luaString) {
        LuaScript script = new LuaScript(this, luaString);
        yield return RunRoutine(script);
    }

    public virtual IEnumerator RunRoutine(LuaScript script) {
        Assert.IsNull(activeScript);
        activeScript = script;
        forceKilled = false;
        try {
            script.scriptRoutine.Resume();
        } catch (Exception e) {
            Debug.Log("Exception during script: " + script + "\n\nerror:\n" + e.Message);
            throw e;
        }
        while (script.scriptRoutine.State != CoroutineState.Dead && !forceKilled) {
            yield return null;
        }
        activeScript = null;
    }

    protected virtual void AssignGlobals() {
        lua.Globals["debugLog"] = (Action<DynValue>)DebugLog;
        lua.Globals["playSFX"] = (Action<DynValue>)PlaySFX;
        lua.Globals["cs_wait"] = (Action<DynValue>)Wait;
        lua.Globals["getSwitch"] = (Func<DynValue, DynValue>)GetSwitch;
        lua.Globals["setSwitch"] = (Action<DynValue, DynValue>)SetSwitch;
        lua.Globals["eventNamed"] = (Func<DynValue, LuaMapEvent>)EventNamed;
    }

    protected void LoadDefines(string path) {
        LuaSerializedScript script = Resources.Load<LuaSerializedScript>(path);
        lua.DoString(script.luaString);
    }

    // === LUA CALLABLE ============================================================================

    private LuaMapEvent EventNamed(DynValue eventName) {
        MapEvent mapEvent = Global.Instance().Maps.activeMap.GetEventNamed(eventName.String);
        if (mapEvent == null) {
            return null;
        } else {
            return mapEvent.luaObject;
        }
    }

    private DynValue GetSwitch(DynValue switchName) {
        bool value = Global.Instance().Memory.GetSwitch(switchName.String);
        return Marshal(value);
    }

    private void SetSwitch(DynValue switchName, DynValue value) {
        Global.Instance().Memory.SetSwitch(switchName.String, value.Boolean);
    }

    private void DebugLog(DynValue message) {
        Debug.Log(message.CastToString());
    }

    private void Wait(DynValue seconds) {
        RunRoutineFromLua(CoUtils.Wait((float)seconds.Number));
    }

    private void PlaySFX(DynValue sfxKey) {
        Global.Instance().Audio.PlaySFX(sfxKey.String);
    }
}
