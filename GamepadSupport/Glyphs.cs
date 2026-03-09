using GamepadSupport.SteamInput;

using HarmonyLib;

using MyGame;
using MyGame.InputStatus;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamepadSupport;

public static class Glyphs {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(KeyImageCheck), nameof(KeyImageCheck.Awake))]
    private static void SetRect(KeyImageCheck __instance) {
        var tex = new Texture2D(256, 256);
        var img = __instance.GetComponent<Image>();

        img.sprite = Sprite.Create(tex, new(0, 0, 256, 256), img.sprite.pivot);

        var rt = __instance.GetComponent<RectTransform>();
        // Cover the common cases of icons being double-wide due to the vanilla sprite layout
        if (rt.sizeDelta == new Vector2(320, 160)) {
            rt.sizeDelta = new(160, 160);
        } else if (rt.sizeDelta == new Vector2(120, 60)) {
            rt.sizeDelta = new(60, 60);
        } else if (rt.sizeDelta == new Vector2(88.84f, 44.42f)) {
            rt.sizeDelta = new(44.42f, 44.42f);
        } else if (rt.sizeDelta == new Vector2(85, 101)) {
            // Cover the "Skip" prompt in the results screen being too big
            rt.sizeDelta = new(42, 42);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EulaManager), nameof(EulaManager.CheckInput))]
    public static bool DontOverrideButtonsForEula() => false;

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
            __instance.StartCoroutine(QueueForRecheck(__instance));
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIKinoko), nameof(UIKinoko.Start))]
    public static void UseKeyImageCheck(UIKinoko __instance) {
        SetDashIcons(__instance._go_guidePlay.transform.Find("tame_icon/icon/pad_joy-con"));

        var textChoice2 = __instance._go_guideMenu.transform.Find("Text_Choice2");
        var icons = new[] { textChoice2.GetChild(0), textChoice2.GetChild(1), textChoice2.GetChild(2) };

        foreach (var icon in icons) {
            icon.gameObject.AddComponent<KeyImageCheck>().SetIcon(KeyMap.StickRightUp);
        }

        var rotateIcon2 = UnityObject.Instantiate(icons[0], textChoice2);
        rotateIcon2.GetComponent<KeyImageCheck>().SetIcon(KeyMap.StickRightDown);
        rotateIcon2.gameObject.SetActive(icons.Any(x => x.gameObject.activeSelf));
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

    private static readonly Dictionary<string, Texture2D> glyphCache = [];

    public static Texture2D LoadGlyphFile(ActionOrigin origin, GlyphSize size) {
        // TODO: Figure out what the flags argument does. Hopefully it could be of some use for choosing the "theme".
        string path = ISteamInput.Instance.GetGlyphPNGForActionOrigin(origin, size, 0);
        {
            var colorPath = path
                .Replace("knockout\\ps_button", "light\\ps_color_button")
                .Replace("knockout\\shared_color_button", "light\\shared_color_button");
            if (File.Exists(colorPath)) path = colorPath;
        }
        if (SceneManager.GetActiveScene().name == "Title2") {
            path = path.Replace("knockout", "dark");
        }

        if (glyphCache.TryGetValue(path, out var existing)) return existing;

        var tex = new Texture2D(0, 0) { wrapMode = TextureWrapMode.Clamp };
        var data = File.ReadAllBytes(path);
        ImageConversion.LoadImage(tex, data);

        glyphCache[path] = tex;
        return tex;
    }

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
        return LoadGlyphFile(actionOrigin, GlyphSize.Large);
    }

    public static Texture2D LoadGenericGlyph(KeyImageCheck instance) {
        if (IsStickIcon(instance, out var origin)) {
            var actionOrigin = origin switch {
                XboxOrigin.LeftStickMove => ActionOrigin.LeftStickMove,
                XboxOrigin.RightStickMove => ActionOrigin.RightStickMove,
                _ => throw new System.Exception(), // unreachable
            };
            return LoadGlyphFile(actionOrigin, GlyphSize.Large);
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
        return LoadGlyphFile(actionOrigin, GlyphSize.Large);
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

    public static void SetIcon(this KeyImageCheck self, KeyMap key) {
        self.iconKeyType = self.iconKeyTypeWork = key;
        // This triggers the "change detection" without doing anything destructive.
        // The private field gets updated to match the public property.
        self.OnOff = true;
        self.onOff = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TutorialPadKeyboardCheck), nameof(TutorialPadKeyboardCheck.Set))]
    public static void DecideWhetherToFixMeteorGlyph(TutorialPadKeyboardCheck __instance, out bool __state) {
        __state = InputController.Instance.Pad(__instance.playerIndex).IconType != __instance.iconType;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TutorialPadKeyboardCheck), nameof(TutorialPadKeyboardCheck.Set))]
    public static void FixMeteorGlyph(TutorialPadKeyboardCheck __instance, bool __state) {
        if (!__state) return;

        var label = __instance.transform.parent.Find("Text_buttonInfo")?.GetComponent<UIControllerTextLocalizer>();
        if (label == null) return; // Avoids false positives in other scenes
        var idx = label.keyMap switch {
            KeyMap.Up => 0,
            KeyMap.Right => 1,
            KeyMap.Down => 2,
            KeyMap.Left => 3,
            _ => 4,
        };
        if (idx >= 4) return;
        if (__instance.iconType == IconType.Keybord) return;

        // yes this ternary is redundant after the check above, but whatever
        var icons = __instance.iconType == IconType.Keybord ? __instance.objKeyboard : __instance.objGamePad;
        for (var i = 0; i < 4; i++) {
            icons[i].SetActive(i == idx);
        }
    }
}