using DefineEnum;

using HarmonyLib;

using System.Collections.Generic;

namespace DevUtils;

public static class FrustrationMusic {
    private static readonly Dictionary<int, bool> playedThisSession = new() {
        [Define.GAMEINFO_M_URSA_MAJOR] = false,
        [Define.GAMEINFO_M_TAURUS] = false,
        [Define.GAMEINFO_M_POLARIS] = false,
    };

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_GameBgmStart))]
    public static void Foo(GameManager __instance, out bool __runOriginal) {
        __runOriginal = true;
        var gWork = __instance.gWork;

        if (gWork.u8GameMode != GI_GMODE.GI_GMODE_NORMAL) return;
        if (gWork.u8GameInfoMode != GAMEINFO_MODE.GAMEINFO_MODE_1P) return;

        var mission = (int)gWork.playMission;
        // If not present in the dictionary, then this isn't a level that this patch cares about.
        if (!playedThisSession.TryGetValue(mission, out var playedBefore)) return;
        playedThisSession[mission] = true;
        // If the value in the dictionary wasn't already true,
        // then this is the first time entering the level this session,
        // and we should therefore play the usual track.
        if (!playedBefore) return;

        // If we get to this point, the level in question has already been played at least once in this session.
        // As such, we should play a random track instead of the usual.
        __runOriginal = false;
        __instance.gYm_GameBgmStartEternal();
    }
}
