using BepInEx;

using GamepadSupport.SteamInput;

using HarmonyLib;

using MyGame;
using MyGame.InputStatus;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using SDL = GamepadSupport.SDL3;

namespace GamepadSupport;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    private static Plugin __instance = null!;

    public void Awake() {
        __instance = this;

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

    private static void RunCoroutine(System.Collections.IEnumerator coroutine) => __instance.StartCoroutine(coroutine);

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KeyImageCheck), nameof(KeyImageCheck.Awake))]
    private static void SetRect(KeyImageCheck __instance) {
        var tex = new Texture2D(256, 256);
        var img = __instance.GetComponent<Image>();

        img.sprite = Sprite.Create(tex, new(0, 0, 256, 256), img.sprite.pivot);

        var rt = __instance.GetComponent<RectTransform>();
        if (rt.sizeDelta == new Vector2(320, 160)) {
            rt.sizeDelta = new(160, 160);
        } else if (rt.sizeDelta == new Vector2(120, 60)) {
            rt.sizeDelta = new(60, 60);
        } else if (rt.sizeDelta == new Vector2(88.84f, 44.42f)) {
            rt.sizeDelta = new(44.42f, 44.42f);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EulaManager), nameof(EulaManager.CheckInput))]
    public static bool DontOverrideButtonsForEula() => false;

    public static Texture2D LoadGlyph(InputPadSDL3 sdl, KeyMap key) {
        var xboxOrigin = key switch {
            KeyMap.A => XboxOrigin.A,
            KeyMap.B => XboxOrigin.B,
            KeyMap.X => XboxOrigin.X,
            KeyMap.Y => XboxOrigin.Y,
            KeyMap.L1 => XboxOrigin.LeftBumper,
            KeyMap.R1 => XboxOrigin.RightBumper,
            KeyMap.Back => XboxOrigin.View,
            KeyMap.Start => XboxOrigin.Menu,
            KeyMap.L3 => XboxOrigin.LeftStickClick,
            KeyMap.R3 => XboxOrigin.RightStickClick,
            KeyMap.L2 => XboxOrigin.LeftTriggerClick,
            KeyMap.R2 => XboxOrigin.RightTriggerClick,
            KeyMap.Home => XboxOrigin.Menu, // ¯\_(ツ)_/¯
            KeyMap.Left => XboxOrigin.DPadWest,
            KeyMap.Right => XboxOrigin.DPadEast,
            KeyMap.Up => XboxOrigin.DPadNorth,
            KeyMap.Down => XboxOrigin.DPadSouth,
            KeyMap.StickLeftLeft => XboxOrigin.LeftStickWest,
            KeyMap.StickLeftRight => XboxOrigin.LeftStickEast,
            KeyMap.StickLeftUp => XboxOrigin.LeftStickNorth,
            KeyMap.StickLeftDown => XboxOrigin.LeftStickSouth,
            KeyMap.StickRightLeft => XboxOrigin.RightStickWest,
            KeyMap.StickRightRight => XboxOrigin.RightStickEast,
            KeyMap.StickRightUp => XboxOrigin.RightStickNorth,
            KeyMap.StickRightDown => XboxOrigin.RightStickSouth,

            // TODO: Learn more about why these are separate.
            // Likely to do with US/Japan button switching.
            KeyMap.Enter => XboxOrigin.A,
            KeyMap.Cancel => XboxOrigin.B,

            _ => XboxOrigin.Menu, // idk
        };
        return LoadGlyph(sdl, xboxOrigin);
    }

    public static Texture2D LoadGlyph(InputPadSDL3 sdl, XboxOrigin xboxOrigin) {
        var actionOrigin = sdl.Inner.Steam.GetActionOriginFromXboxOrigin(xboxOrigin);
        return Glyphs.Get(actionOrigin, GlyphSize.Large);
    }

    public static Texture2D LoadGenericGlyph(KeyImageCheck instance) {
        if (IsStickIcon(instance, out var origin)) {
            var actionOrigin = origin switch {
                XboxOrigin.LeftStickMove => ActionOrigin.LeftStickMove,
                XboxOrigin.RightStickMove => ActionOrigin.RightStickMove,
                _ => throw new Exception(), // unreachable
            };
            return Glyphs.Get(actionOrigin, GlyphSize.Large);
        } else {
            return LoadGenericGlyph(instance.iconKeyType);
        }
    }

    public static Texture2D LoadGenericGlyph(KeyMap key) {
        var actionOrigin = key switch {
            KeyMap.A => ActionOrigin.A,
            KeyMap.B => ActionOrigin.B,
            KeyMap.X => ActionOrigin.X,
            KeyMap.Y => ActionOrigin.Y,
            KeyMap.L1 => ActionOrigin.L1,
            KeyMap.R1 => ActionOrigin.R1,
            KeyMap.Back => ActionOrigin.View,
            KeyMap.Start => ActionOrigin.Menu,
            KeyMap.L3 => ActionOrigin.L3,
            KeyMap.R3 => ActionOrigin.L3,
            KeyMap.L2 => ActionOrigin.L2,
            KeyMap.R2 => ActionOrigin.R2,
            KeyMap.Home => ActionOrigin.Menu, // ¯\_(ツ)_/¯
            KeyMap.Left => ActionOrigin.DPadWest,
            KeyMap.Right => ActionOrigin.DPadEast,
            KeyMap.Up => ActionOrigin.DPadNorth,
            KeyMap.Down => ActionOrigin.DPadSouth,
            KeyMap.StickLeftLeft => ActionOrigin.LeftStickWest,
            KeyMap.StickLeftRight => ActionOrigin.LeftStickEast,
            KeyMap.StickLeftUp => ActionOrigin.LeftStickNorth,
            KeyMap.StickLeftDown => ActionOrigin.LeftStickSouth,
            KeyMap.StickRightLeft => ActionOrigin.RightStickWest,
            KeyMap.StickRightRight => ActionOrigin.RightStickEast,
            KeyMap.StickRightUp => ActionOrigin.RightStickNorth,
            KeyMap.StickRightDown => ActionOrigin.RightStickSouth,

            // TODO: See above.
            KeyMap.Enter => ActionOrigin.A,
            KeyMap.Cancel => ActionOrigin.B,

            _ => ActionOrigin.Menu, // idk
        };
        return Glyphs.Get(actionOrigin, GlyphSize.Large);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KeyImageCheck), nameof(KeyImageCheck.GetTexture))]
    public static bool UseSteamGlyph(KeyImageCheck __instance, ref Texture2D __result, KeyMap _iconKeyType) {
        var pad = InputController.Instance.Pad(__instance.padIndex);

        if (!pad.IsConnectPad || pad is not InputPadSDL3 sdl) {
            __result = LoadGenericGlyph(__instance);
            return false;
        }

        try {
            if (IsStickIcon(__instance, out var origin)) {
                __result = LoadGlyph(sdl, origin);
            } else {
                __result = LoadGlyph(sdl, _iconKeyType);
            }
        } catch {
            __result = LoadGenericGlyph(__instance);
            RunCoroutine(QueueForRecheck(__instance));
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TutorialUI), nameof(TutorialUI.Awake))]
    public static void UseKeyImageCheck(TutorialUI __instance) {
        var icons = new Dictionary<string, KeyMap> {
            ["1/joy-con/icon1"] = KeyMap.StickLeftUp,
            ["1/joy-con/icon1 (1)"] = KeyMap.StickLeftDown,
            ["1/joy-con/icon2"] = KeyMap.StickRightDown,
            ["1/joy-con/icon2 (1)"] = KeyMap.StickRightUp,
            ["3/window_switch/stick1"] = KeyMap.L3,
            ["3/window_switch/stick2"] = KeyMap.R3,
        };

        foreach (var entry in icons) {
            __instance.objPage2[0].transform
                .Find(entry.Key).gameObject
                .AddComponent<KeyImageCheck>()
                .SetIcon(entry.Value);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIKinoko), nameof(UIKinoko.Start))]
    public static void UseKeyImageCheck(UIKinoko __instance) {
        SetDashIcons(__instance._go_guidePlay.transform.Find("tame_icon/icon/pad_joy-con"));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PadTypeEnableVS), nameof(PadTypeEnableVS.Start))]
    public static void UseKeyImageCheck(PadTypeEnableVS __instance) {
        SetDashIcons(__instance.padDefault.transform.Find("pad_sw"));
    }

    private static void SetDashIcons(Transform parent) {
        if (parent == null) return;
        var icons = new[] {
            KeyMap.StickLeftUp,
            KeyMap.StickLeftDown,
            KeyMap.StickRightDown,
            KeyMap.StickRightUp,
        };
        for (var i = 0; i < icons.Length; i++) {
            parent.GetChild(i).gameObject
                .AddComponent<KeyImageCheck>()
                .SetIcon(icons[i]);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TutorialJoypad), nameof(TutorialJoypad.Start))]
    public static void ObviousJoysticks(TutorialJoypad __instance) {
        __instance.stickLen = 1;
    }

    private static System.Collections.IEnumerator QueueForRecheck(KeyImageCheck key) {
        // For some reason, the Steam Input stuff isn't immediately ready upon the controller showing up as an SDL device.
        // This fix is kinda sketchy, but seems to get the job done.
        yield return new WaitForSeconds(0.5f);
        key.Set();
    }

    private static bool IsStickIcon(KeyImageCheck k, out XboxOrigin xboxOrigin) {
        xboxOrigin = k.iconKeyType switch {
            KeyMap.StickLeftLeft => XboxOrigin.LeftStickMove,
            KeyMap.StickRightRight => XboxOrigin.RightStickMove,
            _ => XboxOrigin.A,
        };

        if (xboxOrigin == XboxOrigin.A) return false;
        if (k.gameObject.name is not ("Image_icon" or "Image_leftStick" or "ImagePad")) return false;
        var idx = k.transform.GetSiblingIndex();
        if (k.transform.parent.childCount <= idx) return false;

        var siblingName = k.transform.parent.GetChild(idx + 1).name;
        return siblingName is "Image_icon_up" or "Image_icon_up (1)" or "Image_icon (1)" or "ImageKeyboardUp (1)";
    }

    // This was supposed to be a patch for a KeyImage method but I guess I forgot?
    public static bool UseSteamGlyph(KeyImage __instance, ref Texture2D __result, KeyMap key) {
        var pad = InputController.Instance.Pad(0);
        if (!pad.IsConnectPad) return true;
        if (pad is not InputPadSDL3 sdl) return true;

        __result = LoadGlyph(sdl, key);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KeyImageCheck), nameof(KeyImageCheck.Awake))]
    public static void AddShadow(KeyImageCheck __instance) {
        if (__instance.gameObject.GetComponent<Shadow>() != null) return; // bail if there's already a shadow
        var parentShadow = __instance.transform.parent.GetComponent<Shadow>() ?? __instance.transform.parent.parent?.GetComponent<Shadow>();
        if (parentShadow == null || !parentShadow.enabled) return; // bail if the text has no matching shadow

        var shadow = __instance.gameObject.AddComponent<Shadow>();
        shadow.effectColor = parentShadow.effectColor;
        shadow.effectDistance = parentShadow.effectDistance / __instance.transform.localScale;
    }
}

static class Extensions {
    public static void SetIcon(this KeyImageCheck self, KeyMap key) {
        self.iconKeyType = self.iconKeyTypeWork = key;
        // This triggers the "change detection" without doing anything destructive.
        // The private field gets updated to match the public property.
        self.OnOff = true;
        self.onOff = false;
    }
}
