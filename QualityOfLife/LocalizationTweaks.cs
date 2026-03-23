using DefineEnum;

using HarmonyLib;

using System.Collections.Generic;

namespace QualityOfLife;

public static class LocalizationTweaks {

    private static readonly Dictionary<string, string> replacements = new() {
        {"OT_CTG_012", "Stationery"},
        {"OT_MON_021", "Polaris"}, // "Moon memorial" menu entry
        {"OT_CNS_009", "Polaris"}, // Constellation in Constellations scene
        {"UI_ERT_029", "Make Polaris"},
        {"KG_O_RSL_319", "Do you want to make this Polaris?{^5}\nOr stardust?" },
    };

    private static readonly HashSet<string> colombo = [
        "KG_C_URS_010",
        "UI_MSH_040",
        "OT_OBJ_1495",
        "OT_OBJ_1713",
        "OT_KNG_021",
    ];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Entity_king_text), nameof(Entity_king_text.GetLocaliseText))]
    public static void OverrideLocalization(string id, int local, ref string __result) {
        if (colombo.Contains(id)) {
            __result = __result.Replace("Colombo", "Njamo").Replace("コロンボ", "ンジャモ");
            return;
        }

        if (local != (int)LANGUAGE.ENGLISH) return;

        if (replacements.TryGetValue(id, out var text)) {
            __result = text;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Entity_king_text), nameof(Entity_king_text.GetGroupTextIDs))]
    public static void OverrideGroupLocalization(List<Entity_king_text.CommonContent> __result, GlobalWork gWork) {
        if (gWork.language != LANGUAGE.ENGLISH) return;
        foreach (var item in __result) {
            if (replacements.TryGetValue(item.textID, out var replacement)) {
                item.content = replacement;
            }
        }
    }
}