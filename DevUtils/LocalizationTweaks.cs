using DefineEnum;

using HarmonyLib;

using System.Collections.Generic;

namespace DevUtils;

public static class LocalizationTweaks {

    private static readonly Dictionary<string, string> replacements = new() {
        {"OT_CTG_012", "Stationery"},
        {"OT_MON_021", "Polaris"}, // "Moon memorial" menu entry
        {"OT_CNS_009", "Polaris"}, // Constellation in Constellations scene
        {"UI_ERT_029", "Make Polaris"},
        {"KG_O_RSL_319", "Do you want to make this Polaris?{^5}\nOr stardust?" },
    };

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Entity_king_text), nameof(Entity_king_text.GetLocaliseText))]
    public static void OverrideLocalization(string id, int local, ref string __result) {
        if (local != (int)LANGUAGE.ENGLISH) return;

        if (replacements.TryGetValue(id, out var text)) {
            __result = text;
        }
    }
}