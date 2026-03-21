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

    private static int deltaMillis = 0;
    private static float accumulatedRoundingError = 0;

    public void Update() {
        var dt = Time.deltaTime * 1000f;
        deltaMillis = Mathf.FloorToInt(dt);
        accumulatedRoundingError += dt % 1f;
        while (accumulatedRoundingError >= 1f) {
            accumulatedRoundingError -= 1f;
            deltaMillis += 1;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Tick))]
    public static void OnTick(float delta) {
        KatamariFfi.SetDeltaTime(delta);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static IL Uncap(IL il) {
        MemberInfo
            deltaFrame = Member.Field<GameManager>(gm => gm.deltaFrame),
            mSimulation = Member.Field<GameManager>(gm => gm.mSimulation),
            sCtrlWhirlpool = Member.Method<GameManager>(gm => gm.sCtrl_Whirlpool());
        var thirtieth = 0.03333333f;

        return new CodeMatcher(il)
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
        s32Time = Member.Field<GameManager>(gm => gm.s32Time),
        s32VsTime = Member.Field<GameManager>(gm => gm.s32VsTime),
        s32VSTime = Member.Field<GameManager>(gm => gm.s32VSTime), // ಠ_ಠ
        tutoTimer = Member.Field<SI_TUTORIAL>(tut => tut.s32Timer),
        gWork = Member.Field<GameManager>(gm => gm.gWork),
        siTutorial = Member.Field<GlobalWork>(gw => gw.siTutorial),
        f32Scale = Member.Field<GameManager>(gm => gm.f32Scale),
        f32KataAlpha = Member.Field<GameManager>(gm => gm.f32KataAlpha),
        getDeltaTime = Member.Getter(() => Time.deltaTime),
        fieldDeltaMillis = Member.Field(() => Plugin.deltaMillis),
        f32VSScale = Member.Field<GameManager>(gm => gm.f32VSScale),
        s32SepaTime = Member.Field<GameManager>(gm => gm.s32SepaTime);

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
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sVSCancel))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sResultMain))]
    public static IL PatchTimerIncrements(IL il) {
        var timers = new List<object> { s32Time, s32VsTime, s32VSTime, tutoTimer };
        var loadsTimer = new CodeMatch(OpCodes.Ldfld) { operands = timers };
        var storesTimer = new CodeMatch(OpCodes.Stfld) { operands = timers };

        return new CodeMatcher(il)
            // Match all code patterns that increment or decrement a timer by 1
            .MatchForward(false,
                          loadsTimer,
                          new(OpCodes.Ldc_I4_1),
                          new(ci => ci.opcode == OpCodes.Add || ci.opcode == OpCodes.Sub),
                          storesTimer)
            .Repeat(cm => cm
                // Replace the 1 with Plugin.deltaMillis
                .Advance(1)
                .Set(OpCodes.Ldsfld, fieldDeltaMillis))
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
    public static IL TimerFixup1(IL il) {
        return new CodeMatcher(il)
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
    public static IL TimerFixup2(IL il, ILGenerator gen) {
        var loc = gen.DeclareLocal(typeof(int));

        return new CodeMatcher(il)
            .MatchForward(false,
                          // Old C#: if (timer == 22 frames)
                          new(OpCodes.Ldfld, tutoTimer),
                          new(OpCodes.Ldc_I4_S, (sbyte)22),
                          new(OpCodes.Bne_Un))
            .Advance(1)
            .RemoveInstruction()
            .GetOperand(out Label target)
            .RemoveInstruction()
            // New C#: if (timer <= 733 ms && timer > 667 ms)
            .Insert(new(OpCodes.Stloc, loc),
                    new(OpCodes.Ldloc, loc),
                    new(OpCodes.Ldc_I4, 733),
                    new(OpCodes.Bgt_Un, target), // the condition fails if s32Timer > 733 ms
                    new(OpCodes.Ldloc, loc),
                    new(OpCodes.Ldc_I4, 667),
                    new(OpCodes.Ble_Un, target)) // the condition fails if s32Timer <= 667 ms
            .MatchForward(false,
                          // Old C#: if (timer == 20 frames)
                          new(OpCodes.Ldfld, tutoTimer),
                          new(OpCodes.Ldc_I4_S, (sbyte)20),
                          new(OpCodes.Bne_Un))
            .Advance(1)
            .RemoveInstruction()
            .GetOperand(out target)
            .RemoveInstruction()
            // New C#: if (timer <= 667 ms && timer > 0)
            .Insert(new(OpCodes.Ldc_I4, 667),
                    new(OpCodes.Bgt_Un, target), // the condition fails if s32Timer > 667 ms (possibly superfluous but I'd rather be safe)
                    new(OpCodes.Ldloc, loc), // We already stored it in our personal local variable in the last bit.
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Ble_Un, target)) // the condition fails if s32Timer <= 0
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sStartEffectMain))]
    public static IL TimerFixup3(IL il) {
        return new CodeMatcher(il)
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
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static IL FixSplitscreenSeparatorEasing(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Dup),
                          new(OpCodes.Ldfld, s32SepaTime),
                          new(OpCodes.Ldc_I4_1),
                          new(OpCodes.Sub),
                          new(OpCodes.Stfld, s32SepaTime))
            .RemoveMatching(new(OpCodes.Ldc_I4_1),
                            new(OpCodes.Sub))
            .Insert([new(OpCodes.Call, Member.Method(() => DecrementSepaTimer(0)))])
            .RemoveMatching(new(OpCodes.Ldc_R4, 0.017453292f),
                            new(OpCodes.Mul),
                            new(OpCodes.Call, Member.Method(() => Mathf.Sin(0f))))
            .Insert([new(OpCodes.Call, Member.Method(() => EaseSeparator(0f)))])
            // Skip the misguided 1-second lerp that would otherwise "kick in" after the original sine lerp fails to do its job.
            .MatchForward(false,
                          new(OpCodes.Ldc_R4, 0f),
                          new(OpCodes.Stfld, Member.Field<GameManager>(gm => gm.separateTimer)))
            .SetOperandAndAdvance(1.0001f) // 1.0 triggers a different codepath that doesn't do the right thing.
            .Instructions();
    }

    private static int DecrementSepaTimer(int n) {
        n -= deltaMillis;
        if (n < 0) n = 0;
        return n;
    }

    private static float EaseSeparator(float t) => Mathf.Cos(t * Mathf.PI) * -0.5f + 0.5f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_SepaRequest))]
    public static void UseMillisecondsForSplitscreenTimer(ref int time) {
        time *= 1000;
        time /= 30;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sGameClear))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sVSCancel))]
    public static IL DeltaTimeRainbowAndVsZoom(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldfld) { operands = { f32Scale, f32KataAlpha, f32VSScale } },
                          new(OpCodes.Ldc_R4),
                          new() { opcodes = { OpCodes.Add, OpCodes.Sub } })
            .Repeat(cm => cm
                .Advance(2)
                .Insert(new(OpCodes.Call, getDeltaTime),
                        new(OpCodes.Mul)))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sVolumeDiff_Main))]
    public static IL DeltaTimeSizeDiffSpin(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldfld, Member.Field<GlobalManager>(gm => gm.collectRot)),
                          new(OpCodes.Ldc_R4),
                          new(OpCodes.Add))
            .Advance(2)
            .Insert(new(OpCodes.Call, Member.Getter(() => Time.unscaledDeltaTime)),
                    new(OpCodes.Ldc_R4, 30f),
                    new(OpCodes.Mul),
                    new(OpCodes.Mul))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.UpdateMono))]
    public static IL DeltaTimePropUpdates(IL il) {
        List<object> timers = [
            Member.Field<AttachableProp>(ap => ap.checkSimpleWait),
            Member.Field<AttachableProp>(ap => ap.checkSimpleWait2),
            Member.Field<AttachableProp>(ap => ap.s16EscapeTimer),
        ];

        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_S, 30),
                          new(OpCodes.Stfld) { operands = timers })
            .Repeat(cm => cm
                .SetAndAdvance(OpCodes.Ldc_I4, 1000))
            .Start()
            // Replace: this.(timer)--;
            // With: this.(timer) -= Plugin.deltaMillis
            .MatchForward(false,
                          new(OpCodes.Ldfld) { operands = timers },
                          new(OpCodes.Ldc_I4_1),
                          new(OpCodes.Sub))
            .Repeat(cm => cm
                .Advance(1)
                .Set(OpCodes.Ldsfld, fieldDeltaMillis))
            .Instructions();
    }

    // It seems like there was a latent bug in the vanilla game, where PauseProc calls these two functions in *the wrong order*.
    // With the other patches active, this resulted in being unable to exit first-person view using the left trigger/bumper,
    // as the single-frame suppression of the triggers doesn't properly take effect.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.PauseProc))]
    public static IL FixExitingPrinceView(IL il) {
        var m = new CodeMatcher(il);

        var triggerClear = Member.Method<Player>(p => p.TriggerClear());
        var doPS2ControllerSimulation = Member.Method<Player>(p => p.DoPS2ControllerSimulation());

        var secondCall = m.MatchForward(false, new CodeMatch(OpCodes.Callvirt, triggerClear)).Instruction;
        var firstCall = m.MatchBack(false, new CodeMatch(OpCodes.Callvirt, doPS2ControllerSimulation)).Instruction;

        firstCall.operand = triggerClear;
        secondCall.operand = doPS2ControllerSimulation;

        return m.Instructions();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_Game2dRequestWhirlpool))]
    public static bool WhirlpoolStart(GameManager __instance, ref int time, ref float scaleS, ref float scaleE, float angleS, float angleE) {
        __instance.isEnableWhirlpool = true;

        // Things that initialize it to 75 (or 76) are already "patched",
        // since those callers read the value from s32Timer,
        // whose value already gets patched by PatchTimerIncrements.
        // The only other value the game uses is 30.
        if (time == 30) time = 1000;
        scaleS *= 2;
        scaleE *= 2;

        // The vanilla code multiplies the timer by 2.
        // It does this because all the things that start the timer operate on a 30 Hz cycle,
        // while the whirlpool instead gets animated at 60.
        // With the patches, everything just counts milliseconds instead, so this becomes unnecessary.
        __instance.whirlTime = time;
        __instance.whirlScaleDelta = (scaleE - scaleS) / time;
        __instance.whirlAngleDelta = (angleE - angleS) / time;
        __instance.whirlScale = scaleS;
        __instance.whirlAngle = angleS;

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sCtrl_Whirlpool))]
    public static void WhirlpoolUpdate(GameManager __instance, ref bool __runOriginal) {
        var self = __instance;
        __runOriginal = false;

        if (self.whirlTime > 0) {
            self.whirlTime -= deltaMillis;
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

        var dt = Time.deltaTime * 1000f;
        self.whirlAngle += self.whirlAngleDelta * dt;
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
        // The angle is negated because a positive value means counterclockwise rotation,
        // while the PS2 game clearly has it rotate clockwise.
        // As a result of the math mishap described above, the vanilla game achieves the illusion of slow clockwise rotation
        // by rotating slightly less than ⅛ turn every update and leveraging the rotational symmetry of the sprite.
        t.rotation = Quaternion.Euler(0, 0, -self.whirlAngle);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PS2KatamariSimulation), nameof(PS2KatamariSimulation.DoTick))]
    public static void UpperBound(ref float delta) {
        delta = Mathf.Min(delta, 0.1f);
    }
}

internal static partial class KatamariFfi {
    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstallHooks();

    [DllImport("katamari_ffi", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDeltaTime(float delta);
}
