using BepInEx;

using HarmonyLib;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace KatamariDama60;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public void Awake() {
        Harmony.CreateAndPatchAll(typeof(Plugin));
        Harmony.CreateAndPatchAll(typeof(SkipIntro));
        if (Environment.CommandLine.Contains("--skip-steam")) {
            Harmony.CreateAndPatchAll(typeof(SkipSteam));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.IsSelectingVS))]
    public static bool DontDisableRotation(ref bool __result) {
        __result = false;
        return false;
    }

    private static int n = 2;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), "Start")]
    public static void Awake(Player __instance) {
        //__instance.oujiNo = GlobalWork.Instance.selectCharacter[__instance.playerNo];
        __instance.oujiNo = n;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OujiStarCharacter), "OnEnable")]
    public static void Qux(OujiStarCharacter __instance) {
        n++;
        if (n > 24) n = 2;

        var oujiName = $"OUJI{n:D2}";
        var prefab = AssetBundleSimulator.Instance.LoadAsset<GameObject>(oujiName, oujiName);

        var ouji = Instantiate(prefab);
        ouji.SetActive(false);
        ouji.name = oujiName;

        var old = __instance._animator_ouji.gameObject;

        ouji.transform.SetParent(old.transform.parent, false);
        ouji.transform.localRotation = old.transform.localRotation;
        ouji.transform.localPosition = old.transform.localPosition;

        var presentRoot = old.transform.Find("pre_root").gameObject;
        TransferPresents(ouji, presentRoot);
        ouji.AddComponent<UIOujiWear>()._go_oujiPresentParent = presentRoot;

        var animator = ouji.GetComponent<Animator>();
        var oldAnimator = old.GetComponent<Animator>();
        animator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;
        __instance._animator_ouji = animator;
        __instance._oujiStarRotator._animator_ouji = animator;
        __instance._starMover._tran_oujiStarLandingPosition = ouji.transform;

        Destroy(old);
        ouji.SetActive(true);
    }

    private static void TransferPresents(GameObject ouji, GameObject presentRoot) {
        presentRoot.transform.SetParent(ouji.transform, false);

        var bones = new Dictionary<string, GameObject>();
        foreach (var bone in ouji.GetComponentsInChildren<Transform>(true)) {
            bones[bone.name] = bone.gameObject;
        }

        foreach (var renderer in presentRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            renderer.rootBone = bones[renderer.rootBone.name].transform;
            // For some reason, this seems to only work properly if I replace the entire array.
            var newBones = new Transform[renderer.bones.Length];
            for (var i = 0; i < renderer.bones.Length; i++) {
                newBones[i] = bones[renderer.bones[i].name].transform;
            }
            renderer.bones = newBones;
        }
    }
}