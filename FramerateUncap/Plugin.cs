using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using UnityEngine;

namespace FramerateUncap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public const float TARGET_FPS = 180;

    public void Awake() {
        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(TimerPatches));
        KatamariFfi.InstallHooks();
        Application.runInBackground = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void OnTick(float delta) {
        KatamariFfi.SetDeltaTime(delta);
        KatamariFfi.PreTick();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void AfterTick() {
        KatamariFfi.PostTick();
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

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.UpdateMono))]
    public static IEnumerable<CodeInstruction> DeltaTimePropUpdates(IEnumerable<CodeInstruction> instructions) {
        var input = instructions.ToArray();

        static FieldInfo field(string name, BindingFlags flags = BindingFlags.Public) => typeof(AttachableProp).GetField(name, flags | BindingFlags.Instance);

        var checkSimpleWait = field(nameof(AttachableProp.checkSimpleWait));
        var checkSimpleWait2 = field(nameof(AttachableProp.checkSimpleWait2));
        var s16EscapeTimer = field(nameof(AttachableProp.s16EscapeTimer), BindingFlags.NonPublic);
        var deltaTime = typeof(Time).GetProperty(nameof(Time.deltaTime)).GetGetMethod();

        for (var i = 0; i < input.Length; i++) {
            var instr = input[i];

            if (instr.LoadsConstant(30)) {
                var next = input[i + 1];
                if (next.StoresField(checkSimpleWait) || next.StoresField(checkSimpleWait2)) {
                    instr.operand = 1000;
                }
            } else if (instr.LoadsField(checkSimpleWait) || instr.LoadsField(checkSimpleWait2) || instr.LoadsField(s16EscapeTimer)) {
                CodeInstruction next = input[i + 1], next2 = input[i + 2];

                if (next.opcode == OpCodes.Ldc_I4_1 && next2.opcode == OpCodes.Sub) {
                    yield return instr;

                    // Before: this.$field -= 1;
                    // After: this.$field -= (int)(Time.deltaTime * 1000f;
                    // TODO: Track microsecond rounding error like in the native-code equivalent?
                    // Getting the lifecycle timing right might be tricky.
                    yield return new(OpCodes.Call, deltaTime);
                    yield return new(OpCodes.Ldc_R4, 1000f);
                    yield return new(OpCodes.Mul);
                    yield return new(OpCodes.Conv_I4);
                    i++; // That stuff replaces LDC.I4.1. (The SUB stays intact.)

                    continue;
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

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PreTick();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PostTick();
}
