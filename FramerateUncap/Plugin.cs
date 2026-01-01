using BepInEx;

using HarmonyLib;

using MonoMod.Utils;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FramerateUncap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        Harmony.CreateAndPatchAll(this.GetType());
        Kernel32.LoadLibrary("katamari_ffi");
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
                    instr.operand = 1.0f / 180.0f;
                    //instr.operand = 0.0083333333333333f;
                    //instr.operand = 0.01666667f;
                }
            }

            yield return instr;
        }
    }
}

internal static class Kernel32 {
    [DllImport("kernel32", SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool LoadLibrary(string lpLibFileName);

    public const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
}