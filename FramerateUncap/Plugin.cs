using BepInEx;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FramerateUncap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public const float TARGET_FPS = 150;

    public void Awake() {
        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(TimerPatches));
        KatamariFfi.InstallHooks();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void OnTick(float delta) {
        KatamariFfi.SetDeltaTime(delta);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sAreaChange))]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.FadeUpdate))]
    [HarmonyPatch(typeof(MsgSys), nameof(MsgSys.Update))] // This one is the important guy for fixing the dialogue bug.
    [HarmonyPatch(typeof(King), nameof(King.Update))]
    public static IEnumerable<CodeInstruction> Uncap(IEnumerable<CodeInstruction> instructions) {
        foreach (var instr in instructions) {
            if (instr.LoadsConstant()) {
                if (instr.OperandIs(0.03333333f) || instr.OperandIs(0.01666667f)) {
                    instr.operand = 1.0f / TARGET_FPS;
                }
            }

            yield return instr;
        }
    }

}

internal static class KatamariFfi {
    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstallHooks();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDeltaTime(float delta);
}
