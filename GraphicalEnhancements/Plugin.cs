using BepInEx;

using HarmonyLib;

namespace GraphicalEnhancements;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        IngameOptions.AddListener();
        Harmony.CreateAndPatchAll(typeof(IngameOptions));
        Harmony.CreateAndPatchAll(typeof(ExtraOptions));
        Harmony.CreateAndPatchAll(typeof(DepthOfField));
        Harmony.CreateAndPatchAll(typeof(HighResRenderTargets));
    }
}