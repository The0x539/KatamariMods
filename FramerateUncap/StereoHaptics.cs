using HarmonyLib;

using MyGame;

using System.Runtime.InteropServices;

using UnityEngine;

namespace FramerateUncap;

public static class StereoHaptics {
    private readonly record struct QueuedVibration(float Ratio, float Time);

    private static readonly QueuedVibration?[] queuedVibrations = new QueuedVibration?[2];

    private static bool deferring = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void BeforeTick() {
        deferring = true;
        KatamariFfi.ResetRumbleBias();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void AfterTick() {
        deferring = false;
        for (nuint i = 0; i <= 1; i++) {
            if (queuedVibrations[i] is not QueuedVibration qv) continue;
            queuedVibrations[i] = null;

            var bias = KatamariFfi.GetRumbleBias(i);
            // bias represents the fraction of the initially-equal strength
            // that gets transferred from one side to the other
            var left = Mathf.Clamp01(qv.Ratio * (1f - bias)) * 255f;
            var right = Mathf.Clamp01(qv.Ratio * (1f + bias)) * 255f;

            var pad = InputController.Instance.Pad((int)i);
            pad.MotorStrengthL = (int)left;
            pad.MotorStrengthS = (int)right;
            pad.Vibration(qv.Time / 30f);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.CallbackVibration))]
    public static bool DeferVibration(int playerNo, float ratio, float time) {
        if (!deferring) return true;
        if (time <= 0f) return true;

        queuedVibrations[playerNo] = new(ratio, time);
        return false;
    }
}

internal static partial class KatamariFfi {
    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ResetRumbleBias();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern float GetRumbleBias(nuint pIdx);
}
