using DefineEnum;

using HarmonyLib;

namespace DevUtils;

public static class VersusOnEarth {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalWork), nameof(GlobalWork.SetRandomMission))]
    public static void Foo(GlobalWork __instance) {
        __instance.playMission = GAMEINFO_MIS.GAMEINFO_MIS_03;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalWork), nameof(GlobalWork.InitObjMessage))]
    public static void Bar(GlobalWork __instance) {
        __instance.playMission = GAMEINFO_MIS.GAMEINFO_VS_00;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalWork), nameof(GlobalWork.LoadMissionData))]
    public static void Baz(GlobalWork __instance) {
        __instance.playMission = GAMEINFO_MIS.GAMEINFO_MIS_03;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.Init))]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.MonoInitStart))]
    public static void Qux(ref int mission) {
        mission = (int)GAMEINFO_MIS.GAMEINFO_VS_00;
    }
}
