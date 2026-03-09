using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

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

    /*
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.LoadSinglePlayerGame))]
    public static void CowbearCheat() {
        GlobalWork.Instance.bearTypes = [472];
        GlobalWork.Instance.cowTypes = [490];
    }
    */
}
