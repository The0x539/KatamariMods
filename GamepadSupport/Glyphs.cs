using GamepadSupport.SDL3;
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
    public static bool UseSteamGlyph(KeyImageCheck __instance, ref Texture2D __result, in KeyMap _iconKeyType) {
        var pad = InputController.Instance.Pad(__instance.padIndex);

        if (!pad.IsConnectPad || pad is not InputPadSDL3 sdl) {
            __result = LoadGlyphFile(DetermineGenericGlyph(__instance));
            return false;
        }

        try {
            if (IsStickIcon(__instance, out var xboxOrigin)) {
                __result = LoadGlyphFile(DetermineGlyph(sdl, xboxOrigin));
            } else if (IsMeteorDpadIcon(__instance, out var steamOrigin)) {
                __result = LoadGlyphFile(steamOrigin, forceStyle: GlyphStyle.Knockout);
            } else {
                __result = LoadGlyphFile(DetermineGlyph(sdl, _iconKeyType.ToXboxOrigin(japaneseFaceButtons: false)));
            }
        } catch {
            __result = LoadGlyphFile(DetermineGenericGlyph(__instance));
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
        yield return new WaitForSecondsRealtime(0.5f);
        key.Set();
    }

    private static readonly Dictionary<string, Texture2D> glyphCache = [];

    public static Texture2D LoadGlyphFile(ActionOrigin origin, GlyphSize size = GlyphSize.Large, GlyphStyle? forceStyle = null) {
        var steam = ISteamInput.Instance;

        GlyphStyle style;

        if (steam.TranslateActionOrigin(InputType.SteamDeck, origin) is >= ActionOrigin.A and <= ActionOrigin.Y) {
            // Face buttons should be dark detail on a colorful shape.
            style = GlyphStyle.Light;
            if (origin is >= ActionOrigin.PS5_X and <= ActionOrigin.PS5_Square) {
                // PS4 gets colorful face buttons; PS5 does not.
                origin = steam.TranslateActionOrigin(InputType.PS4, origin);
            }
        } else if (SceneManager.GetActiveScene().name == "Title2") {
            // On the save select screen, everything other than face buttons should be white detail on a dark shape.
            style = GlyphStyle.Dark;
        } else {
            // In most cases, icons should be dark detail on a white shape.
            style = GlyphStyle.Light;
        }

        string path = steam.GetGlyphPNGForActionOrigin(origin, size, forceStyle ?? style);
        if (forceStyle == null && SceneManager.GetActiveScene().name == "Title2" && path.Contains("light") && !path.Contains("color")) {
            path = steam.GetGlyphPNGForActionOrigin(origin, size, GlyphStyle.Dark);
        }

        if (glyphCache.TryGetValue(path, out var existing)) return existing;

        var tex = new Texture2D(0, 0, TextureFormat.RGBA32, mipmap: true) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            name = Path.GetFileNameWithoutExtension(path),
        };
        var data = File.ReadAllBytes(path);
        ImageConversion.LoadImage(tex, data, markNonReadable: true);

        glyphCache[path] = tex;
        return tex;
    }

    public static ActionOrigin DetermineGlyph(InputPadSDL3 sdl, XboxOrigin xboxOrigin) {
        if (sdl.Inner.Steam.IsValid) {
            return sdl.Inner.Steam.GetActionOriginFromXboxOrigin(xboxOrigin);
        } else {
            return ISteamInput.Instance.TranslateActionOrigin(sdl.Inner.GamepadType.ToSteam(), xboxOrigin.ToActionOrigin());
        }
    }

    public static ActionOrigin DetermineGenericGlyph(KeyImageCheck instance) {
        if (IsStickIcon(instance, out var stickOrigin)) {
            return stickOrigin.ToActionOrigin();
        } else {
            return instance.iconKeyType.ToXboxOrigin(japaneseFaceButtons: false).ToActionOrigin();
        }
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

    private static bool IsMeteorDpadIcon(KeyImageCheck k, out ActionOrigin steamOrigin) {
        steamOrigin = ActionOrigin.None;
        if (k.transform.parent?.name != "Guide_meteor") return false;

        steamOrigin = k.iconKeyType switch {
            KeyMap.Up => ActionOrigin.SC_DPadNorth,
            KeyMap.Down => ActionOrigin.SC_DPadSouth,
            KeyMap.Left => ActionOrigin.SC_DPadEast,
            KeyMap.Right => ActionOrigin.SC_DPadWest,
            _ => ActionOrigin.None,
        };

        return steamOrigin != ActionOrigin.None;
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