using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

using MyGame;

using System;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;

using SDL = GamepadSupport.SDL3;

namespace GamepadSupport;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    private static Plugin __instance = null!;
    public static ManualLogSource Log => __instance.Logger;

    public void Awake() {
        __instance = this;

        SDL.SDL.SetHint("SDL_JOYSTICK_HIDAPI_VERTICAL_JOY_CONS", "1");
        SDL.SDL.InitSubSystem(SDL.InitFlags.Gamepad | SDL.InitFlags.Sensor);

        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(Glyphs));
    }

    public void Update() {
        while (SDL.SDLEvent.Poll() is SDL.SDLEvent ev) {
            var d = ev.Dispatch();

            if (d is not SDL.GamepadDeviceEvent gde) continue;
            if (gde.common.type == SDL.EventType.GamepadAdded) {
                OnConnect(gde.which);
            } else if (gde.common.type == SDL.EventType.GamepadRemoved) {
                OnDisconnect(gde.which);
            }
        }
    }

    private static void OnConnect(SDL.JoystickID id) {
        var gamepadType = SDL.Gamepad.GamepadTypeForID(id);
        Log.LogInfo($"New connection from joystick {id}, type: {gamepadType}");

        if (gamepadType is SDL.GamepadType.SwitchJoyConLeft or SDL.GamepadType.SwitchJoyConRight) {
            Log.LogWarning("Ignoring individual Joy-Con");
            return;
        }

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
        Log.LogWarning($"Joystick {id} disconnected");

        if (GetPauseMenu() is not PauseMenu menu) return;
        if (menu.sCheckPause() != Define.TRUE) return;
        if (menu.gWork.u8Pause != Define.OFF) return;

        menu.Pause();
        menu.gWork.u8Pause = Define.ON;
        menu.sInit(0); // ideally we'd detect which player got disconnected but honestly? whatever
        menu.objGuidePause.SetActive(true);

        return;
    }

    private static PauseMenu? GetPauseMenu() {
        var scene = SceneManager.GetSceneByName("UI_Pause");
        if (!scene.IsValid()) return null;

        foreach (var o in scene.GetRootGameObjects()) {
            if (o.GetComponent<PauseMenu>() is PauseMenu menu) return menu;
        }
        return null;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(InputController), nameof(InputController.Setup))]
    public static IL UseMyGuy(IL il) {
        static MethodInfo addComponent<T>() where T : Component => Member.Method<GameObject>(o => o.AddComponent<T>());

        return new CodeMatcher(il)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, addComponent<InputPadRewired>()))
            .SetOperandAndAdvance(addComponent<InputPadSDL3>())
            .Instructions();
    }

    // Disable the object so it doesn't try to update and produce those annoying logs accordingly
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputController), nameof(InputController.Setup))]
    public static void SuppressRewiredA(InputController __instance) {
        __instance.userdatastore.gameObject.SetActive(false);
    }

    // Remove the "userdatastore" setup
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.Start))]
    public static IL SuppressRewiredB(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Call, Member.Method<KatamariPauseController>(kpc => kpc.GetJoystickData())))
            .GetPos(out var pos)
            .RemoveInstructionsInRange(0, pos - 1)
            .Instructions();
    }

    // Reroute "ReInput.controllers.joystickCount" to my SDL replacement since the `.controllers` part goes awry
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.ControllerCheck))]
    public static IL SuppressRewiredC(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Call, Member.Getter(() => Rewired.ReInput.controllers)),
                          new(OpCodes.Callvirt, Member.Getter<Rewired.ReInput.ControllerHelper>(ch => ch.joystickCount)))
            .Repeat(cm => cm
                .RemoveInstructions(2)
                .Insert(new(OpCodes.Ldc_I4_0),
                        new(OpCodes.Call, Member.Method(() => GetConnectCount(0)))))
            .Instructions();
    }

    // Disable these methods to save and load keyboard settings since this mod doesn't currently support keyboard input at all
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputController), nameof(InputController.LoadSetting))]
    [HarmonyPatch(typeof(InputController), nameof(InputController.SaveSetting))]
    public static bool SuppressRewiredD(out bool __result) {
        __result = true;
        return false;
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

        self.lstPlayer ??= [];
        if (self.lstPlayer.Count == 0) {
            self.lstPlayer.Add(new() { id = 0, difineName = "Player 1" });
            self.lstPlayer.Add(new() { id = 1, difineName = "Player 2" });
        }

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.Start))]
    public static void ExposeMotionControls(KatamariPauseController __instance) {
        var joyA = __instance.gWork.localKingText.GetLocaliseText("UI_PRN_025", (int)__instance.gWork.language);
        var joyB = __instance.gWork.localKingText.GetLocaliseText("UI_PRN_026", (int)__instance.gWork.language);
        __instance.textMoveType = [.. __instance.textMoveType, joyA /* , joyB */];
    }
}
