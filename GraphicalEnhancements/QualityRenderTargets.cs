using HarmonyLib;

using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.UI;

namespace GraphicalEnhancements;

static class QualityRenderTargets {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.gTj_ResultWait))]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.GetTexture2DInner))]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    public static IL HiResKatamariPortrait(IL il, ILGenerator generator) {
        var getWidth = Member.Method(() => GetWidth());
        var getHeight = Member.Method(() => GetHeight());

        // We need to supersample it a bit because MSAA only applies to face edges, not along sharp lines in textures.
        // This game's art style has... a lot of sharp lines in textures.
        return new CodeMatcher(il, generator)
            .MatchForward(false,
                          new(OpCodes.Ldc_R4, 1920f),
                          new(OpCodes.Ldc_R4, 1080f))
            .Repeat(cm => cm
                .SetAndAdvance(OpCodes.Call, getWidth)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add),
                                  new(OpCodes.Conv_R4))
                .SetAndAdvance(OpCodes.Call, getHeight)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add),
                                  new(OpCodes.Conv_R4)))
            .Start()
            .MatchForward(false,
                          new(OpCodes.Ldc_I4, 1920),
                          new(OpCodes.Ldc_I4, 1080))
            .Repeat(cm => cm
                .SetAndAdvance(OpCodes.Call, getWidth)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add))
                .SetAndAdvance(OpCodes.Call, getHeight)
                .InsertAndAdvance(new(OpCodes.Dup),
                                  new(OpCodes.Add))
)
            .Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    public static void AntialiasKatamariResultImage(CameraKatamari __instance) {
        var old = CameraKatamari._rTexture;
        var descriptor = old.descriptor;
        var name = old.name;
        descriptor.msaaSamples = 4;
        var rt = new RenderTexture(descriptor) { name = name };
        __instance._camera.targetTexture = CameraKatamari._rTexture = rt;
        __instance._rImage?.texture = rt;
        old.Release();
    }


    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CameraKatamari), nameof(CameraKatamari.Setup))]
    [HarmonyPatch(typeof(MonoOnlyCamera), nameof(MonoOnlyCamera.SetTargetTexture))]
    [HarmonyPatch(typeof(UIFixedCamera), nameof(UIFixedCamera.Start))]
    [HarmonyPatch(typeof(UIMonoCamera), nameof(UIMonoCamera.SetTargetTextureDonotAddCamera))]
    public static IL MsaaEverywhere(IL il) {
        var setAllowMSAA = Member.Setter<Camera>(c => c.allowMSAA);

        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_0),
                          new(OpCodes.Callvirt, setAllowMSAA))
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_1)
            .Instructions();
    }

    private static int MsaaSamples => QualitySetting.antialiasingValue[QualitySetting.Instance.statusNo[4]];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Awake))]
    public static void MsaaPausesCameras(PauseMenu __instance) {
        __instance.objKatamariCamera.GetComponent<Camera>().allowMSAA = true;
        __instance.propCamera.allowMSAA = true;

        var descriptor = __instance.propCamera.targetTexture.descriptor with { msaaSamples = MsaaSamples };
        var rt = new RenderTexture(descriptor) { name = __instance.propCamera.targetTexture.name };
        __instance.propCamera.targetTexture = rt;
        __instance.objUIPause.transform.Find("ImageProp/CollectProp_2D").GetComponent<RawImage>().texture = rt;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDController), nameof(HUDController.Awake))]
    public static void MsaaHudCameras(HUDController __instance) {
        __instance.topCamera.allowMSAA = true;
        __instance.propCamera.allowMSAA = true;

        var descriptor = __instance.propCamera.targetTexture.descriptor with { msaaSamples = MsaaSamples };
        var rt = new RenderTexture(descriptor) { name = __instance.propCamera.targetTexture.name };
        __instance.propCamera.targetTexture = rt;

        var canvas = __instance.objUICanvas.transform;
        var overlayNames = new[] {
            "CollectedProp",
            "MsgArrow",
            "MsgArrow2",
            "MsgArrow_result"
        };
        foreach (var name in overlayNames) {
            if (canvas.Find(name) is not Transform hudElement) continue;
            var hudImage = hudElement.Find("CollectProp_2D") ?? hudElement.Find("CollectProp_2D (1)");
            if (hudImage?.GetComponent<RawImage>() is not RawImage img) continue;
            img.texture = rt;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MonoScene), nameof(MonoScene.Awake))]
    public static void HiResCollectionImages(MonoScene __instance) {
        var dim = 512 * GetHeight() / 1080;
        var descriptor = new RenderTextureDescriptor(dim, dim) { depthBufferBits = 24, msaaSamples = MsaaSamples };

        for (var i = 0; i < __instance.renderTexture.Length; i++) {
            __instance.renderTexture[i].Release();
            __instance.renderTexture[i] = new(descriptor);
            __instance.renderTexture[i].Create();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NameSelector), nameof(NameSelector.Start))]
    public static void HiResCollectionListImage(NameSelector __instance) {
        var dim = 512 * GetHeight() / 1080;
        var descriptor = new RenderTextureDescriptor(dim, dim) { depthBufferBits = 24, msaaSamples = MsaaSamples };
        var rt = new RenderTexture(descriptor);

        __instance.rt.Release();
        __instance.rt = rt;
        rt.Create();
        __instance._monoOnlyCamera.SetTargetTexture(rt);
        __instance._rImage_viewer.texture = rt;
    }

    private static int GetWidth() => Screen.currentResolution.width;
    private static int GetHeight() => Screen.currentResolution.height;
}