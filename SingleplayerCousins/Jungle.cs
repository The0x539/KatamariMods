using HarmonyLib;

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace SingleplayerCousins;

public static class Jungle {
    private static class Prefabs {
        public static GameObject billboard = null!;
        public static Material material = null!;

        internal static IEnumerator Init() {
            const string sceneName = "UI_Collection_Mono";
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            var scene = SceneManager.GetSceneByName(sceneName);

            var roots = scene.GetRootGameObjects();

            // Prevent the menu from briefly showing up on screen while the game is loading.
            // Ideally we could just directly get the two prefabs we actually care about,
            // but I have yet to figure out a working way to do that.
            foreach (var obj in roots) obj.gameObject.SetActive(false);

            var monoScene = roots.FirstWithName("GO_MonoScene")?.GetComponent<MonoScene>();
            if (monoScene is null) yield break;

            billboard = Object.Instantiate(monoScene.jungleBoard);
            billboard.name = "JungleBillboardPrefab";
            material = Object.Instantiate(monoScene.matJungle);
            material.name = "JungleMaterialPrefab";
            Object.DontDestroyOnLoad(billboard);
            Object.DontDestroyOnLoad(material);

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    // For some reason, BepInEx freaks out if it sees a coroutine in a class it's scanning for patches,
    // so as a workaround, stick the actual yielding method elsewhere.
    internal static IEnumerator LoadPrefabs() => Prefabs.Init();

    public static void Dress(GameObject ouji) {
        if (Prefabs.billboard == null || Prefabs.material == null) return;

        var billboard = Object.Instantiate(Prefabs.billboard);
        var material = new Material(Prefabs.material);

        billboard.name = "JungleBoardEnding";
        billboard.transform.SetParent(ouji.transform, worldPositionStays: false);
        billboard.layer = ouji.layer;
        billboard.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");

        var bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
            .AllWithNames("head_tawara_m", "body01_m", "hand_m");

        var sceneName = ouji.scene.name;
        if (sceneName == "Result2") {
            // On the results screen, there's a weird issue where the Jungle material setup
            // doesn't write to the depth buffer, so it fails to overdraw the background.
            // One way to fix this is to have two copies of the affected body parts:
            // one with Jungle's special material, one with the standard material.
            // The standard one comes first and is responsible for writing to the depth buffer.
            //
            // It would be nice to find a better way to do this, but as long as it works, honestly whatever.
            // At least this was easier to figure out than the wrong-pixel-format thing.
            foreach (var renderer in bodyParts) {
                var standard = renderer.gameObject;
                var special = Object.Instantiate(standard);

                special.name = standard.name;
                renderer.name += " (Jungle Depth Buffer Hack)";
                special.transform.parent = renderer.transform.parent;
                special.GetComponent<SkinnedMeshRenderer>().material = material;
            }
        } else {
            foreach (var renderer in bodyParts) {
                renderer.material = material;
            }
        }

        billboard.transform.localScale = Vector3.one * 5;
        var faceCamera = billboard.AddComponent<FaceCamera>();

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
            faceCamera.target = Camera.allCameras.FirstWithName("GO_uiMonoCamera");
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
    [HarmonyPatch(typeof(MoonMovieSelector), nameof(MoonMovieSelector.Start), MethodType.Enumerator)]
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.CloneItoko))]
    public static void TweakVanillaBillboardPosition(KinokoRotator __instance) {
        if (__instance.objBillboard == null) return;
        __instance.objBillboard.transform.localScale = Vector3.one * 2.5f;
        __instance.objBillboard.transform.localPosition = Vector3.up * 0.55f;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(KinokoItokoSelector), nameof(KinokoItokoSelector.Update))]
    public static IEnumerable<CodeInstruction> TweakVanillaBillboardRotation(IEnumerable<CodeInstruction> instructions) {
        MemberInfo
            objBillboard = AccessTools.Field(typeof(KinokoItokoSelector), nameof(KinokoItokoSelector.objBillboard)),
            getObjectTransform = AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.transform)),
            getComponentTransform = AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform)),
            getZero = AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.zero)),
            euler = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Euler), [typeof(Vector3)]),
            setRotation = AccessTools.PropertySetter(typeof(Transform), nameof(Transform.rotation)),
            cameraPlayer = AccessTools.Field(typeof(KinokoItokoSelector), nameof(KinokoItokoSelector._cameraPlayer)),
            lookAt = AccessTools.Method(typeof(Transform), nameof(Transform.LookAt), [typeof(Transform)]);

        return new CodeMatcher(instructions)
            // Find: this.objBillboard.transform.rotation = Quaternion.Euler(Vector3.zero);
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, objBillboard),
                          new(OpCodes.Callvirt, getObjectTransform),
                          new(OpCodes.Call, getZero),
                          new(OpCodes.Call, euler),
                          new(OpCodes.Callvirt, setRotation))
            // Keep: this.objBillboard.transform
            .Advance(3)
            // Remove: .rotation = Quaternion.Euler(Vector3.zero);
            .RemoveInstructions(3)
            // Insert: .LookAt(this._cameraPlayer.transform);
            .InsertAndAdvance(new(OpCodes.Ldarg_0),
                              new(OpCodes.Ldfld, cameraPlayer),
                              new(OpCodes.Call, getComponentTransform),
                              new(OpCodes.Call, lookAt))
            .Instructions();
    }
}
