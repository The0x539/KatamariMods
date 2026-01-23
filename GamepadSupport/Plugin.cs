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
        while (SDL.SDLEvent.Poll() is SDL.SDLEvent ev) {
            if (ev.Dispatch() is not SDL.GamepadDeviceEvent gde) continue;

            if (gde.common.type == SDL.EventType.GamepadAdded) {
                OnConnect(gde.which);
            } else if (gde.common.type == SDL.EventType.GamepadRemoved) {
                OnDisconnect(gde.which);
            }
        }
    }

    private static void OnConnect(SDL.JoystickID id) {
        var player = SDL.Gamepad.PlayerIndexForID(id);

        // Completely ignore the player index that the gamepad is currently assigned to.
        // Instead, just find the first player without an assigned gamepad.
        for (var i = 0; i < InputController.CONTROLLER_MAX; i++) {
            var pad = (InputPadSDL3)InputController.Instance.Pad(i);
            if (pad != null && !pad.IsConnectPad) {
                pad.Connect(id);
                break;
            }
        }
    }

    private static void OnDisconnect(SDL.JoystickID id) {
        // Is there actually anything to be done here, though?
        Console.WriteLine($"Gamepad removed: {id}");
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputController), nameof(InputController.GetConnectCount))]
    [HarmonyPatch(typeof(Rewired.ReInput.ControllerHelper), nameof(Rewired.ReInput.ControllerHelper.joystickCount), MethodType.Getter)]
    public static int GetConnectCount(int _) => SDL.Gamepad.GetGamepads().Count;

    // Yeah this is gonna need reintroducing the keyboard for gamepadloc = -1
    [HarmonyPrefix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.RewirdPadChange))]
    public static void Reassign(KatamariPauseController __instance, int loc_add, out bool __runOriginal) {
        __runOriginal = false;
        var self = __instance;

        var pIdx = self.playerIndex;

        var oldGamepad = self.lstPlayer[pIdx].GamePadLoc;
        var newGamepad = (oldGamepad + loc_add + self.lstGamePad.Count) % self.lstGamePad.Count;

        if (self.lstGamePad[newGamepad].PlayerLoc >= 0) {
            // The other player was using that controller.
            // Give them the one being switched from.
            self.lstPlayer[1 - pIdx].GamePadLoc = oldGamepad;
            self.lstGamePad[oldGamepad].PlayerLoc = 1 - pIdx;
        }

        self.lstPlayer[pIdx].GamePadLoc = newGamepad;
        self.lstGamePad[newGamepad].PlayerLoc = pIdx;

        for (var i = 0; i <= 1; i++) {
            var pad = (InputPadSDL3)InputController.Instance.Pad(i);
            pad.IsVibration = self.gWork.isVibration[i];
            pad.Connect((SDL.JoystickID)self.lstGamePad[self.lstPlayer[i].GamePadLoc].id); ;
        }

        self.CallUpdateBirdAnimator();
    }

    // Ideally, we'd use the joystick GUIDs to persist the preference across runs.
    // The vanilla game does something similar using the GamePadLoc and PlayerLoc fields, which I think it persists.
    // This should be doable, but won't be easy.

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
