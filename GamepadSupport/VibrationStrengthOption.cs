using BepInEx.Configuration;

using HarmonyLib;

using MyGame;
using MyGame.InputStatus;

using UnityEngine;

namespace GamepadSupport;

public static class VibrationStrengthOption {
    private static readonly ConfigEntry<float> configEntry = Plugin.Cfg.Bind(
        "Vibration", "Strength", 1.0f,
        new ConfigDescription(
            "Scaling factor for vibration motor strength",
            new AcceptableValueRange<float>(0.0f, 1.0f)
        )
    );

    public static float Value {
        get => configEntry.Value;
        set => configEntry.Value = value;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.Awake))]
    public static void ConfigureRow4(KatamariPauseController __instance) {
        var menu = __instance.mainCanvas.transform.Find("MainMenuPC");

        var b3L = menu.Find("Button3L").GetComponent<RectTransform>();
        var b4L = menu.Find("Button4L").GetComponent<RectTransform>();
        // I simply cannot be bothered to figure out why the button is a little bit smaller than its peers.
        b4L.localPosition = b4L.localPosition with { x = b3L.localPosition.x };
        b4L.sizeDelta = b3L.sizeDelta;

        UnityObject.Instantiate(b3L.GetChild(0), b4L);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.ButtonClick))]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.PauseState))]
    public static void ShowButton4R(KatamariPauseController __instance) {
        if (__instance.playerIndex == 0) {
            __instance.mainUguiUtility.UIObjectVisible("Button4R", true);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.KeyboardConfigState))]
    public static bool NoMoreKeyboardConfig(KatamariPauseController __instance) {
        __instance.state.ChangeState(__instance.PauseState);
        __instance.isKeyChange = false;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.MainGuiDraw))]
    public static void RedrawVibrationStrength(KatamariPauseController __instance) {
        var ugui = __instance.mainUguiUtility;
        ugui.SetText("TextItemExp4", $"{Value * 100}%");
        ugui.SetText("TextItem4", $"Vibration Strength");
        ugui.UIObjectGet("TextItem4")
            .GetComponent<UnityEngine.UI.Text>()
            .horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.ButtonClick))]
    public static void Row4Click(KatamariPauseController __instance, GameObject obj) {
        var self = __instance;
        var delta = -0.05f;

        if (obj.name == "Button4R") {
            delta = 0.05f;
            if (self.cursorLine != 4) {
                self.cursorLine = 4;
                self.cursorLinePrev = 4;
                self.mainUguiUtility.SetFocus(4);
            }
            SoundController.Instance.Play(SoundController.eChannel.SE, SoundData.eClipGroup.CommonBank, 1);
        }

        if (obj.name is "Button4L" or "Button4R") {
            Set(self, delta);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.PauseState))]
    public static void Row4LR(KatamariPauseController __instance) {
        var self = __instance;

        if (self.cursorLine != 4) return;
        float delta;
        var pad = InputController.Instance.Pad(0);
        if (pad.IsDown(KeyMap.Left)) {
            delta = -0.05f;
        } else if (pad.IsDown(KeyMap.Right)) {
            delta = 0.05f;
        } else {
            return;
        }

        SoundController.Instance.Play(SoundController.eChannel.SE, SoundData.eClipGroup.CommonBank, 1);
        Set(self, delta);
    }

    private static void Set(KatamariPauseController self, float delta) {
        var n = Value;
        n = Mathf.Clamp01(n + delta);
        n = Mathf.Round(n * 20) / 20; // attempt to mitigate precision errors
        Value = n;
        self.redraw = true;

        var pad = InputController.Instance.Pad(self.playerIndex);
        pad.motorStrengthL = pad.motorStrengthS = 127;
        pad.Vibration(0.3f);
    }
}
