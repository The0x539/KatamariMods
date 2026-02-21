using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine;

namespace GraphicalEnhancements;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        IngameOptions.AddListener();
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(IngameOptions));
        Harmony.CreateAndPatchAll(typeof(ExtraOptions));
        Harmony.CreateAndPatchAll(typeof(DepthOfField));
        Harmony.CreateAndPatchAll(typeof(HighResRenderTargets));
        Harmony.CreateAndPatchAll(typeof(ShadowAngle));
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