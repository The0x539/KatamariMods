using HarmonyLib;

using System.Collections.Generic;

using UnityEngine.SceneManagement;

namespace SingleplayerCousins;

public static class SkipIntro {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SplashScreenManager), nameof(SplashScreenManager.StartSplash), MethodType.Enumerator)]
    public static bool SkipIntroPart1(SplashScreenManager __instance) {
        SceneManager.LoadSceneAsync("Title2");
        return false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Title2Manager), nameof(Title2Manager.ChangeAlphaNamco), MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> AccelerateIntroPart2(IEnumerable<CodeInstruction> instructions) {
        var ret = new List<CodeInstruction>();

        foreach (var instr in instructions) {
            if (instr.LoadsConstant()) {
                if (instr.OperandIs(0.5f) || instr.OperandIs(0.8f)) {
                    instr.operand = 100.0f;
                } else if (instr.OperandIs(3f)) {
                    instr.operand = 0f;
                }
            }

            ret.Add(instr);
        }

        return ret;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Title2Manager), nameof(Title2Manager.SetupSelectedSlot))]
    public static void Foo(Title2Manager __instance) {
        __instance._corocoro._rlIndex = 0;
        __instance.StartCoroutine(__instance.GoToNextScene());
    }
}
