using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace KatamariDama60;

public static class Jungle {
    private static class Prefabs {
        public static GameObject billboard = null!;
        public static Material material = null!;
    }

    internal static IEnumerator Init() {
        const string sceneName = "UI_MainMenu";

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        //load.allowSceneActivation = false;
        yield return load;

        var scene = SceneManager.GetSceneByName(sceneName);

        KinokoRotator? kinoko = null;
        foreach (var obj in scene.GetRootGameObjects()) {
            if (obj.name == "go_kinokoRotator") {
                kinoko = obj.GetComponent<KinokoRotator>();
            }
        }
        if (kinoko is null) yield break;

        Prefabs.billboard = Object.Instantiate(kinoko.jungleBoard);
        Prefabs.billboard.name = "JungleBillboardPrefab";
        Prefabs.material = Object.Instantiate(kinoko.matJungle);
        Prefabs.material.name = "JungleMaterialPrefab";
        Object.DontDestroyOnLoad(Prefabs.billboard);
        Object.DontDestroyOnLoad(Prefabs.material);

        yield return SceneManager.UnloadSceneAsync(sceneName);
    }

    public static void Dress(GameObject ouji) {
        var sceneName = ouji.scene.name;
        Console.WriteLine($"Trying to dress {ouji.name}, child of {ouji.transform.root.gameObject.name}, in scene {sceneName}");

        if (Prefabs.billboard == null) {
            Console.WriteLine("Oh no, billboard is null");
            return;
        } else if (Prefabs.material == null) {
            Console.WriteLine("Oh no, material is null");
            return;
        }

        var billboard = Object.Instantiate(Prefabs.billboard);
        var material = new Material(Prefabs.material);

        billboard.name = "JungleBoardEnding";
        billboard.transform.SetParent(ouji.transform, worldPositionStays: false);
        billboard.layer = ouji.layer;
        billboard.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");

        var hackPending = new List<GameObject>();

        foreach (var renderer in ouji.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            if (renderer?.name is "head_tawara_m" or "body01_m" or "hand_m") {
                if (sceneName == "Result2") {
                    hackPending.Add(renderer.gameObject);
                } else {
                    renderer.material = material;
                }
            }
        }

        billboard.transform.localScale = Vector3.one * 5;
        var faceCamera = billboard.AddComponent<FaceCamera>();

        if (sceneName == "Result2") {
            // It would be nice to find a better way to do this, but as long as it works, honestly whatever.
            // At least this was easier to figure out than the wrong-pixel-format thing.
            foreach (var obj in hackPending) {
                var copy = Object.Instantiate(obj);
                copy.name = obj.name;
                obj.name += " (Jungle Depth Buffer Hack)";
                copy.transform.parent = obj.transform.parent;
                copy.GetComponent<SkinnedMeshRenderer>().material = material;
            }
        }

        // Are there any scenes where this is *not* desirable?
        // TBD, but for everything I've checked it seems close to ideal
        billboard.transform.localScale = Vector3.one * 2.5f;
        billboard.transform.localPosition = new Vector3(0f, 0.55f);

        if (ouji.transform.root.gameObject.name is "EarchCharacter" or "GO_ouji_motion") {
            // This results in a slight "jump" when landing on the home planet,
            // but I think this looks better than using the same transform in both cases.
            // Takeoff from the home planet is already not smoothly animated anyway.
            // Ideally it would do some interpolation during the landing animation,
            // but I'm not ready to implement that just yet.
            billboard.transform.localPosition = new Vector3(0f, 0.45f);
        }

        if (sceneName is "UI_Star" or "Select") {
            faceCamera.target = Camera.allCameras.First(c => c.name == "GO_uiMonoCamera");
        }
    }

    public sealed class FaceCamera : MonoBehaviour {
        public Camera? target = null;

        public void Update() {
            var target = this.target ?? Camera.main;
            if (target != null) {
                this.transform.LookAt(target.transform);
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.Start))]
    [HarmonyPatch(typeof(StarSky), nameof(StarSky.Start))]
    public static IEnumerable<CodeInstruction> FixJungleInLecture(IEnumerable<CodeInstruction> instructions) {
        var newRenderTexture = AccessTools.Constructor(typeof(RenderTexture), [typeof(int), typeof(int), typeof(int)]);

        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4),
                          new(OpCodes.Ldc_I4),
                          new(OpCodes.Ldc_I4_S, (sbyte)16),
                          new(OpCodes.Newobj, newRenderTexture))
            .Advance(2)
            .SetOperandAndAdvance((sbyte)24)
            .Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SetupCharacterElement))]
    public static void FixJungleInHudPart1(HUDManager __instance) {
        // HUDManager already does this, but only as part of a versus-only block.
        for (var i = 0; i <= 1; i++) {
            if (__instance.gWork.player[i] == null) continue;
            if (__instance.transJungleBoard[i] != null) continue;
            __instance.transJungleBoard[i] = __instance.mCharacterClone[i].transform.Find("JungleBoard(Clone)");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    public static void FixJungleInHudPart2(HUDManager __instance) {
        // HUDManager already does this, but only as part of a versus-only block.
        foreach (var billboard in __instance.transJungleBoard) {
            if (billboard == null) continue;
            var z = __instance.HUDCamera.transform.position.z;
            billboard.LookAt(billboard.position with { z = z });
        }
    }
}
