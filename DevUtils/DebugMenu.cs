using HarmonyLib;

using MyGame.InputStatus;

using Object = UnityEngine.Object;

namespace DevUtils;

public static class DebugMenu {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameDebug), nameof(GameDebug.Awake))]
    public static void ConnectGameDebug(GameDebug __instance) {
        var pauseMenu = Object.FindObjectOfType<PauseMenu>();
        if (pauseMenu == null) return;
        if (pauseMenu._gameDebug != null) return;

        pauseMenu._gameDebug = __instance;
        pauseMenu.isEnableGameDebug = true;
        __instance.gameObject.SetActive(false);
        __instance._gWork = pauseMenu.gWork;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.PauseProc))]
    public static void ActivateGameDebug(PauseMenu __instance, out bool __runOriginal) {
        __runOriginal = true;
        var self = __instance;

        if (!self.isEnableGameDebug) return;
        if (self._gameDebug is not GameDebug debug) return;
        if (self.gWork.player[0] is not Player player) return;

        var pad = self.input.Pad(player.controllerNo);

        var canvas = debug._canvas.gameObject;

        if (canvas.activeSelf) {
            if (pad.IsDown(KeyMap.Start) || pad.IsDown(KeyMap.B)) {
                __runOriginal = false; // without this, pressing Start from the debug menu immediately opens the pause menu

                self.Pause();
                self.sContinue();
                self.gWork.manGame.sVolumeDiff_End();
                canvas.SetActive(false);
                debug.gameObject.SetActive(false);
            }
        } else {
            if (pad.IsDown(KeyMap.Start) && pad.IsPush(KeyMap.Up)) {
                self.Pause();
                self.gWork.u8Pause = Define.ON;
                self.sPauseSound();
                self.sInit(0);
                canvas.SetActive(true);
                self.canvas.gameObject.SetActive(false);
                debug.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Selector), nameof(Selector.Update))]
    public static void InvokeButton(Selector __instance) {
        if (__instance is not GameDebug self) return;


        if (self._inputBase.Pad(0).IsDown(KeyMap.A)) {
            self._gameDebugSelectable?.uEvent?.Invoke();
        }
    }
}
