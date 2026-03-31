using HarmonyLib;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace GraphicalEnhancements;

public static class TransparencyFixes {
    // The vanilla code works fine for distance fade,
    // but for some reason props end up using the wrong material
    // if attached to the katamari during the Royal Rainbow animation.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.SetShaderTransparent))]
    public static void FixTransparent(AttachableProp __instance, out bool __runOriginal) {
        var self = __instance;
        if (!self.isMultiMesh) {
            __runOriginal = true;
            return;
        }

        __runOriginal = false;

        if (self.isDisableSimple && !self.IsAttachedToKatamari) return;
        self.isShaderTransparent = true;

        var state = self.GetComponent<TransparencyFixState>();
        if (state == null) {
            state = self.gameObject.AddComponent<TransparencyFixState>();
            state.opaqueMaterials = [.. self.monoMaterial];
        }

        for (var i = 0; i < self.meshMaterialName.Length; i++) {
            var name = self.meshMaterialName[i].Split('(')[0].Trim();
            name = self.activeScene + "Transparent" + name;
            if (self.gWork.dicPropMaterial.TryGetValue(name, out var mat)) {
                self.monoMaterial[i] = mat;
                self.isChangeMaterial = true;
            }
        }

        self.SetMeshMaterial(self.activeScene, self.mRenderers, self.smRenderers);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.SetMeshMaterial))]
    public static void FixOpaque(AttachableProp __instance) {
        var self = __instance;
        if (self.isShaderTransparent && self.f32AlphaRatio == 1) {
            if (self.GetComponent<TransparencyFixState>() is TransparencyFixState state) {
                self.monoMaterial = [.. state.opaqueMaterials];
                self.isChangeMaterial = true;
            }
        }
    }

    // Without this patch, the Katamari and Prince still get "drawn" during the Royal Rainbow exit animation.
    // Normally this wouldn't do anything, but they get written to the depth buffer,
    // meaning they end up creating a sort of ghost/shadow for the SSAO effect.
    // (This would also affect the depth of field, but that's probably a lot harder to notice than the ghost.)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_OujiSetAlpha))]
    public static void UpdateKatamariVisibility(GameManager __instance, int player, float alpha, out bool __state) {
        var playerObj = __instance.gWork.player[player];
        var visibleBefore = playerObj.f32Alpha > 0;
        var visibleAfter = alpha > 0;

        __state = playerObj.f32Alpha == 1 && alpha < 1;

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

                foreach (var renderer in Enumerable.Concat<Renderer>(obj.mRenderers, obj.smRenderers)) {
                    if (renderer != null && renderer.enabled) {
                        renderer.enabled = false;
                        memory.renderers.Add(renderer);
                    }
                }
            }
        }
    }

    /*
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.gYm_OujiSetAlpha))]
    public static void UpdateKatamariOpacity(GameManager __instance, float alpha, bool __state) {
        if (alpha == 0 || alpha == 1) return;

        Console.WriteLine($"hi {alpha}");

        foreach (var obj in GlobalWork.Instance.listProp) {
            if (obj == null || !obj.IsAttachedToKatamari) continue;

            obj.UpdateMono(true, true, alpha, 0);
        }
    }
    */
}

public sealed class TransparencyFixState : MonoBehaviour {
    public Material[] opaqueMaterials = [];
}

public sealed class MemoryAlpha : MonoBehaviour {
    public List<Renderer> renderers = [];
}