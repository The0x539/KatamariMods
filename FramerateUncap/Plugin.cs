using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;

namespace FramerateUncap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        if (!this.Config.Bind("Main", "Enable", true).Value) return;

        Harmony.CreateAndPatchAll(this.GetType());
        KatamariFfi.InstallHooks();

        if (this.Config.Bind("Inspector", "Enable", false).Value) Clicky.Init();

        Harmony.CreateAndPatchAll(typeof(StereoHaptics));
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
        var sCtrlWhirlpool = AccessTools.Method(typeof(GameManager), nameof(GameManager.sCtrl_Whirlpool));
        var thirtieth = 0.03333333f;

        return new CodeMatcher(instructions)
            // 408: this.sCtrl_Whirlpool()
            // In this case, rather than removing the if statement,
            // just remove the call and use a postfix to insert one without the condition.
            .RemoveMatching(new(OpCodes.Ldarg_0),
                            new(OpCodes.Call, sCtrlWhirlpool)).AssertPos(408)
            // 683: if this.deltaFrame >= 1/30 {} (outer)
            .MatchForward(false, new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, deltaFrame),
                          new(OpCodes.Ldc_R4, thirtieth),
                          new(OpCodes.Blt_Un),
                          new(OpCodes.Ldc_I4_0)).AssertPos(683 - 2)
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
                          new(OpCodes.Stfld, deltaFrame)).AssertPos(693 - 2)
            // New: this.deltaFrame -= deltaTime;
            // Yes, this is a bit silly. I don't want to refactor the target code much more than this,
            // and getting this patch to work properly already took a lot of trial and error.
            .Advance(3).SetInstruction(new(OpCodes.Ldloc_0))
            // 762: this.mSimulation.DoTick(1/30)
            .MatchForward(true,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, mSimulation),
                          new(OpCodes.Ldc_R4, thirtieth)).AssertPos(764 - 2)
            // new argument: deltaTime
            .SetInstruction(new(OpCodes.Ldloc_0))
            // 812: if this.deltaFrame >= 1/30 {} (inner)
            .RemoveMatching(new(OpCodes.Ldarg_0),
                            new(OpCodes.Ldfld, deltaFrame),
                            new(OpCodes.Ldc_R4, thirtieth)).AssertPos(812 - 2)
            // For some reason, this is a backward jump, so we need to keep it and just make it unconditional.
            .SetOpcodeAndAdvance(OpCodes.Br)
            .Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static void UpdateWhirlpool(GameManager __instance) {
        if (__instance.isEnableWhirlpool) {
            __instance.sCtrl_Whirlpool();
        }
    }

    private static readonly MemberInfo
        s32Time = AccessTools.Field(typeof(GameManager), nameof(GameManager.s32Time)),
        s32VsTime = AccessTools.Field(typeof(GameManager), nameof(GameManager.s32VsTime)),
        tutoTimer = AccessTools.Field(typeof(SI_TUTORIAL), nameof(SI_TUTORIAL.s32Timer)),
        gWork = AccessTools.Field(typeof(GameManager), nameof(GameManager.gWork)),
        siTutorial = AccessTools.Field(typeof(GlobalWork), nameof(GlobalWork.siTutorial)),
        f32Scale = AccessTools.Field(typeof(GameManager), nameof(GameManager.f32Scale)),
        getDeltaTime = AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime));

    // Some stuff is fine to stay capped at 30 or 60, but anything called by sMain() or that touches the same timers
    // will need to be updated accordingly.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_GameStateInitGameClear))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_GameStateInitGameOver))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sDemo))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameClear))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameOver))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sTutoTitle))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sStartEffectMain))]
    public static IEnumerable<CodeInstruction> PatchTimerIncrements(IEnumerable<CodeInstruction> instructions) {
        var timers = new List<object> { s32Time, s32VsTime, tutoTimer };
        var loadsTimer = new CodeMatch(OpCodes.Ldfld) { operands = timers };
        var storesTimer = new CodeMatch(OpCodes.Stfld) { operands = timers };

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
            // Match all code patterns that branch if a timer is nonzero
            .MatchForward(false,
                          loadsTimer,
                          new(OpCodes.Brtrue))
            .Repeat(cm => cm
                // Instead branch if that timer is GREATER than zero
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
                .SetOpcodeAndAdvance(OpCodes.Bgt))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameOver))]
    public static IEnumerable<CodeInstruction> TimerFixup1(IEnumerable<CodeInstruction> instructions) {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldfld, s32Time),
                          new(OpCodes.Ldc_I4_S, (sbyte)60),
                          new(OpCodes.Blt))
            .Advance(1)
            .SetInstruction(new(OpCodes.Ldc_I4, 2000)) // 60 frames -> 2000 ms
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sTutoTitle))]
    public static IEnumerable<CodeInstruction> TimerFixup2(IEnumerable<CodeInstruction> instructions, ILGenerator gen) {
        // Old C#: if (s32Timer == 22 frames)
        var matcher = new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldfld, tutoTimer),
                          new(OpCodes.Ldc_I4_S, (sbyte)22),
                          new(OpCodes.Bne_Un))
            .Advance(1)
            .RemoveInstruction();

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
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sStartEffectMain))]
    public static IEnumerable<CodeInstruction> TimerFixup3(IEnumerable<CodeInstruction> instructions) {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldfld, s32VsTime),
                          new(OpCodes.Ldc_I4_S, (sbyte)30),
                          new(OpCodes.Blt))
            .Repeat(cm => cm
                .Advance(1)
                .SetInstruction(new(OpCodes.Ldc_I4, 1000))) // 30 frames -> 1000 ms
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameClear))]
    public static IEnumerable<CodeInstruction> DeltaTimeRainbow(IEnumerable<CodeInstruction> instructions) {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldfld, f32Scale),
                          new(OpCodes.Ldc_R4),
                          new() { opcodes = { OpCodes.Add, OpCodes.Sub } })
            .Repeat(cm => cm
                .Advance(2)
                .Insert(new(OpCodes.Call, getDeltaTime),
                        new(OpCodes.Mul)))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.UpdateMono))]
    public static IEnumerable<CodeInstruction> DeltaTimePropUpdates(IEnumerable<CodeInstruction> instructions) {
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
                .Advance(1)                                        // Keep the LDFLD
                .RemoveInstruction()                               // Remove the LDC.I4.1
                .InsertAndAdvance(new(OpCodes.Call, getDeltaTime), // Replace it with (int)(Time.deltaTime * 1000f)
                                  new(OpCodes.Ldc_R4, 1000f),
                                  new(OpCodes.Mul),
                                  new(OpCodes.Conv_I4)))
            .Instructions();
    }

    // It seems like there was a latent bug in the vanilla game, where PauseProc calls these two functions in *the wrong order*.
    // With the other patches active, this resulted in being unable to exit first-person view using the left trigger/bumper,
    // as the single-frame suppression of the triggers doesn't properly take effect.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.PauseProc))]
    public static IEnumerable<CodeInstruction> FixExitingPrinceView(IEnumerable<CodeInstruction> instructions) {
        var m = new CodeMatcher(instructions);

        var triggerClear = AccessTools.Method(typeof(Player), nameof(Player.TriggerClear));
        var doPS2ControllerSimulation = AccessTools.Method(typeof(Player), nameof(Player.DoPS2ControllerSimulation));

        var secondCall = m.MatchForward(false, new CodeMatch(OpCodes.Callvirt, triggerClear)).Instruction;
        var firstCall = m.MatchBack(false, new CodeMatch(OpCodes.Callvirt, doPS2ControllerSimulation)).Instruction;

        firstCall.operand = triggerClear;
        secondCall.operand = doPS2ControllerSimulation;

        return m.Instructions();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_Game2dRequestWhirlpool))]
    public static void UnpatchWhirlpoolTimer(ref int time) {
        // Things that initialize it to 75 (or 76) are already "patched",
        // since those callers read the value from s32Timer,
        // whose value already gets patched by PatchTimerIncrements.
        // The only other value the game uses is 30.
        if (time == 30) time = 1000;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sCtrl_Whirlpool))]
    public static void WhirlpoolUpdate(GameManager __instance, ref bool __runOriginal) {
        var self = __instance;
        __runOriginal = false;

        var dt = Time.deltaTime * 1000f;

        if (self.whirlTime > 0) {
            self.whirlTime -= (int)dt;
            if (self.whirlTime < 0) self.whirlTime = 0;
        } else if (self.whirlTime == 0) {
            if (self.isChangeGameOver) {
                self.gWork.camGame[0].cullingMask = 0;
            } else {
                self.objWhirlpoolObject.SetActive(false);
            }
            self.isEnableWhirlpool = false;
            return;
        }

        self.sMsgSys.objFade.GetComponent<Image>().color = new(0, 0, 0, 0);
        if (!self.objWhirlpoolObject.activeSelf) {
            self.objWhirlpoolObject.SetActive(true);
        }

        // The original code divides by 4 for the angle change.
        // To compensate for the mistake described below, multiply that by a bit.
        self.whirlAngle += self.whirlAngleDelta * dt * 4f;
        self.whirlScale += self.whirlScaleDelta * dt;
        if (self.whirlScale < 0) self.whirlScale = 0;

        var t = self.objWhirlpool.transform;
        t.localScale = new(self.whirlScale, self.whirlScale, 1f);
        t.localPosition = self.gWork.sWhirlpoolPosi;
        // I think the original code was doing rad2deg, wrongly, and nobody noticed because of 30 FPS
        // evidence:
        // - the values for angleE passed to Game2dRequestWhirlpool are 180 and 540
        // - the equivalent to this code multiplies by 180/pi, which is nonsensical if starting from degrees.
        // - Quaternion.Euler takes degrees anyway
        t.rotation = Quaternion.Euler(0, 0, self.whirlAngle);
    }
}

internal static partial class KatamariFfi {
    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstallHooks();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDeltaTime(float delta);
}
