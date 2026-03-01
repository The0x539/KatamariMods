using HarmonyLib;

using MyGame;

using UnityEngine;

namespace DevUtils;

public static class HideCursor {
    private static Vector3 prevMousePosition;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static void HideOnControllerInput(Player __instance) {
        if (__instance.isControllerOn) {
            UnityEngine.Cursor.visible = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputController), nameof(InputController.Update))]
    public static void ShowOnMouseMovement() {
        if (Input.mousePosition != prevMousePosition) {
            prevMousePosition = Input.mousePosition;
            UnityEngine.Cursor.visible = true;
        }
    }
}
