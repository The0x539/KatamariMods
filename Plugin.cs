using BepInEx;

using HarmonyLib;

using MonoMod.Utils;

using MyGame;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace KatamariDama60;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BaseUnityPlugin {
    private static Plugin self = null!;

    public void Awake() {
        self = this;

        this.StartCoroutine(Jungle.Init());

        // I would like to just do the usual thing here,
        // but that causes Harmony to see Jungle.Init,
        // which for some reason makes it panic due to IteratorStateMachineAttribute.
        var h = new Harmony(MyPluginInfo.PLUGIN_NAME);
        h.PatchAll(this.GetType());
        h.PatchAll(typeof(SkipIntro));

        SceneManager.sceneLoaded += (scene, mode) => {
            if (scene.name == "Result2") {
                ReplaceOuji(GameObject.Find("OUJI01"), OujiId, o => { });
            }

            if (mode == LoadSceneMode.Single) {
                Console.WriteLine($"Loaded {scene.name} singly");
            } else {
                Console.WriteLine($"Loaded {scene.name} additively");
            }
        };

        SceneManager.activeSceneChanged += (current, next) => {
            Console.WriteLine($"Scene change: {current.name} -> {next.name}");
        };

        SceneManager.sceneUnloaded += (scene) => {
            Console.WriteLine($"Unloaded {scene.name}");
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
    private static ref int OujiId => ref GlobalWork.Instance.siMission[43].u32CatchRanking[1];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.SetSaveData))]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Load))]
    public static void InitializeOujiId() {
        if (OujiId < 1 || OujiId > 24) {
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
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(OujiStarCharacter), nameof(OujiStarCharacter.EnableMainMenu))]
    public static void ReplaceInHomePlanetMenus(OujiStarCharacter __instance) {
        var osc = __instance;
        ReplaceOuji(osc._uiMonoCamera._go_ouji, OujiId, o => {
            osc._uiMonoCamera._go_ouji = o.ouji;
            var uiPresent = osc._uiOujiStarPresent;
            uiPresent._animator_ouji = o.animator;
            uiPresent._uiOujiWear = o.wear;
            uiPresent._go_oujiPresentParent = o.presents;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StarSky), nameof(StarSky.Start))]
    public static void ReplaceInConstellationView(StarSky __instance) {
        ReplaceOuji(__instance._animator_ouji, OujiId, o => {
            __instance._animator_ouji = o.animator;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.Awake))]
    public static void ReplaceInLecture(SelectManager __instance) {
        ReplaceOuji(__instance._uiMonoCamera._go_ouji, OujiId, o => {
            __instance._uiMonoCamera._go_ouji = o.ouji;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameEndManager), nameof(GameEndManager.Start))]
    public static void ReplaceInFarewell(GameEndManager __instance) {
        ReplaceOuji(__instance._animator_ouji, OujiId, o => {
            __instance._animator_ouji = o.animator;
        });
    }

    // TODO: this doesn't really belong in shipped builds, at least not without being a proper feature
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InputController), nameof(InputController.IsSelectDown))]
    public static void NoClick(ref bool isMouse) {
        isMouse = false;
    }

    private const int JUNGLE = 23;

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

        var prefab = AssetBundleSimulator.Instance.LoadAsset<GameObject>(oujiName, oujiName);
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

        if (idx == JUNGLE) {
            try {
                Jungle.Dress(ouji);
            } catch (Exception e) {
                e.LogDetailed();
            }
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
                if (bones.TryGetValue(name, out var bone)) {
                    newBones[i] = bone.transform;
                } else {
                    Console.WriteLine($"Could not find bone: {name}");
                }
            }
            renderer.bones = newBones;
        }
    }
}