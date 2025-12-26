using BepInEx;

using HarmonyLib;

using MyGame;

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

    private static int n = 6;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), "Start")]
    public static void Awake(Player __instance) {
        //__instance.oujiNo = GlobalWork.Instance.selectCharacter[__instance.playerNo];
        __instance.oujiNo = n;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Title3Manager), nameof(Title3Manager.Start))]
    public static void ReplaceInTitle(Title3Manager __instance) {
        var old = __instance._animator_ouji.gameObject;
        var ouji = ReplaceOuji(old, n);
        var animator = ouji.GetComponent<Animator>();
        __instance._animator_ouji = animator;
        ouji.SetActive(old.activeSelf);
        Destroy(old);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartsMover), nameof(StartsMover.Awake))]
    public static void ReplaceInMenu(StartsMover __instance) {
        GameObject old, ouji;
        Animator animator;

        old = __instance._oujiStarCharacter._animator_ouji.gameObject;
        ouji = ReplaceOuji(old, n);
        animator = ouji.GetComponent<Animator>();
        __instance._oujiStarCharacter._animator_ouji = animator;
        __instance._oujiStarRotator._animator_ouji = animator;
        __instance._tran_oujiStarLandingPosition = ouji.transform;
        ouji.SetActive(old.activeSelf);
        Destroy(old);

        old = __instance._animator_oujiInner.gameObject;
        ouji = ReplaceOuji(old, n);
        animator = ouji.GetComponent<Animator>();
        __instance._animator_oujiInner = animator;
        ouji.SetActive(old.activeSelf);
        Destroy(old);

        old = __instance._earchRotator._animator_ouji.gameObject;
        ouji = ReplaceOuji(old, n);
        animator = ouji.GetComponent<Animator>();
        __instance._tran_earchLandingPosition = ouji.transform;
        __instance._earchRotator._animator_ouji = animator;
        ouji.SetActive(old.activeSelf);
        Destroy(old);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.Awake))]
    public static void ReplaceInLecture(SelectManager __instance) {
        var old = __instance._uiMonoCamera._go_ouji;
        var ouji = ReplaceOuji(old, n);
        __instance._uiMonoCamera._go_ouji = ouji;
        ouji.SetActive(old.activeSelf);
        Destroy(old);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputController), nameof(InputController.IsSelectDown))]
    public static void NoClick(ref bool isMouse) {
        isMouse = false;
    }

    private static GameObject ReplaceOuji(GameObject old, int idx) {
        var oujiName = $"OUJI{idx:D2}";
        var prefab = AssetBundleSimulator.Instance.LoadAsset<GameObject>(oujiName, oujiName);

        var ouji = Instantiate(prefab);
        ouji.SetActive(false);
        ouji.name = oujiName;

        ouji.transform.SetParent(old.transform.parent, false);
        ouji.transform.localRotation = old.transform.localRotation;
        ouji.transform.localPosition = old.transform.localPosition;
        ouji.transform.localScale = old.transform.localScale;

        var presentRoot = old.transform.Find("pre_root").gameObject;
        TransferPresents(ouji, presentRoot);
        ouji.AddComponent<UIOujiWear>()._go_oujiPresentParent = presentRoot;

        var animator = ouji.GetComponent<Animator>();
        var oldAnimator = old.GetComponent<Animator>();
        animator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;

        return ouji;
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