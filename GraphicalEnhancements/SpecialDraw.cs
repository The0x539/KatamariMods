using HarmonyLib;

using System.Collections.Generic;

using UnityEngine;

namespace GraphicalEnhancements;

public sealed class SpecialDraw : MonoBehaviour {
    public static readonly HashSet<SpecialDraw> instances = [];

    private AttachableProp prop = null!;
    public AttachableProp Prop => this.prop;

    private bool specialThisFrame = false;
    public bool SpecialThisFrame => this.specialThisFrame;

    public void Awake() {
        this.prop = this.GetComponent<AttachableProp>();
    }

    public void OnEnable() {
        instances.Add(this);
    }

    public void OnDisable() {
        instances.Remove(this);
    }

    public void HandlePreRender() {
        this.specialThisFrame = false;

        if (!this.gameObject.activeInHierarchy) return;
        var prop = this.prop;
        if (prop.IsAttachedToKatamari) return;
        if (prop.mRenderers == null) return;
        if (prop.mRenderers.Length == 0) return;

        var renderer = prop.mRenderers[0];
        if (!renderer.isVisible) return;

        renderer.enabled = false;
        this.specialThisFrame = true;
    }

    public void HandlePostRender() {
        if (!this.specialThisFrame) return;
        this.prop.mRenderers[0].enabled = true;
    }
}

public sealed class SpecialDrawActivator : MonoBehaviour {
    public void OnPreRender() {
        foreach (var obj in SpecialDraw.instances) {
            obj.HandlePreRender();
        }
    }

    public void OnPostRender() {
        foreach (var obj in SpecialDraw.instances) {
            obj.HandlePostRender();
        }
    }
}

public static class SpecialDrawPatches {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.MonoInitUnityPlacements))]
    public static void AddSpecialDrawComponent() {
        foreach (var prop in GlobalWork.Instance.listProp) {
            if (prop == null) continue;

            var matches = prop.u16MonoNameIdx
                is Define.MONO_IDX_CLOUD01_G
                or Define.MONO_IDX_CLOUD02_G
                // or Define.MONO_IDX_CLOUD03_G // TODO: What is this cloud? Must find ingame
                or Define.MONO_IDX_CLOUD04_G
                or Define.MONO_IDX_RAINBOW_G;

            if (matches && prop.gameObject.GetComponent<SpecialDraw>() == null) {
                prop.gameObject.AddComponent<SpecialDraw>();
            }
        }
    }

    private static readonly int smokeLayerId = LayerMask.NameToLayer("TransparentFX");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ParticleManager), nameof(ParticleManager.Start))]
    public static void SetSmokeLayer(ParticleManager __instance) {
        foreach (var fx in __instance.VisualFXMapping) {
            if (fx.VisualFXPrefab is not GameObject prefab) continue;

            if (prefab.GetComponent<ParticleSystemRenderer>()?.mesh?.name == "Ef_Smoke") {
                prefab.layer = smokeLayerId;
            }
        }

        foreach (var prop in GlobalWork.Instance.listProp) {
            if (prop == null) continue;

            if (prop.effectType == AttachableProp.eEffectType.Smoke) {
                prop.effectObject.layer = smokeLayerId;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    public static void AddSpecialDrawActivator(GameManager __instance) {
        if (__instance.gameObject.GetComponent<SpecialDrawActivator>() == null) {
            __instance.gameObject.AddComponent<SpecialDrawActivator>();
        }
    }
}