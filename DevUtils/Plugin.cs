using BepInEx;
using BepInEx.Unity.Mono;

using HarmonyLib;

using UnityEngine;

namespace DevUtils;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public void Awake() {
        Application.runInBackground = true;

        if (this.Config.Bind("Intro", "Skip", true).Value) {
            Harmony.CreateAndPatchAll(typeof(SkipIntro));
        }
    }
}
