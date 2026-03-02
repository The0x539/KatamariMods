using HarmonyLib;

using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine;

namespace GraphicalEnhancements;

static class HighResRenderTargets {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.gTj_ResultWait))]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.GetTexture2DInner))]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    public static IEnumerable<CodeInstruction> HiResKatamariPortrait(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var getWidth = AccessTools.Method(typeof(HighResRenderTargets), nameof(GetWidth));
        var getHeight = AccessTools.Method(typeof(HighResRenderTargets), nameof(GetHeight));

        // We need to supersample it a bit because MSAA only applies to face edges, not along sharp lines in textures.
        // This game's art style has... a lot of sharp lines in textures.
        return new CodeMatcher(instructions, generator)
            .MatchForward(false,
                          new(OpCodes.Ldc_R4, 1920f),
                          new(OpCodes.Ldc_R4, 1080f))
            .Repeat(cm => cm
                .SetAndAdvance(OpCodes.Call, getWidth)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add),
                                  new(OpCodes.Conv_R4))
                .SetAndAdvance(OpCodes.Call, getHeight)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add),
                                  new(OpCodes.Conv_R4)))
            .Start()
            .MatchForward(false,
                          new(OpCodes.Ldc_I4, 1920),
                          new(OpCodes.Ldc_I4, 1080))
            .Repeat(cm => cm
                .SetAndAdvance(OpCodes.Call, getWidth)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add))
                .SetAndAdvance(OpCodes.Call, getHeight)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add))
)
            .Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    public static void AntialiasKatamariResultImage(CameraKatamari __instance) {
        var descriptor = CameraKatamari._rTexture.descriptor;
        CameraKatamari._rTexture.Release();
        descriptor.msaaSamples = 4;
        var rt = new RenderTexture(descriptor);
        __instance._camera.targetTexture = CameraKatamari._rTexture = rt;
        if (__instance._rImage != null) __instance._rImage.texture = rt;
    }

    private static int GetWidth() => Screen.currentResolution.width;
    private static int GetHeight() => Screen.currentResolution.height;
}