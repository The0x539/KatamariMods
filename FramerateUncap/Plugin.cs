using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using UnityEngine;

namespace FramerateUncap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        Harmony.CreateAndPatchAll(this.GetType());
        KatamariFfi.InstallHooks();
        Application.runInBackground = true;

        var clicky = new GameObject("Clicky");
        clicky.AddComponent<Clicky>();
        DontDestroyOnLoad(clicky);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void OnTick(float delta) {
        KatamariFfi.SetDeltaTime(delta);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static IEnumerable<CodeInstruction> Uncap(IEnumerable<CodeInstruction> instructions) {
        var deltaFrame = AccessTools.Field(typeof(GameManager), nameof(GameManager.deltaFrame));
        var mSimulation = AccessTools.Field(typeof(GameManager), nameof(GameManager.mSimulation));
        var thirtieth = 0.03333333f;

        return new CodeMatcher(instructions)
            // 683: if this.deltaFrame >= 1/30 {} (outer)
            .MatchForward(false, new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, deltaFrame),
                          new(OpCodes.Ldc_R4, thirtieth),
                          new(OpCodes.Blt_Un),
                          new(OpCodes.Ldc_I4_0)).AssertPos(683)
            // New condition: this.deltaFrame > 0f
            .Advance(2).SetOperandAndAdvance(0f)
            .SetOpcodeAndAdvance(OpCodes.Ble_Un)
            // 693: this.deltaFrame -= 1/30;
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Dup),
                          new(OpCodes.Ldfld, deltaFrame),
                          new(OpCodes.Ldc_R4, thirtieth),
                          new(OpCodes.Sub),
                          new(OpCodes.Stfld, deltaFrame)).AssertPos(693)
            // New: this.deltaFrame -= deltaTime;
            // Yes, this is a bit silly. I don't want to refactor the target code much more than this,
            // and getting this patch to work properly already took a lot of trial and error.
            .Advance(3).SetInstruction(new(OpCodes.Ldloc_0))
            // 762: this.mSimulation.DoTick(1/30)
            .MatchForward(true,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, mSimulation),
                          new(OpCodes.Ldc_R4, thirtieth)).AssertPos(764)
            // new argument: deltaTime
            .SetInstruction(new(OpCodes.Ldloc_0))
            // 812: if this.deltaFrame >= 1/30 {} (inner)
            .RemoveMatching(new(OpCodes.Ldarg_0),
                            new(OpCodes.Ldfld, deltaFrame),
                            new(OpCodes.Ldc_R4, thirtieth)).AssertPos(812)
            // For some reason, this is a backward jump, so we need to keep it and just make it unconditional.
            .SetOpcodeAndAdvance(OpCodes.Br)
            .Instructions();
    }

    private static readonly FieldInfo
        s32Time = AccessTools.Field(typeof(GameManager), nameof(GameManager.s32Time)),
        tutoTimer = AccessTools.Field(typeof(SI_TUTORIAL), nameof(SI_TUTORIAL.s32Timer)),
        gWork = AccessTools.Field(typeof(GameManager), nameof(GameManager.gWork)),
        siTutorial = AccessTools.Field(typeof(GlobalWork), nameof(GlobalWork.siTutorial));

    // TODO: Improve this further; patch ALL the per-frame stuff.
    // This will do for now by stopping the level-start dialogue from sometimes failing to show up and softlocking the game.
    // Some stuff is fine to stay capped at 30 or 60, but anyhing called by sMain() or that touches the same timers
    // will need to be updated accordingly.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_GameStateInitGameClear))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_GameStateInitGameOver))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sDemo))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameClear))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameOver))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sTutoTitle))]
    public static IEnumerable<CodeInstruction> PatchTimerIncrements(IEnumerable<CodeInstruction> instructions) {
        var getDeltaTime = AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime));

        var loadsTimer = new CodeMatch(OpCodes.Ldfld) { operands = { s32Time, tutoTimer } };
        var storesTimer = new CodeMatch(OpCodes.Stfld) { operands = { s32Time, tutoTimer } };

        return new CodeMatcher(instructions)
            // Match all code patterns that increment or decrement a timer by 1
            .MatchForward(false,
                          loadsTimer,
                          new(OpCodes.Ldc_I4_1),
                          new(ci => ci.opcode == OpCodes.Add || ci.opcode == OpCodes.Sub),
                          storesTimer)
            .Repeat(cm => cm
                // Replace the 1 with (int)(Time.deltaTime * 1000f)
                .Advance(1)
                .RemoveInstruction()
                .Insert(new(OpCodes.Call, getDeltaTime),
                        new(OpCodes.Ldc_R4, 1000f),
                        new(OpCodes.Mul),
                        new(OpCodes.Conv_I4)))
            .Start()
            // Match all code patterns that assign a (nonzero) constant to a timer
            .MatchForward(false,
                          new(ci => ci.LoadsConstant() && ci.opcode != OpCodes.Ldc_I4_0),
                          storesTimer)
            .Repeat(cm => cm
                // Multiply that constant by the milliseconds-per-tick ratio
                .SetInstructionAndAdvance(new(OpCodes.Ldc_I4, cm.Instruction.ConstInt() * 1000 / 30)))
            .Start()
            // Match all code patterns that branch if s32Time is nonzero
            .MatchForward(false,
                          new(OpCodes.Ldfld, s32Time),
                          new(OpCodes.Brtrue))
            .Repeat(cm => cm
                // Instead branch if s32time is GREATER than zero
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
                .SetOpcodeAndAdvance(OpCodes.Bgt))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameOver))]
    public static IEnumerable<CodeInstruction> TimerFixup1(IEnumerable<CodeInstruction> instructions) {
        return new CodeMatcher(instructions)
            .MatchForward(true,
                          new(OpCodes.Ldfld, s32Time),
                          new(OpCodes.Ldc_I4_S, (sbyte)60))
            .SetInstruction(new(OpCodes.Ldc_I4, 2000)) // 60 frames -> 2000 ms
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sTutoTitle))]
    public static IEnumerable<CodeInstruction> TimerFixup2(IEnumerable<CodeInstruction> instructions, ILGenerator gen) {
        var matcher = new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldfld, tutoTimer),
                          new(OpCodes.Ldc_I4_S, (sbyte)22),
                          new(OpCodes.Bne_Un))
            .Advance(1)
            .RemoveInstruction();

        // Old C#: if (s32Timer == 22 frames)
        // New C#: if (s32Timer <= 733 ms && s32Timer > 667 ms)
        var target = (Label)matcher.Operand;
        var loc = gen.DeclareLocal(typeof(int));
        matcher
            .RemoveInstruction()
            .Insert(new(OpCodes.Stloc, loc),
                    new(OpCodes.Ldloc, loc),
                    new(OpCodes.Ldc_I4, 733),
                    new(OpCodes.Bgt_Un, target), // the condition fails if s32Timer > 733 ms
                    new(OpCodes.Ldloc, loc),
                    new(OpCodes.Ldc_I4, 667),
                    new(OpCodes.Ble_Un, target)); // the condition fails if s32Timer <= 667 ms

        matcher
            .MatchForward(false,
                          new(OpCodes.Ldfld, tutoTimer),
                          new(OpCodes.Ldc_I4_S, (sbyte)20),
                          new(OpCodes.Bne_Un))
            .Advance(1)
            .RemoveInstruction();

        target = (Label)matcher.Operand;
        matcher
            .RemoveInstruction()
            .Insert(new(OpCodes.Ldc_I4, 667),
                    new(OpCodes.Bgt_Un, target), // the condition fails if s32Timer > 667 ms (possibly superfluous but I'd rather be safe)
                    new(OpCodes.Ldloc, loc), // We already stored it in our personal local variable in the last bit.
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Ble_Un, target)); // the condition fails if s32Timer <= 0

        return matcher.Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.UpdateMono))]
    public static IEnumerable<CodeInstruction> DeltaTimePropUpdates(IEnumerable<CodeInstruction> instructions) {
        var deltaTime = AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime));

        List<object> timers = [
            AccessTools.Field(typeof(AttachableProp), nameof(AttachableProp.checkSimpleWait)),
            AccessTools.Field(typeof(AttachableProp), nameof(AttachableProp.checkSimpleWait2)),
            AccessTools.Field(typeof(AttachableProp), nameof(AttachableProp.s16EscapeTimer)),
        ];
        var storesTimer = new CodeMatch(OpCodes.Stfld) { operands = timers };
        var loadsTimer = new CodeMatch(OpCodes.Ldfld) { operands = timers };

        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_S, 30),
                          storesTimer)
            .Repeat(cm => cm
                .SetInstructionAndAdvance(new(OpCodes.Ldc_I4, 1000)))
            .Start()
            // Replace: this.(timer)--;
            // With: this.(timer) -= (int)(Time.deltaTime * 1000f);
            .MatchForward(false,
                          loadsTimer,
                          new(OpCodes.Ldc_I4_1),
                          new(OpCodes.Sub))
            .Repeat(cm => cm
                .Advance(1)                                     // Keep the LDFLD
                .RemoveInstruction()                            // Remove the LDC.I4.1
                .InsertAndAdvance(new(OpCodes.Call, deltaTime), // Replace it with (int)(Time.deltaTime * 1000f)
                                  new(OpCodes.Ldc_R4, 1000f),
                                  new(OpCodes.Mul),
                                  new(OpCodes.Conv_I4)))
            .Instructions();
    }
}

internal static class KatamariFfi {
    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstallHooks();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDeltaTime(float delta);

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateUI();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PickThing(ushort idx);
}
