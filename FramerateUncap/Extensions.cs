using HarmonyLib;

using System;
using System.Reflection.Emit;

namespace FramerateUncap;

internal static class Extensions {
    public static CodeMatcher RemoveMatching(this CodeMatcher m, params CodeMatch[] ms) {
        m.MatchForward(false, ms);
        m.ThrowIfInvalid("Could not find a matching pattern of instructions");
        var labels = m.Instruction.ExtractLabels();
        m.RemoveInstructions(ms.Length);
        m.AddLabels(labels);
        return m;
    }

    public static CodeMatcher AssertPos(this CodeMatcher m, int pos) {
        if (m.Pos != pos) {
            throw new InvalidOperationException($"Wrong instruction position: expected {pos}, got {m.Pos}");
        }
        return m;
    }

    public static int ConstInt(this CodeInstruction i) {
        if (i.opcode == OpCodes.Ldc_I4_M1) return -1;
        if (i.opcode == OpCodes.Ldc_I4_0) return 0;
        if (i.opcode == OpCodes.Ldc_I4_1) return 1;
        if (i.opcode == OpCodes.Ldc_I4_2) return 2;
        if (i.opcode == OpCodes.Ldc_I4_3) return 3;
        if (i.opcode == OpCodes.Ldc_I4_4) return 4;
        if (i.opcode == OpCodes.Ldc_I4_5) return 5;
        if (i.opcode == OpCodes.Ldc_I4_6) return 6;
        if (i.opcode == OpCodes.Ldc_I4_7) return 7;
        if (i.opcode == OpCodes.Ldc_I4_8) return 8;
        if (i.opcode == OpCodes.Ldc_I4_S) return (sbyte)i.operand;
        return (int)i.operand;
    }
}
