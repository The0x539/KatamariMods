using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using MonoMod.Utils;

using SingleplayerCousins.Cousins;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SingleplayerCousins;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    public static ConfigFile configFile = null!;

    public void Awake() {
        configFile = this.Config;
        this.StartCoroutine(Jungle.LoadPrefabs());

        Harmony.CreateAndPatchAll(this.GetType());
        Harmony.CreateAndPatchAll(typeof(Jungle));
        Harmony.CreateAndPatchAll(typeof(Njamo));
        Harmony.CreateAndPatchAll(typeof(PretenderPatches));

        SceneManager.sceneLoaded += (scene, mode) => {
            if (scene.name is "Result2" or "UI_Moon") {
                ReplaceOuji(GameObject.Find("OUJI01"), OujiId, o => { });
            }

            if (scene.name == "UI_Moon") {
                var cam = FindObjectOfType<UIMonoCamera>();
                ScaleCamera(cam);
                if ((Cousin)OujiId is Cousin.Odeko or Cousin.Fujio) {
                    cam.fvMinMax = -150f;
                    cam.radius = 17;
                }
            }
        };
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.IsSelectingVS))]
    public static bool DontDisableRotation(ref bool __result) {
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIKinoko), nameof(UIKinoko.SetEnableLR))]
    public static void DontDisableRotationVisually(ref bool value) {
        value = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KinokoItokoSelector), nameof(KinokoItokoSelector.Update))]
    public static void OnSelectOuji(KinokoItokoSelector __instance) {
        if (!__instance.IsOnePlayer) return;
        var selection = UIKinoko.selectCharacter1Player;
        if (selection == 0) return;

        if (OujiId == selection) return;
        OujiId = selection;
        var sm = FindObjectOfType<StartsMover>();
        ReplaceInMenu(sm);
        ReplaceInHomePlanetMenus(sm._oujiStarCharacter);
    }

    // One of many unused spots in the save file.
    internal static ref int OujiId => ref GlobalWork.Instance.siMission[43].u32CatchRanking[1];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.SetSaveData))]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Load))]
    public static void InitializeOujiId() {
        if (OujiId < 1) {
            OujiId = 1;
        } else if (OujiId > 24 && !Pretender.pretenders.ContainsKey(OujiId)) {
            OujiId = 1;
        }

        if (FindObjectOfType<StartsMover>() is StartsMover sm) {
            ReplaceInMenu(sm);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static void ReplaceInGameplay(Player __instance) {
        // TODO: don't actually override versus mode?
        // Or does this running as a prefix get overruled by that anyway?
        __instance.oujiNo = OujiId;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static void GivePresents(Player __instance) {
        if (OujiId == 01) return;
        if (__instance.gWork.u8GameInfoMode == DefineEnum.GAMEINFO_MODE.GAMEINFO_MODE_VS) return;

        var thePrincePrefab = AssetBundleSimulator.Instance.LoadAsset<GameObject>("ouji01", "ouji01");
        var thePrince = Instantiate(thePrincePrefab);
        var preRoot = thePrince.transform.Find("pre_root").gameObject;

        __instance.objPresent = preRoot.GetChildren();

        preRoot.SetActive(true);
        foreach (var present in __instance.objPresent) present.SetActive(false);

        TransferPresents(__instance.objOuji, preRoot);
        preRoot.SetLayer(__instance.objOuji.layer, true);

        __instance.objHUDPresent = new GameObject[__instance.objPresent.Length];
        Destroy(thePrince);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Title3Manager), nameof(Title3Manager.Start))]
    public static void ReplaceInTitle(Title3Manager __instance) {
        ReplaceOuji(__instance._animator_ouji, OujiId, o => {
            __instance._animator_ouji = o.animator;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartsMover), nameof(StartsMover.Awake))]
    public static void ReplaceInMenu(StartsMover __instance) {
        var sm = __instance;

        ReplaceOuji(sm._oujiStarCharacter._animator_ouji, OujiId, o => {
            sm._oujiStarCharacter._animator_ouji = o.animator;
            sm._oujiStarRotator._animator_ouji = o.animator;
            sm._tran_oujiStarLandingPosition = o.transform;
        });

        ReplaceOuji(sm._animator_oujiInner, OujiId, o => {
            sm._animator_oujiInner = o.animator;
        });

        ReplaceOuji(sm._earchRotator._animator_ouji, OujiId, o => {
            sm._earchRotator._animator_ouji = o.animator;
            sm._tran_earchLandingPosition = o.transform;
        });

        ReplaceOuji(sm._earchRotator._goToMoonAnimator._animator_ouji, OujiId, o => {
            sm._earchRotator._goToMoonAnimator._animator_ouji = o.animator;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OujiStarCharacter), nameof(OujiStarCharacter.EnableMainMenu))]
    public static void ReplaceInHomePlanetMenus(OujiStarCharacter __instance) {
        var osc = __instance;

        if (osc._uiMonoCamera is not UIMonoCamera camera) {
            Console.WriteLine("Ope!");
            return;
        }

        ReplaceOuji(camera._go_ouji, OujiId, o => {
            camera._go_ouji = o.ouji;
            var uiPresent = osc._uiOujiStarPresent;
            uiPresent._animator_ouji = o.animator;
            uiPresent._uiOujiWear = o.wear;
            uiPresent._go_oujiPresentParent = o.presents;
            ScaleCamera(camera);
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StarSky), nameof(StarSky.Start))]
    public static void ReplaceInConstellationView(StarSky __instance) {
        ReplaceOuji(__instance._animator_ouji, OujiId, o => {
            __instance._animator_ouji = o.animator;
            __instance._uiMonoCamera._go_ouji = o.ouji;
            ScaleCamera(__instance._uiMonoCamera);
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.Awake))]
    public static void ReplaceInLecture(SelectManager __instance) {
        ReplaceOuji(__instance._uiMonoCamera._go_ouji, OujiId, o => {
            __instance._uiMonoCamera._go_ouji = o.ouji;
            ScaleCamera(__instance._uiMonoCamera);
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameEndManager), nameof(GameEndManager.Start))]
    public static void ReplaceInFarewell(GameEndManager __instance) {
        ReplaceOuji(__instance._animator_ouji, OujiId, o => {
            __instance._animator_ouji = o.animator;
        });
    }

    public static void ScaleCamera(UIMonoCamera c) {
        if (originalCameraValues.TryGetValue(c.GetInstanceID(), out var original)) {
            c.fvMinMax = original[0];
            c.radius = original[1];
        }

        if (cameraFactors.TryGetValue((Cousin)OujiId, out var factors)) {
            originalCameraValues[c.GetInstanceID()] = [c.fvMinMax, c.radius];
            c.fvMinMax *= factors[0];
            c.radius *= factors[1];
        }
    }

    private static readonly Dictionary<int, float[]> originalCameraValues = new();
    private static readonly Dictionary<Cousin, float[]> cameraFactors = new() {
        [Cousin.Odeko] = [1.72f, 1.74f],
        [Cousin.Havana] = [1.15f, 1.41f],
        [Cousin.Fujio] = [1.3f, 1.3f],
        [Cousin.Foomin] = [1.2f, 1.2f],
    };

    private class OujiRefs(GameObject Obj) {
        public readonly GameObject ouji = Obj;
        public Transform transform = Obj.transform;
        public Animator animator = Obj.GetComponent<Animator>();
        public UIOujiWear wear = Obj.GetComponent<UIOujiWear>();
        public GameObject presents = Obj.GetComponent<UIOujiWear>()._go_oujiPresentParent;
    }

    private delegate void OujiCallback(OujiRefs o);

    private static void ReplaceOuji(Animator old, int idx, OujiCallback func) {
        ReplaceOuji(old.gameObject, idx, func);
    }

    private static void ReplaceOuji(GameObject old, int idx, OujiCallback func) {
        //Console.WriteLine($"old: {old} in {old.scene.name} on {old.layer} under {old.transform.parent?.name}");
        var oujiName = $"OUJI{idx:D2}";
        if (oujiName == old.name) return;

        GameObject prefab;

        var itoko = GlobalWork.Instance.objItoko;
        if (itoko != null && itoko.Length >= idx && itoko[idx - 1] != null) {
            prefab = itoko[idx - 1];
        } else if (Pretender.pretenders.TryGetValue(idx, out var pretender)) {
            prefab = pretender.Reify();
        } else {
            prefab = AssetBundleSimulator.Instance.LoadAsset<GameObject>(oujiName, oujiName);
        }

        if (prefab == null) {
            Console.WriteLine($"Could not load player model: {oujiName}");
            return;
        }

        var ouji = Instantiate(prefab);
        ouji.SetActive(false);
        ouji.name = oujiName;

        if (old.transform.parent != null) {
            ouji.transform.SetParent(old.transform.parent, worldPositionStays: false);
        } else {
            SceneManager.MoveGameObjectToScene(ouji, old.scene);
        }

        ouji.transform.localRotation = old.transform.localRotation;
        ouji.transform.localPosition = old.transform.localPosition;
        ouji.transform.localScale = old.transform.localScale;

        var presentRoot = old.transform.Find("pre_root").gameObject;
        TransferPresents(ouji, presentRoot);
        ouji.AddComponent<UIOujiWear>()._go_oujiPresentParent = presentRoot;

        var animator = ouji.GetComponent<Animator>();
        var oldAnimator = old.GetComponent<Animator>();
        animator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController;

        switch (idx) {
            case (int)Cousin.Jungle:
                Jungle.Dress(ouji);
                break;
            case (int)Cousin.Marny:
                ouji.AddComponent<Marny>();
                break;
            case (int)Cousin.Dipp:
                // This is exceptionally dumb.
                // Ideally I'd simply "merge" the animation controllers,
                // as the one that Dipp loads in with has his animation clip(s)
                // for animating the texture, but it seems like Unity makes that fundamentally impossible for some reason.
                ouji.AddComponent<Dipp>();
                break;
            case (int)PretenderId.Vanta:
                Vanta.Dress(ouji);
                break;
        }

        try {
            func(new OujiRefs(ouji));
        } catch (Exception e) {
            e.LogDetailed();
        }

        ouji.SetLayer(old.layer, true);

        ouji.SetActive(old.activeSelf);
        Destroy(old);
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
                var name = renderer.bones[i].name;


                if (!bones.ContainsKey(name)) {
                    if (name.StartsWith("PRE_")) {
                        name = renderer.bones[i].parent.name;
                    } else if (name == "JNT_antenna") {
                        // Fix for e.g. Shikao and Foomin, who have two antennae
                        name = "JNT_antenna_L";
                    } else if (name is "JNT_antenna_L" or "JNT_antenna_R") {
                        name = "JNT_antenna";
                    }
                }

                if (bones.TryGetValue(name, out var bone)) {
                    newBones[i] = bone.transform;
                } else {
                    Console.WriteLine($"Could not find bone: {name}");
                }
            }
            renderer.bones = newBones;

            if (renderer.gameObject.GetComponent<PresentAdjuster>() is PresentAdjuster existing) {
                existing.Setup();
            } else {
                renderer.gameObject.AddComponent<PresentAdjuster>().Setup();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static void EnableFaceExpressions(Player __instance) {
        __instance.objParts[0].transform.parent.gameObject.SetActive(true);
    }
}
