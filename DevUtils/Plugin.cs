using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine;

namespace DevUtils;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public void Awake() {
        Application.runInBackground = true;

        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(DepthOfField));

        if (this.Config.Bind("Intro", "Skip", true).Value) {
            Harmony.CreateAndPatchAll(typeof(SkipIntro));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MyGame.InputController), nameof(MyGame.InputController.IsSelectDown))]
    public static void NoClick(ref bool isMouse) {
        isMouse = false;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    [HarmonyPatch(typeof(MonoOnlyCamera), nameof(MonoOnlyCamera.SetTargetTexture))]
    [HarmonyPatch(typeof(UIFixedCamera), nameof(UIFixedCamera.Start))]
    [HarmonyPatch(typeof(UIMonoCamera), nameof(UIMonoCamera.SetTargetTextureDonotAddCamera))]
    public static IEnumerable<CodeInstruction> MsaaEverywhere(IEnumerable<CodeInstruction> instructions) {
        var setAllowMSAA = AccessTools.PropertySetter(typeof(Camera), nameof(Camera.allowMSAA));

        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_0),
                          new(OpCodes.Callvirt, setAllowMSAA))
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_1)
            .Instructions();
    }
}
