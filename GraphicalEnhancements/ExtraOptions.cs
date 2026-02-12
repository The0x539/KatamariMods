using HarmonyLib;

using MyGame;

using System.IO;
using System.Linq;

using UnityEngine;

namespace GraphicalEnhancements;

// Add extra settings to the graphics menu and make them take effect right away where they previously didn't.

static class ExtraOptions {
    private static readonly string QualitySettingsPath = Path.Combine(FileManager.SaveTemporaryPath, "Setting/quality.ex.setting");

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
        var path = Path.Combine(FileManager.SaveTemporaryPath, "Setting/quality.ex.setting");
        var text = __instance.statusNo.Join(n => n.ToString(), ",");
        File.WriteAllText(path, text);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), MethodType.Constructor)]
    public static void ExposeExtraSettings(QualitySetting __instance) {
        var items = __instance.itemName;
        items.Add(["16x", "1x", "2x", "4x", "8x"]);
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

        // TODO: Off/Low/Med/High DoF options
        // TODO: Actually honor the DoF and AF settings rather than just giving them a UI

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
                    Object.Destroy(existing.gameObject);
                }

                var obj = Object.Instantiate(prefab, prefab.parent); // TODO: I didn't know this overload exists and should use it in more places.
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

            label.GetComponent<UITextLocalizer>().textID = i switch {
                6 => "UI_SYS_215", // Depth of Field
                7 => "UI_SYS_278", // Vignette
                8 => "UI_SYS_213", // Anisotropic Filtering
                _ => "UI_TTR_022", // THWACK! (placeholder)
            };
        }

        LinkFocus(previous, get("TextItem0"));
    }

    private static void LinkFocus(Component prev, Component next) {
        var prevF = prev.GetComponent<UguiFocus>();
        var nextF = next.GetComponent<UguiFocus>();
        prevF.downKeyMove = nextF;
        nextF.upKeyMove = prevF;
    }

    // TODO: Update quality settings mid-level, e.g. ambient occlusion
}
