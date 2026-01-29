using HarmonyLib;

using UnityEngine;

using UnityEngine.PostProcessing;

namespace DevUtils;

static class DepthOfField {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.SetActiveStageObject))]
    public static void EnableDof2() {
        var gw = GlobalWork.Instance;
        if (gw.u8GameMode != DefineEnum.GI_GMODE.GI_GMODE_ENDING) {
            foreach (var cam in gw.camGame) {
                if (cam == null) continue;

                var ppb = cam.GetComponent<PostProcessingBehaviour>();
                ppb.profile.depthOfField.enabled = QualitySetting.Instance.IsDOF;
            }
        }
    }

    private static Camera depthCamera = null!;
    private static RenderTexture depthCamColor = null!, depthCamDepth = null!;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DepthOfFieldComponent), nameof(DepthOfFieldComponent.Prepare))]
    public static void OnDof(DepthOfFieldComponent __instance) {
        var ctx = __instance.context;

        if (depthCamera == null) {
            depthCamera = new GameObject().AddComponent<Camera>();
            var desc = ctx.camera.targetTexture.descriptor;

            desc.colorFormat = RenderTextureFormat.R8;
            desc.depthBufferBits = 0;
            depthCamColor = new RenderTexture(desc) { name = "Depth Pass - Color" };

            desc.colorFormat = RenderTextureFormat.Depth;
            desc.depthBufferBits = 16;
            depthCamDepth = new RenderTexture(desc) { name = "Depth Pass - Depth" };

            depthCamera.SetTargetBuffers(depthCamColor.colorBuffer, depthCamDepth.depthBuffer);
        }

        var depthCam = depthCamera.GetComponent<Camera>();
        depthCam.enabled = false;

        var blitToDepth = ctx.materialFactory.Get("Hidden/BlitToDepth");

        depthCam.CopyFrom(ctx.camera);
        depthCam.SetTargetBuffers(depthCamColor.colorBuffer, depthCamDepth.depthBuffer);
        depthCam.Render();

        RenderTexture.active = Shader.GetGlobalTexture("_CameraDepthTexture") as RenderTexture;
        blitToDepth.SetPass(0);
        blitToDepth.SetTexture("_MainTex", depthCamDepth);
        DrawQuad();
    }

    private static void DrawQuad() {
        GL.LoadOrtho();

        GL.Begin(GL.QUADS);

        GL.TexCoord2(0, 0);
        GL.Vertex3(0, 0, 0);

        GL.TexCoord2(1, 0);
        GL.Vertex3(1, 0, 0);

        GL.TexCoord2(1, 1);
        GL.Vertex3(1, 1, 0);

        GL.TexCoord2(0, 1);
        GL.Vertex3(0, 1, 0);

        GL.End();
    }
}
