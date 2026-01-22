using BepInEx;
using BepInEx.Unity.Mono;

using HarmonyLib;

using MyGame;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

using SDL = GamepadSupport.SDL3;

namespace GamepadSupport;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        SDL.SDL.InitSubSystem(SDL.InitFlags.Gamepad);
        Harmony.CreateAndPatchAll(this.GetType());
    }

    public void Update() {
        while (SDL.SDLEvent.Poll() is not null) { }
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

    // TODO: This still needs to line up with which controllers actually get used
    [HarmonyPrefix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.GetJoystickData))]
    public static void GetJoystickData(KatamariPauseController __instance, out bool __runOriginal) {
        __runOriginal = false;
        var self = __instance;
        self.lstGamePad.Clear();

        self.lstPlayer[0].GamePadLoc = -1;
        self.lstPlayer[1].GamePadLoc = -1;

        foreach (var joystickId in SDL.Gamepad.GetGamepads()) {
            var player = SDL.Gamepad.PlayerIndexForID(joystickId);
            var name = SDL.Gamepad.NameForID(joystickId);
            var setting = new KatamariPauseController.RwGamePadSetting {
                id = (int)joystickId,
                dispName = name,
                PlayerLoc = player,
            };

            if (player is 0 or 1) {
                self.lstPlayer[player].GamePadLoc = self.lstGamePad.Count;
            }

            self.lstGamePad.Add(setting);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.MainGuiDraw))]
    public static void StopHardcoding(KatamariPauseController __instance) {
        var self = __instance;

        var loc = self.lstPlayer[self.playerIndex].GamePadLoc;
        if (loc >= 0) {
            self.mainUguiUtility.SetText("TextItemExp3", self.lstGamePad[loc].dispName);
            self.mainUguiUtility.SetTextColor("TextItemExp3", self.textColor[self.playerIndex]);
        }
    }
}
