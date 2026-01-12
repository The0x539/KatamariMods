using BepInEx;

using GamepadSupport.SDL3;

using HarmonyLib;

using MyGame;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

namespace GamepadSupport;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        SDL.InitSubSystem(InitFlags.Gamepad);
        Harmony.CreateAndPatchAll(this.GetType());
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(InputController), nameof(InputController.Setup))]
    public static IEnumerable<CodeInstruction> UseMyGuy(IEnumerable<CodeInstruction> instructions) {
        static MethodInfo addComponent(Type component) => AccessTools.Method(typeof(GameObject), nameof(GameObject.AddComponent), null, [component]);

        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, addComponent(typeof(InputPadRewired))))
            .SetOperandAndAdvance(addComponent(typeof(InputPadSDL3)))
            .Instructions();
    }
}
