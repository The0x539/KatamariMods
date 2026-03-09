using BepInEx.Configuration;

using DefineEnum;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevUtils;

public static class SkipIntro {
    private static readonly Dictionary<string, int> missionNames = new() {
        ["cancer"] = Define.GAMEINFO_M_CANCER,
        ["cygnus"] = Define.GAMEINFO_M_CYGNUS,
        ["corona borealis"] = Define.GAMEINFO_M_CORONA_BOREALIS,
        ["pisces"] = Define.GAMEINFO_M_PISCES,
        ["virgo"] = Define.GAMEINFO_M_VIRGO,
        ["gemini"] = Define.GAMEINFO_M_GEMINI,
        ["ursa major"] = Define.GAMEINFO_M_URSA_MAJOR,
        ["taurus"] = Define.GAMEINFO_M_TAURUS,
        ["polaris"] = Define.GAMEINFO_M_POLARIS,
        ["e1"] = 22,
        ["e2"] = 23,
        ["e3"] = 24,
    };

    static SkipIntro() {
        for (var i = 1; i <= 10; i++) {
            var key = i.ToString();
            missionNames[key] = i;
        }
        missionNames["3"] = 4;
        missionNames["4"] = 3;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SplashScreenManager), nameof(SplashScreenManager.StartSplash), MethodType.Enumerator)]
    public static void SkipTitle() {
        LoadSaveFile(SaveManager2.Slot.ONE);
    }

    private static void LoadSaveFile(SaveManager2.Slot slot) {
        GlobalWork.Instance.InitObjMessage();

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Title3Manager), nameof(Title3Manager.Start))]
    public static void LoadDirectlyIntoLevel(Title3Manager __instance) {
        var acceptableValues = new AcceptableValueList<string>(missionNames.Keys.Concat(["none"]).ToArray());
        var entry = Plugin.configFile.Bind("Intro", "Load directly into level", "None", new ConfigDescription("Number/name of level to load into", acceptableValues));

        if (!missionNames.TryGetValue(entry.Value.ToLower().Trim(), out var mission)) {
            return;
        }

        __instance._isSkip = true;
        __instance.StartCoroutine(LoadDirectlyIntoLevelCoroutine(mission));
    }

    public static System.Collections.IEnumerator LoadDirectlyIntoLevelCoroutine(int mission) {
        yield return new WaitForEndOfFrame();
        var gw = GlobalWork.Instance;
        gw.playMission = (GAMEINFO_MIS)mission;
        yield return SceneManager.LoadSceneAsync("GameStart");
        StartsMover.isTitleEarchMode = false;
    }
}
