using BepInEx;
using BepInEx.Configuration;

using DefineEnum;

using HarmonyLib;

using System.Collections;

using UnityEngine;

namespace DevUtils;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public static ConfigFile configFile = null!;

    public void Awake() {
        configFile = this.Config;

        Application.runInBackground = true;

        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(FrustrationMusic));
        Harmony.CreateAndPatchAll(typeof(DebugMenu));
        Harmony.CreateAndPatchAll(typeof(HideCursor));
        //Harmony.CreateAndPatchAll(typeof(VersusOnEarth));
        Harmony.CreateAndPatchAll(typeof(LocalizationTweaks));
        Harmony.CreateAndPatchAll(typeof(EarlyOptions));

        if (this.Config.Bind("Intro", "Skip", false).Value) {
            Harmony.CreateAndPatchAll(typeof(SkipIntro));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MyGame.InputController), nameof(MyGame.InputController.IsSelectDown))]
    public static void NoClick(ref bool isMouse) {
        isMouse = false;
    }

    // When a "non-skippable" / "passive" / "background" message is visible,
    // it's possible to "buffer" a message skip by pressing START.
    // This is a bug that's haunted me for weeks, assuming it was my mod's fault because of how sporadically I would encounter it.
    // Fix the bug by clearing the relevant flags whenever there's no message present.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MsgSys), nameof(MsgSys.Update))]
    public static void FixSkipBuffering(MsgSys __instance) {
        if (__instance.u8Job == 0) {
            __instance.isKeySkip = false;
            __instance.isSelectDown = false;
        }
    }

    // This patch is a great example of how much more code it takes
    // to figure out what exactly to patch than ends up in the final result.
    // This started with trying to make it so that the *animation* for crossing a size threshold triggered multiple times.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.SetNextArea))]
    public static void MultiAreaChange(GlobalManager __instance) {
        var gw = __instance.gWork;
        if (gw.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_VS) return;

        var threshold = gw.changeArea[gw.playArea];
        var size = gw.katamariDiameterInt[Define.PLAYER_1] / 10f;
        var shouldKeepGoing = threshold > 0f && size >= threshold;
        if (shouldKeepGoing) {
            gw.StartCoroutine(KeepGoing(gw));
        }
    }

    private static IEnumerator KeepGoing(GlobalWork gw) {
        // Patches welcome if you can figure out how to make this timing tighter while remaining reliable.
        yield return new WaitForSeconds(2);
        while (GlobalManager.Instance.sMsgSys.u8Job != 0) yield return new WaitForSeconds(2);
        yield return new WaitForSeconds(2);
        gw.u8SwMapChange = 1;
    }

    /*
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.LoadSinglePlayerGame))]
    public static void CowbearCheat() {
        GlobalWork.Instance.bearTypes = [472];
        GlobalWork.Instance.cowTypes = [490];
    }
    */
}
