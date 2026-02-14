using BepInEx;

using HarmonyLib;

using UnityEngine;

namespace DevUtils;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public void Awake() {
        Application.runInBackground = true;

        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(FrustrationMusic));
        Harmony.CreateAndPatchAll(typeof(DebugMenu));

        if (this.Config.Bind("Intro", "Skip", true).Value) {
            Harmony.CreateAndPatchAll(typeof(SkipIntro));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MyGame.InputController), nameof(MyGame.InputController.IsSelectDown))]
    public static void NoClick(ref bool isMouse) {
        isMouse = false;
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
