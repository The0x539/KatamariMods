using BepInEx;

using HarmonyLib;

using UnityEngine;

namespace GraphicalEnhancements;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(ExtraOptions));
        Harmony.CreateAndPatchAll(typeof(PostProcessing));
        Harmony.CreateAndPatchAll(typeof(QualityRenderTargets));
        Harmony.CreateAndPatchAll(typeof(ShadowAngle));
        Harmony.CreateAndPatchAll(typeof(SpecialDrawPatches));
        Harmony.CreateAndPatchAll(typeof(TVFixes)); // This patch should go after PostProcessing's patch so that the TV texture blit sees all the post-processing effects
        Harmony.CreateAndPatchAll(typeof(TransparencyFixes));
        AnisotropicGround.Init();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EarchRotator), nameof(EarchRotator.Start))]
    public static void AnisotropicEarch(EarchRotator __instance) => MakeAnisotropic(__instance._tranSphere);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OujiStarRotator), nameof(OujiStarRotator.Start))]
    public static void AnisotropicOujiStar(OujiStarRotator __instance) => MakeAnisotropic(__instance._tranSphere);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.Start))]
    public static void AnisotropicKinoko(KinokoRotator __instance) => MakeAnisotropic(__instance._pre_kinoko.transform.Find("pre_kinoko"));

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EndingController), nameof(EndingController.Start))]
    public static void AnisotropicCredits(EndingController __instance) {
        MakeAnisotropic(__instance._animator_kinoko);
        MakeAnisotropic(__instance._animator_oujiStar);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EndingEarthController), nameof(EndingEarthController.Start))]
    public static void AnisotropicCredits(EndingEarthController __instance) => MakeAnisotropic(__instance);

    private static void MakeAnisotropic(Component t) => MakeAnisotropic(t.gameObject);

    private static void MakeAnisotropic(GameObject obj) {
        foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>(includeInactive: true)) {
            renderer.sharedMaterial?.mainTexture?.anisoLevel = 16;
        }
    }
}