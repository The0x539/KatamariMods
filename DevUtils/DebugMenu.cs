using HarmonyLib;

using MyGame.InputStatus;

using System.Collections;

using UnityEngine;

namespace DevUtils;

public static class DebugMenu {
    private static void FixMenu(GameDebug menu) {
        if (menu._gWork != null) return;
        menu._gWork = GlobalWork.Instance;

        const int ROWS = 4;
        const int COLS = 5;
        const int ITEMS = ROWS * COLS;

        var buttons = new GameDebugSelectable[ITEMS];
        var buttonParent = menu._gameDebugSelectable.transform.parent;

        for (var i = 0; i < ITEMS; i++) {
            buttons[i] = buttonParent.GetChild(i).GetComponent<GameDebugSelectable>();
        }

        for (var i = 0; i < ITEMS; i++) {
            buttons[i]._up = buttons[(i + ITEMS - COLS) % ITEMS];
            buttons[i]._down = buttons[(i + COLS) % ITEMS];
        }

        menu.StartCoroutine(FixMenuPart2(menu));
    }

    private static IEnumerator FixMenuPart2(GameDebug menu) {
        yield return new WaitForSecondsRealtime(0.1f);
        menu._image_cursor.transform.position = menu._gameDebugSelectable.transform.position;
    }

    private static bool menuOpen = false;

    public static KonamiCode konamiCode = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Start))]
    public static void ListenForKonamiCode(GameManager __instance) {
        var listener = new GameObject("Konami Code Listener");
        listener.transform.parent = __instance.transform;
        konamiCode = listener.AddComponent<KonamiCode>();
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
                menuOpen = false;
                __runOriginal = false; // without this, pressing Start from the debug menu immediately opens the pause menu

                self.Pause();
                self.sContinue();
                self.gWork.manGame.sVolumeDiff_End();
                canvas.SetActive(false);
                debug.gameObject.SetActive(false);
            }
        } else {
            if (pad.IsDown(KeyMap.Start) && konamiCode.IsPrimed) {
                konamiCode.Reset();

                menuOpen = true;
                self.Pause();
                self.gWork.u8Pause = Define.ON;
                self.sPauseSound();
                self.sInit(0);
                canvas.SetActive(true);
                self.canvas.gameObject.SetActive(false);
                debug.gameObject.SetActive(true);
                FixMenu(debug);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Selector), nameof(Selector.Update))]
    public static void InvokeButton(Selector __instance) {
        if (__instance is not GameDebug self) return;

        if (menuOpen && self._inputBase.Pad(0).IsDown(KeyMap.A)) {
            self._gameDebugSelectable?.uEvent?.Invoke();
        }
    }
}
