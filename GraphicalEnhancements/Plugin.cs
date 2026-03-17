using BepInEx;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace GraphicalEnhancements;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        IngameOptions.AddListener();
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(IngameOptions));
        Harmony.CreateAndPatchAll(typeof(ExtraOptions));
        Harmony.CreateAndPatchAll(typeof(PostProcessing));
        Harmony.CreateAndPatchAll(typeof(QualityRenderTargets));
        Harmony.CreateAndPatchAll(typeof(ShadowAngle));
        Harmony.CreateAndPatchAll(typeof(SpecialDrawPatches));
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
    [HarmonyPatch(typeof(EarchObject), nameof(EarchObject.Start))]
    public static void AnisotropicEarthObjects(EarchObject __instance) {
        __instance.GetComponent<MeshRenderer>().sharedMaterial.mainTexture.anisoLevel = 16;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnPostRender))]
    public static void FixTVResolution(GameManager __instance) {
        if (__instance.texCapture is not RenderTexture tv) return;

        var res = QualitySetting.Instance.Resolution;
        if (res != new Vector2(tv.width, tv.height)) {
            tv.Release();
            tv.width = (int)res.x;
            tv.height = (int)res.y;
            tv.Create();
        }
    }
}

public sealed class MemoryAlpha : MonoBehaviour {
    public List<Renderer> renderers = [];
}