using DefineEnum;

using HarmonyLib;

using UnityEngine;

namespace FramerateUncap;

public static class TimerPatches {
    public static float initialTime = 60f;
    public static float gameTime = 0f;
    public static float remainingTime = 0f;

#pragma warning disable IDE1006
    private static GlobalWork __gWork = null!;
    private static GlobalWork gWork => __gWork ??= GlobalWork.Instance;

    private static GlobalManager __gman = null!;
    private static GlobalManager gman => __gman ??= GlobalManager.Instance;
#pragma warning restore IDE1006

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_GameStateInit))]
    public static void ScaleInitialTimer(GameManager __instance) {
        if (gWork.s32RemainTime == 1800) {
            remainingTime = 60f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.gYm_GameSetTime))]
    public static bool GameSetTime(int time) {
        remainingTime = initialTime = time / 30f;
        gWork.f32RemainTimeRatio = 1f;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sProcRemainTime))]
    public static bool ProcRemainTime(GameManager __instance) {
        var game = __instance;
        var state = gWork.u8State;

        if (state == GAMEINFO_STATE.GAMEINFO_STATE_DEMO) {
            if (gWork.u8SwFreeze == 0) game.sSubRemainTime();
            return false;
        }


        if (state != GAMEINFO_STATE.GAMEINFO_STATE_PLAY) return false;

        if (gWork.u8SwReduceTime == 0 || gWork.u8Pause != 0) return false;

        var oldTime = remainingTime;
        if (game.sSubRemainTime()) {
            TimerAlerts(oldTime, remainingTime, game);
        }

        gWork.f32RemainTimeRatio = remainingTime / initialTime;
        var clear = game.gYm_GameClearCheck();
        if (gWork.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_1P) {
            gWork.u8SwGameOn = 0;
            gWork.u8ClearStatus = 0;

            switch (clear) {
                case GAMEINFO_CLEAR.GAMEINFO_CLEAR_SUCCESS:
                    game.gYm_GameStateInitGameClear();
                    game.gYm_GameStop();
                    break;

                case GAMEINFO_CLEAR.GAMEINFO_CLEAR_FAILURE:
                    game.gYm_GameStateInitGameOver();
                    game.gYm_OujiMotionCtrl(Define.PLAYER_1);
                    game.gYm_GameStop();
                    break;

                case GAMEINFO_CLEAR.GAMEINFO_CLEAR_PLAY:
                default:
                    break;
            }
        } else {
            if (clear != GAMEINFO_CLEAR.GAMEINFO_CLEAR_PLAY) {
                game.gYm_GameStop();
                gWork.u8SwGameOn = 0;
                gWork.u8SwReduceTime = 0;
                game.gYm_VsRequestResult();
            }
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sSubRemainTime))]
    public static bool SubRemainTime(GameManager __instance, ref bool __result) {
        // the original function returns true iff it reaches the end.
        // this function always returns false, because it's a Harmony patch that suppresses the original function.
        __result = false;

        if (gWork.u8SwFreeze != 0) return false;

        gameTime += Time.deltaTime;
        gWork.u32GameTime = (int)(gameTime * 30f);

        bool hasTimeLimit = true;
        switch (gWork.u8GameType) {
            case GI_GAMETYPE.GI_GAMETYPE_N:
                // Make a Star 1 also serves as the second half of the tutorial.
                // Under those circumstances, its time limit is disabled.
                hasTimeLimit = gWork.u8GameMode != GI_GMODE.GI_GMODE_TUTORIAL_B;
                break;

            case GI_GAMETYPE.GI_GAMETYPE_A:
            case GI_GAMETYPE.GI_GAMETYPE_B:
            case GI_GAMETYPE.GI_GAMETYPE_D:
                hasTimeLimit = true;
                break;

            case GI_GAMETYPE.GI_GAMETYPE_C:
            case GI_GAMETYPE.GI_GAMETYPE_E:
                hasTimeLimit = false;
                break;

            case GI_GAMETYPE.GI_GAMETYPE_S:
            case GI_GAMETYPE.GI_GAMETYPE_X:
                // prevent the caller from receiving a `true` return value
                return false;
        }

        if (gWork.u8SwPlayDemo != 0) return false;

        if (hasTimeLimit && remainingTime > 0) {
            remainingTime = Mathf.MoveTowards(remainingTime, 0, Time.deltaTime);
            gWork.s32RemainTime = (int)(remainingTime * 30); // for compatibility

            if (remainingTime == 0) {
                SoundController.Instance.Play(SoundController.eChannel.SE, SoundData.eClipGroup.GameBank, 46, false, 1, 0, 1, 0);
            }
        }

        __result = true;
        return false;
    }

    private static void TimerAlerts(float oldTime, float newTime, GameManager game) {
        if (gWork.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_1P) {
            if (oldTime > 60 && newTime <= 60) game.sMsgSys.gYm_MessageRequest("@COM_TIME");
        } else {
            if (oldTime > 30 && newTime <= 30) game.gYm_VsRequestTelop();
        }

        if (newTime <= 30) {
            if (Mathf.Floor(newTime) != Mathf.Floor(oldTime)) {
                SoundController.Instance.Play(SoundController.eChannel.SE, SoundData.eClipGroup.GameBank, 38, false, 1f, 0f, 1f, 0);
            }
        }
    }
}