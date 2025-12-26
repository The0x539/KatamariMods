using HarmonyLib;

using Steamworks;

using UnityEngine;

namespace KatamariDama60;

public static class SkipSteam {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Application), nameof(Application.Quit))]
    public static bool DoNotQuit() {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamAPI), nameof(SteamAPI.InitSafe))]
    [HarmonyPatch(typeof(SteamAPI), nameof(SteamAPI.Init))]
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
    public static bool DoNotInitialize() {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamUser), nameof(SteamUser.GetSteamID))]
    public static bool SpoofTheID(ref CSteamID __result) {
        __result = new CSteamID(80645326);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Initialized), MethodType.Getter)]
    public static bool PretendToBeInitialized(ref bool __result) {
        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamApps), nameof(SteamApps.GetCurrentGameLanguage))]
    public static bool SpoofTheLanguage(ref string __result) {
        __result = "english";
        return false;
    }
}
