using HarmonyLib;

using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace SingleplayerCousins.Cousins;

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
            billboard.SetActive(false);
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

        var billboard = Object.Instantiate(Prefabs.billboard, ouji.transform);
        billboard.SetActive(true);
        var material = new Material(Prefabs.material);

        billboard.name = "JungleBoardEnding";
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

        billboard.transform.localScale = Vector3.one * 2.5f;
        billboard.transform.localPosition = new Vector3(0f, 0.55f);

        if (ouji.transform.root.gameObject.name is "EarchCharacter" or "GO_ouji_motion") {
            // This position happens to be a bit nicer for the sideways flying animation.
            billboard.transform.localPosition = new Vector3(0f, 0.45f);
        }

        if (sceneName is "UI_Star" or "Select") {
            faceCamera.target = Camera.allCameras.FirstWithName("GO_uiMonoCamera");
        }
    }

    // The vanilla code only does this during Update, which I think is too early under some circumstances.
    // This component will fix that for the main player game object, as well as its main job of doing it "at all" for other instances.
    public sealed class FaceCamera : MonoBehaviour {
        public Camera? target = null;

        public void LateUpdate() {
            var target = this.target ?? Camera.main;
            if (target != null) {
                if (this.gameObject.scene.name == "UI_HUD") {
                    this.transform.LookAt(this.transform.position with { z = -target.transform.position.z });
                } else {
                    this.transform.LookAt(target.transform);
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static void FixEarlyBillboardUpdate(Player __instance) {
        var billboard = __instance.objBillboard;
        if (billboard == null) return;

        var faceCamera = billboard.GetComponent<FaceCamera>() ?? billboard.AddComponent<FaceCamera>();
        faceCamera.target = __instance.gWork.camGame[__instance.playerNo];
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.Start))]
    [HarmonyPatch(typeof(StarSky), nameof(StarSky.Start))]
    [HarmonyPatch(typeof(MoonMovieSelector), nameof(MoonMovieSelector.Start), MethodType.Enumerator)]
    public static IL FixJungleInLecture(IL il) {
        var newRenderTexture = Member.Constructor(() => new RenderTexture(1920, 1080, 24));

        return new CodeMatcher(il)
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
    public static void FixJungleInHud(HUDManager __instance) {
        // HUDManager already does this, but only as part of a versus-only block.
        for (var i = 0; i <= 1; i++) {
            if (__instance.gWork.player[i] == null) continue;
            if (__instance.transJungleBoard[i] != null) continue;
            __instance.transJungleBoard[i] = __instance.mCharacterClone[i].transform.Find("JungleBoard(Clone)");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.CloneItoko))]
    public static void TweakVanillaBillboardPosition(KinokoRotator __instance) {
        if (__instance.objBillboard == null) return;
        __instance.objBillboard.transform.localScale = Vector3.one * 2.5f;
        __instance.objBillboard.transform.localPosition = Vector3.up * 0.55f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MonoOnlyCamera), nameof(MonoOnlyCamera.Update))]
    public static void TweakVanillaBillboardPosition(MonoOnlyCamera __instance) {
        if (__instance.objBillboard is not GameObject billboard) return;
        var panel = billboard.transform.GetChild(0);
        panel.localScale = Vector3.one * 1.65f;
        panel.localPosition = Vector3.up * 0.55f;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(KinokoItokoSelector), nameof(KinokoItokoSelector.Update))]
    public static IL TweakVanillaBillboardRotation(IL il) {
        MemberInfo
            objBillboard = Member.Field<KinokoItokoSelector>(sel => sel.objBillboard),
            getObjectTransform = Member.Getter<GameObject>(o => o.transform),
            getComponentTransform = Member.Getter<Component>(c => c.transform),
            getZero = Member.Getter(() => Vector3.zero),
            euler = Member.Method(() => Quaternion.Euler(Vector3.zero)),
            setRotation = Member.Setter<Transform>(t => t.rotation),
            cameraPlayer = Member.Field<KinokoItokoSelector>(sel => sel._cameraPlayer),
            lookAt = Member.Method<Transform>(t => t.LookAt(t));

        return new CodeMatcher(il)
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

    // This is a big improvement, but for some reason, the transition from the title screen to the home planet still has a jump,
    // because the billboard is positioned too high up during that animation? Like there's some kind of additional offset.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartsMover), nameof(StartsMover.LandOujiStar))]
    public static IEnumerator EaseBillboard(IEnumerator __result, StartsMover __instance) {
        var anim = __instance._animator_ouji;
        if (Plugin.OujiId != (int)Cousin.Jungle || anim.transform.Find("GO_defaultPosition/OUJI23/JungleBoardEnding") is not Transform billboard) {
            while (__result.MoveNext()) {
                yield return __result.Current;
            }
            yield break;
        }

        while (__result.MoveNext()) {
            yield return __result.Current;
            var time = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (time == 0) continue;
            var t = Mathf.Clamp01((time - 1.05f) / 0.1f);
            var y = Mathf.Lerp(a: 0.45f, b: 0.55f, t: t);
            billboard.transform.localPosition = Vector3.up * y;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartsMover), nameof(StartsMover.LandKinoko))]
    [HarmonyPatch(typeof(StartsMover), nameof(StartsMover.LandEarch))]
    [HarmonyPatch(typeof(StartsMover), nameof(StartsMover.LandOujiStar))]
    public static void ResetBillboard(StartsMover __instance) {
        var anim = __instance._animator_ouji;
        if (Plugin.OujiId != (int)Cousin.Jungle || anim.transform.Find("GO_defaultPosition/OUJI23/JungleBoardEnding") is not Transform billboard) {
            return;
        }

        billboard.transform.localPosition = Vector3.up * 0.45f;
    }
}
