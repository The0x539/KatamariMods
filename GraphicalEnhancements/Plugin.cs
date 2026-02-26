using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine;

namespace GraphicalEnhancements;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        IngameOptions.AddListener();
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(IngameOptions));
        Harmony.CreateAndPatchAll(typeof(ExtraOptions));
        Harmony.CreateAndPatchAll(typeof(DepthOfField));
        Harmony.CreateAndPatchAll(typeof(HighResRenderTargets));
        Harmony.CreateAndPatchAll(typeof(ShadowAngle));
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    [HarmonyPatch(typeof(MonoOnlyCamera), nameof(MonoOnlyCamera.SetTargetTexture))]
    [HarmonyPatch(typeof(UIFixedCamera), nameof(UIFixedCamera.Start))]
    [HarmonyPatch(typeof(UIMonoCamera), nameof(UIMonoCamera.SetTargetTextureDonotAddCamera))]
    public static IEnumerable<CodeInstruction> MsaaEverywhere(IEnumerable<CodeInstruction> instructions) {
        var setAllowMSAA = AccessTools.PropertySetter(typeof(Camera), nameof(Camera.allowMSAA));

        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_0),
                          new(OpCodes.Callvirt, setAllowMSAA))
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_1)
            .Instructions();
    }

    // Without this patch, the Katamari and Prince still get "drawn" during the Royal Rainbow exit animation.
    // Normally this wouldn't do anything, but they get written to the depth buffer,
    // meaning they end up creating a sort of ghost/shadow for the SSAO effect.
    // (This would also affect the depth of field, but that's probably a lot harder to notice than the ghost.)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_OujiSetAlpha))]
    public static void UpdateKatamariVisibility(GameManager __instance, int player, ref float alpha) {
        var playerObj = __instance.gWork.player[player];
        var visibleBefore = playerObj.f32Alpha > 0;
        var visibleAfter = alpha > 0;

        if (visibleBefore == visibleAfter) return;

        // This doesn't actually work for re-enabling visibility, but we can pretend, since the game doesn't actually use it.
        var active = visibleAfter;

        playerObj.objOuji.SetActive(active);
        playerObj.katamari.SetActive(active);
        playerObj.ShadowTransform.GetChild(0).gameObject.SetActive(active);
    }
}