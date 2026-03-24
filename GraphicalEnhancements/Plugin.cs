using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;

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
        AnisotropicGround.Init();
    }

    // Without this patch, the Katamari and Prince still get "drawn" during the Royal Rainbow exit animation.
    // Normally this wouldn't do anything, but they get written to the depth buffer,
    // meaning they end up creating a sort of ghost/shadow for the SSAO effect.
    // (This would also affect the depth of field, but that's probably a lot harder to notice than the ghost.)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_OujiSetAlpha))]
    public static void UpdateKatamariVisibility(GameManager __instance, int player, float alpha) {
        var playerObj = __instance.gWork.player[player];
        var visibleBefore = playerObj.f32Alpha > 0;
        var visibleAfter = alpha > 0;

        if (visibleBefore == visibleAfter) return;

        // This doesn't actually work properly for re-enabling visibility,
        // but we can pretend, since the game doesn't actually use it.
        var active = visibleAfter;

        playerObj.objOuji.SetActive(active);
        playerObj.katamari.SetActive(active);
        playerObj.ShadowTransform.GetChild(0).gameObject.SetActive(active);
        foreach (var effect in playerObj.effectKiraKira ?? []) {
            effect.gameObject.SetActive(active);
        }

        // Here be dragons.
        if (active) {
            foreach (var obj in GlobalWork.Instance.listProp) {
                if (obj != null && obj.GetComponent<MemoryAlpha>() is MemoryAlpha memory) {
                    foreach (var renderer in memory.renderers) {
                        renderer?.enabled = true;
                    }
                }
            }
        } else {
            foreach (var obj in GlobalWork.Instance.listProp) {
                if (obj == null || !obj.IsAttachedToKatamari) continue;

                var memory = obj.gameObject.AddComponent<MemoryAlpha>();
                memory.renderers ??= [];

                IEnumerable<Renderer>
                    mRenderers = obj.mRenderers ?? [],
                    smRenderers = obj.smRenderers ?? [];

                foreach (var renderer in mRenderers.Concat(smRenderers)) {
                    if (renderer != null && renderer.enabled) {
                        renderer.enabled = false;
                        memory.renderers.Add(renderer);
                    }
                }
            }
        }
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

public sealed class MemoryAlpha : MonoBehaviour {
    public List<Renderer> renderers = [];
}