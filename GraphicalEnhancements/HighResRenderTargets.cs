using HarmonyLib;

using System.Collections.Generic;
using System.Reflection.Emit;

namespace GraphicalEnhancements;

static class HighResRenderTargets {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.gTj_ResultWait))]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.GetTexture2DInner))]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    public static IEnumerable<CodeInstruction> HiResKatamariPortrait(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator)
            .MatchForward(false,
                          new(OpCodes.Ldc_R4, 1920f),
                          new(OpCodes.Ldc_R4, 1080f))
            .Repeat(cm => cm
                .SetOperandAndAdvance(7680f)
                .SetOperandAndAdvance(4320f))
            .Start()
            .MatchForward(false,
                          new(OpCodes.Ldc_I4, 1920),
                          new(OpCodes.Ldc_I4, 1080))
            .Repeat(cm => cm
                .SetOperandAndAdvance(7680)
                .SetOperandAndAdvance(4320))
            .Instructions();
    }
}