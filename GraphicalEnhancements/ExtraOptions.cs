using HarmonyLib;

using MyGame;

using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GraphicalEnhancements;

// Add extra settings to the graphics menu and make them take effect right away where they previously didn't.

static class ExtraOptions {
    private static readonly string QualitySettingsPath = Path.Combine(FileManager.SaveTemporaryPath, "Setting/quality.ex.setting");

    private static readonly int[] fpsValues = [360, 240, 180, 165, 144, 120, 90, 60, 30, 24, 15, 10];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Load))]
    public static bool LoadExtraSettings(QualitySetting __instance) {
        if (!File.Exists(QualitySettingsPath)) return true;

        try {
            var status = File.ReadAllText(QualitySettingsPath)
                .Split(',')
                .Select(int.Parse)
                .ToList();

            while (status.Count < __instance.ItemMax) status.Add(0);

            if (status[0] >= __instance.itemName.Count) status[0] = 0;
            if (status[4] >= QualitySetting.antialiasingValue.Length) status[4] = 1;

            __instance.statusNo = status.ToArray();

            return false;
        } catch (System.Exception ex) {
            System.Console.WriteLine($"Error loading extended graphics settings: {ex}");
            return true;
        }
    }

    // In case the special load fails (e.g. the first time you use this mod)
    // or if new settings are added to the mod
    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Load))]
    public static void PadSettings(QualitySetting __instance) {
        while (__instance.statusNo.Length < __instance.ItemMax) {
            __instance.statusNo = [.. __instance.statusNo, 0];
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Save))]
    public static bool SaveExtraSettings(QualitySetting __instance) {
        const string exFilename = "Setting/quality.ex.setting";
        var text = __instance.statusNo.Join(n => n.ToString(), ",");
        FileManager.Instance.Write(FileManager.SaveTemporaryPath, exFilename, Encoding.UTF8.GetBytes(text));
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), MethodType.Constructor)]
    public static void ExposeExtraSettings(QualitySetting __instance) {
        var items = __instance.itemName;
        //items.Add(["16x", "1x", "2x", "4x", "8x"]);
        items.Add(fpsValues.Select(n => n.ToString()).ToList());
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UguiUtility), nameof(UguiUtility.Awake))]
    public static void ExtendSettingsUI(UguiUtility __instance) {
        if (__instance.gameObject.scene.name != "Quality") return;

        Transform get(string name) => __instance.transform.Find(name);

        var prefabs = new {
            label = get("TextItem5"),
            value = get("TextItemExp5"),
            left = get("Button5L"),
            right = get("Button5R"),
        };

        var previous = prefabs.label;
        var spacing = prefabs.label.position.y - get("TextItem4").position.y;

        // Things get a bit "inconsistent" after 5, so just recreate it all ourselves
        // Text item 6 comes after 7 in the vanilla layout, and it's the disabled DoF option
        // The disabled options also have slightly different sizing/spacing
        for (var i = 6; i < QualitySetting.Instance.ItemMax; i++) {
            var offset = new Vector2(0, spacing * (i - 5));

            Transform make(Transform prefab, string name) {
                var existing = get(name);
                if (existing != null) {
                    existing.SetParent(null);
                    UnityObject.Destroy(existing.gameObject);
                }

                var obj = UnityObject.Instantiate(prefab, prefab.parent);
                obj.name = name;
                obj.Translate(offset);
                return obj;
            }

            make(prefabs.value, $"TextItemExp{i}");
            make(prefabs.right, $"Button{i}R");
            make(prefabs.left, $"Button{i}L");

            var label = make(prefabs.label, $"TextItem{i}");
            LinkFocus(previous, label);
            label.GetComponent<UguiFocus>().focusID = i;
            previous = label;

            var name = GetSettingName(i, out var localized);
            if (localized) {
                label.GetComponent<UITextLocalizer>().textID = name;
            } else {
                UnityObject.Destroy(label.GetComponent<UITextLocalizer>());
                label.GetComponent<Text>().text = name;
            }
        }

        LinkFocus(previous, get("TextItem0"));
    }

    const int ROW_MAX_FPS = 8;

    private static string GetSettingName(int row, out bool localized) {
        localized = true;
        switch (row) {
            case 6: return "UI_SYS_215"; // Depth of Field
            case 7: return "UI_SYS_278"; // Vignette
            case ROW_MAX_FPS:
                localized = false;
                return "Max FPS";
            default:
                localized = false;
                return "(?)";
        }
    }

    private static void LinkFocus(Component prev, Component next) {
        var prevF = prev.GetComponent<UguiFocus>();
        var nextF = next.GetComponent<UguiFocus>();
        prevF.downKeyMove = nextF;
        nextF.upKeyMove = prevF;
    }

    private static int GetTargetFPS(QualitySetting qs) {
        return qs.statusNo.Length > ROW_MAX_FPS ? fpsValues[qs.statusNo[ROW_MAX_FPS]] : 120;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Set))]
    public static void SetMaxFps(QualitySetting __instance) => Application.targetFrameRate = GetTargetFPS(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))] // GameManager sets targetFrameRate to 60 with VSync on and -1 with it off.
    public static void SetMaxFps() => SetMaxFps(QualitySetting.Instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Set))]
    public static void SetDof(QualitySetting __instance) {
        if (!SceneManager.GetSceneByName("GameMain").isLoaded) return;

        var camera = Camera.main;
        if (camera.GetComponent<PostProcessingBehaviour>() is PostProcessingBehaviour ppb) {
            ppb.profile.depthOfField.enabled = __instance.IsDOF;
        }
    }

    // If VSync is enabled, then Unity disregards the setting that this menu item controls.
    // https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Application-targetFrameRate.html
    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.GetItemName))]
    public static string RespondToVsync(string result, int line) {
        if (line == 8 && QualitySettings.vSyncCount != 0) {
            return "VSync";
        } else {
            return result;
        }
    }
}
