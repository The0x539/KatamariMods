using HarmonyLib;

using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine.SceneManagement;

namespace DevUtils;

public static class SkipIntro {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SplashScreenManager), nameof(SplashScreenManager.StartSplash), MethodType.Enumerator)]
    public static void SkipTitle() {
        LoadSaveFile(SaveManager2.Slot.ONE);
    }

    private static void LoadSaveFile(SaveManager2.Slot slot) {
        var sys = new SystemSaveData();
        if (SaveManager2.ExistSystemData() && !SaveManager2.SystemLoad(ref sys)) {
            throw new System.Exception("System save seems corrrupt?");
        }

        var save = new SaveManager2.SaveData();
        save.Init();

        SaveManager2.Load(ref save, slot);
        SaveManager2.SetSaveData(ref save, GlobalWork.Instance);
        GlobalWork.Instance.saveSlotIndex = 0;
        SceneManager.LoadScene("Title3");
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Title3Manager), nameof(Title3Manager.Start), MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> SkipMovie(IEnumerable<CodeInstruction> instructions) {
        var playMove = AccessTools.Method(typeof(UIMoviePlayer), nameof(UIMoviePlayer.PlayMove));

        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, playMove))
            .Repeat(cm => cm
                .RemoveInstruction()
                .InsertAndAdvance(new(OpCodes.Pop),
                                  new(OpCodes.Pop),
                                  new(OpCodes.Pop)))
            .Instructions();
    }
}
