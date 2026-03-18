using DefineEnum;

using HarmonyLib;

using MyGame;
using MyGame.InputStatus;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevUtils;

public static class EarlyOptions {
    // Normally, the SELECT ("Back") button toggles vibration mid-level.
    // This always seemed strange to me, but whatever.
    // However, during the first phase of the tutorial,
    // we can repurpose it to instead open the full-fledged options menu,
    // since the normal pause menu isn't accessible yet.
    // (From there, you can toggle vibration the slow way, if you want, so the King's dialogue is still accurate.)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.sSetVibration))]
    public static void ReplaceVibrationWithFullMenu(Player __instance, out bool __runOriginal) {
        __runOriginal = false;

        // Ignore presses that are part of the konami code entry.
        // KonamiCode's update happens during LateUpdate, which ends up meaning it's actually *ahead* of this update.
        if (DebugMenu.konamiCode.IsPrimed) return;

        var p = __instance;
        if (p.gWork.u8GameMode != GI_GMODE.GI_GMODE_TUTORIAL) {
            __runOriginal = true;
            return;
        }

        // Perform most of the same early-bail checks that the original method does
        if (p.gWork.u8State != GAMEINFO_STATE.GAMEINFO_STATE_PLAY) return;
        if (p.sMsgSys.u8Job != 0) return;
        if (p.sKing.u8SwAppear != 0) return;
        // gamemode checks aren't necessary because we already established that we're in Tutorial A
        if (p.gYm_GameClearCheckApplyE()) return;
        if (!InputController.Instance.Pad(p.playerNo).IsDown(KeyMap.Back)) return;

        // At this point, the original code toggles vibration. Instead, open the options menu.
        p.StartCoroutine(ShowOptionsMenu(p));
    }

    private static System.Collections.IEnumerator ShowOptionsMenu(Player p) {
        var optionsScene = SceneManager.GetSceneByName("Option");
        if (optionsScene.isLoaded) yield break;
        yield return SceneManager.LoadSceneAsync("Option", LoadSceneMode.Additive);

        var tutorialUI = p.gWork.manGame.hudController.GetComponent<Canvas>();

        foreach (var obj in optionsScene.GetRootGameObjects()) {
            if (obj.GetComponent<KatamariPauseController>() is not KatamariPauseController kpc) continue;
            var fancyCursor = kpc.cursorImages[0];
            fancyCursor.transform.parent.GetChild(0).gameObject.SetActive(true);
            kpc.transform.Find("Canvas/RawImage").gameObject.SetActive(true);
            break;
        }

        p.gWork.u8Pause = 1;
        Time.timeScale = 0;
        p.CharacterAnimController.PauseAnimation();
        tutorialUI.enabled = false;

        void onUnload(Scene scene) {
            if (scene == optionsScene) {
                SceneManager.sceneUnloaded -= onUnload;
                p.gWork.u8Pause = 0;
                Time.timeScale = 1;
                p.CharacterAnimController.ResumeAnimation();
                tutorialUI.enabled = true;
            }
        }
        SceneManager.sceneUnloaded += onUnload;
    }
}