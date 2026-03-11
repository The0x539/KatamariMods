using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using UnityEngine;

using UnityEngine.PostProcessing;
using UnityEngine.Rendering;

namespace GraphicalEnhancements;

static class DepthOfField {
    // TODO: This probably isn't the place to put the updater component on the camera.
    // I need to learn more about the "life cycles" of some of these objects and components.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.SetActiveStageObject))]
    public static void EnableDof() {
        var gw = GlobalWork.Instance;
        if (gw.u8GameMode == DefineEnum.GI_GMODE.GI_GMODE_ENDING) return;

        for (var i = 0; i <= 1; i++) {
            var cam = gw.camGame[i];
            if (cam == null) continue;

            var ppb = cam.GetComponent<PostProcessingBehaviour>();
            ppb.profile.depthOfField.enabled = QualitySetting.Instance.IsDOF;
            // This should probably be a graphics option
            ppb.profile.depthOfField.settings = ppb.profile.depthOfField.settings with { kernelSize = DepthOfFieldModel.KernelSize.VeryLarge };

            var vanillaSSAO = ppb.profile.ambientOcclusion.settings;
            ppb.profile.ambientOcclusion.settings = vanillaSSAO with {
                // This game uses a rather distant far-clip-plane value due to how the gameplay works.
                // Doing this causes the ambient occlusion effect's first shader to experience some precision-related errors
                // if it tries to use the low-precision depth information from the DepthNormals texture.
                // This setting tells the SSAO effect to use the output of a dedicated depth pass instead of DepthNormals,
                // which the vanilla game didn't have working, but my patches get into a usable state.
                highPrecision = true,
                intensity = vanillaSSAO.intensity * 0.65f,
                radius = vanillaSSAO.radius * 1.1f,
                // This should maybe be configurable ingame.
                sampleCount = AmbientOcclusionModel.SampleCount.Low, // 6 "samples", as opposed to the default of Lowest = 3
            };

            if (cam.GetComponent<UpdateDof>() == null) {
                cam.gameObject.AddComponent<UpdateDof>();
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.PauseProc))]
    public static void FixPrinceLookDof(PauseMenu __instance) {
        var player = __instance.playerNo;
        var gWork = __instance.gWork;
        var dof = gWork.camGame[player]?.GetComponent<UpdateDof>();
        if (dof == null) return;

        // The idea of using "the distance between the camera and the katamari" works pretty well,
        // except for when the camera actually goes to the position of the katamari, i.e. first-person mode.
        //
        // When that happens, just use the distance from the moment the player entered first-person mode,
        // since that should work pretty well for the current first-person "session".
        if (gWork.oujiFaceMode[player] == 2) {
            dof.FreezeDistance();
        } else {
            dof.ThawDistance();
        }
    }

    private static Camera depthCamera = null!;
    private static Camera smokeCamera = null!;

    // TODO: Split the code that's not really DoF related into another guy
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PostProcessingBehaviour), nameof(PostProcessingBehaviour.OnPreRender))]
    public static IL TakeControlOfSSAO(IL il) {
        // Normally, the ambient occlusion effect schedules its stuff to run during CameraEvent.BeforeImageEffectsOpaque.
        // However, it's VERY difficult to get non–command buffer–flavored code to run immediately before this,
        // and command buffer code can't really "draw the whole world" the way we need to.
        // However, we're fortunate in that everything in this game's world uses a basic opaque material.
        // This means that there's nothing that runs in between that step and the OnRenderImage "camera message".
        // As such, we can just surgically remove the normal "TryExecuteCommandBuffer" invocation here,
        // and instead manually execute the command buffer "right away" after we do our depth-pass fix hack thing.
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, Member.Field<PostProcessingBehaviour>(ppb => ppb.m_AmbientOcclusion)),
                          new(OpCodes.Call))
            .RemoveInstructions(4)
            .Instructions();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PostProcessingBehaviour), nameof(PostProcessingBehaviour.OnRenderImage))]
    public static void ManualDepthPass(PostProcessingBehaviour __instance, RenderTexture source, RenderTexture destination) {
        if (!__instance.m_AmbientOcclusion.active && !__instance.m_DepthOfField.active) return;

        var ctx = __instance.m_Context;

        if (depthCamera == null) {
            if (ctx.camera.targetTexture == null) return;
            depthCamera = new GameObject("Manual Depth Pass").AddComponent<Camera>();
            depthCamera.enabled = false;
        }

        var renderersToSkip = new List<Renderer>();

        if (GlobalWork.Instance.player[0].f32Alpha == 0) {
            // This might be slightly expensive but should only happen during the end-of-level Royal Rainbow animation, not during gameplay.
            foreach (var prop in GlobalWork.instance.listProp) {
                if (prop == null) continue;
                if (!prop.mIsAttachedToKatamari) continue;

                // For reasons that remain unknown to me, disabling the renderer only in a gYm_OujiSetAlpha prefix
                // doesn't fully work, and leads to the objects here drawing to the depth buffer. Frustrating!
                foreach (var r in prop.mRenderers) {
                    if (r.enabled) renderersToSkip.Add(r);
                }
                foreach (var r in prop.smRenderers) {
                    if (r.enabled) renderersToSkip.Add(r);
                }
            }
        }
        foreach (var obj in SpecialDraw.instances) {
            if (obj.SpecialThisFrame) {
                renderersToSkip.Add(obj.Prop.mRenderers[0]);
            }
        }

        var manualColorTexture = ctx.renderTextureFactory.Get(ctx.width, ctx.height, depthBuffer: 0, RenderTextureFormat.R8, name: "Manual Depth Pass - Color");
        var manualDepthTexture = ctx.renderTextureFactory.Get(ctx.width, ctx.height, depthBuffer: 24, RenderTextureFormat.Depth, name: "Manual Depth Pass - Depth");

        depthCamera.CopyFrom(ctx.camera);
        depthCamera.SetTargetBuffers(manualColorTexture.colorBuffer, manualDepthTexture.depthBuffer);

        foreach (var r in renderersToSkip) r.enabled = false;
        depthCamera.Render();
        foreach (var r in renderersToSkip) r.enabled = true;

        var trueDepthTexture = (RenderTexture)Shader.GetGlobalTexture("_CameraDepthTexture");
        RenderTexture.active = trueDepthTexture;
        var blitToDepth = ctx.materialFactory.Get("Hidden/BlitToDepth");
        blitToDepth.SetPass(0);
        blitToDepth.SetTexture("_MainTex", manualDepthTexture);
        DrawQuad();

        ctx.renderTextureFactory.Release(manualColorTexture);
        ctx.renderTextureFactory.Release(manualDepthTexture);

        if (__instance.m_AmbientOcclusion.active) {
            var cb = new CommandBuffer();
            __instance.m_AmbientOcclusion.PopulateCommandBuffer(cb);
            Graphics.ExecuteCommandBuffer(cb);
        }

        GL.SetViewMatrix(ctx.camera.worldToCameraMatrix);
        GL.LoadProjectionMatrix(ctx.camera.projectionMatrix);

        // Now that SSAO is done, it's safe to draw the clouds to the depth buffer so that DoF blurs them correctly.
        RenderTexture.active = trueDepthTexture;
        RedrawClouds();

        // TODO: Similarly, manually draw the "dust/smoke" particles spawned when you roll and from smokestacks

        RenderTexture.active = source;
        RedrawJungle();
        RedrawClouds();
        RedrawSmoke(ctx, source);
    }

    private static void RedrawJungle() {
        var player = GlobalWork.Instance.player[0];
        if (player.oujiNo != 23 || !player.objOuji.activeInHierarchy) return;

        var mesh = new Mesh();
        foreach (var name in new[] { "head_tawara_m", "body01_m", "hand_m" }) {
            var bodyPart = player.objOuji.transform.Find("body_root/" + name).GetComponent<SkinnedMeshRenderer>();
            bodyPart.material.SetPass(0);
            bodyPart.BakeMesh(mesh);
            Graphics.DrawMeshNow(mesh, bodyPart.transform.position, bodyPart.transform.rotation);
        }

        var billboard = player.objBillboard.transform.GetChild(0);
        billboard.GetComponent<MeshRenderer>().material.SetPass(0);
        Graphics.DrawMeshNow(billboard.GetComponent<MeshFilter>().sharedMesh, billboard.localToWorldMatrix);
    }

    private static void RedrawClouds() {
        foreach (var obj in SpecialDraw.instances) {
            if (!obj.SpecialThisFrame) continue;

            var renderer = obj.Prop.mRenderers[0];
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            renderer.material.SetPass(0);
            Graphics.DrawMeshNow(mesh, obj.transform.localToWorldMatrix);
        }
    }

    private static readonly int smokeLayerMask = LayerMask.GetMask("TransparentFX");

    private static void RedrawSmoke(PostProcessingContext ctx, RenderTexture target) {
        if (smokeCamera == null) {
            smokeCamera = new GameObject("Smoke Camera").AddComponent<Camera>();
            smokeCamera.enabled = false;
        }

        smokeCamera.CopyFrom(ctx.camera);
        smokeCamera.clearFlags = CameraClearFlags.Nothing;
        smokeCamera.cullingMask = smokeLayerMask;
        smokeCamera.targetTexture = target;
        smokeCamera.Render();
    }

    // I don't quite understand why patching SetShaderSimple to not set the shader
    // results in things being transparent "too often".
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(AttachableProp), nameof(AttachableProp.UpdateMono))]
    public static IL NoSimpleShader(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_1),
                          new(OpCodes.Stfld, Member.Field<AttachableProp>(ap => ap.isReqSimple)))
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_0)
            .Instructions();
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

// A little object/component with two purposes:
//   - Keep the focal plane "lined up" with the katamari at all times
//   - Make it easier to tweak the DoF parameters, since UnityExplorer only has sliders in its UI for transforms.
public sealed class UpdateDof : MonoBehaviour {
    public DepthOfFieldModel dofModel = null!;
    public Transform katamari = null!;
    public Transform adjuster = null!;

    private float Distance() => Vector3.Distance(this.transform.position, this.katamari.position);

    private float? frozenDistance = null;
    public void FreezeDistance() => this.frozenDistance = this.Distance();
    public void ThawDistance() => this.frozenDistance = null;

    public void Start() {
        var gw = GlobalWork.instance;
        var i = Array.IndexOf(gw.camGame, this.GetComponent<Camera>());
        this.katamari = gw.player[i].KatamariTransform;

        this.dofModel = this.GetComponent<PostProcessingBehaviour>().profile.depthOfField;

        this.adjuster = new GameObject("DoF Adjust").transform;
        this.adjuster.parent = this.transform;

        // We want the DoF to behave fairly consistently throughout the game,
        // despite the scale of the gameplay ranging from centimeters to a kilometer.
        // Unity's "physically based" DoF parameters aren't very helpful for this situation,
        // but these parameters seem to work reasonably well based on experimentation in the eternal stages.
        //
        // Using a vector from a transform to configure the DoF means they can be adjusted as sliders in Unity Explorer.
        // (That's my only reason for not using normal component fields for this.)
        // Camera distance from the katamari seems to be a pretty good independent variable to use here.
        // "Aperture" is set to X. "Focal length" is set to distance^Y * Z.
        this.adjuster.localScale = new(0.25f, 0.52f, 5.6f);
    }

    public void Update() {
        var distance = this.frozenDistance ?? this.Distance();
        var s = this.adjuster.localScale;
        this.dofModel.settings = this.dofModel.settings with {
            aperture = s.x,
            focalLength = Mathf.Pow(distance, s.y) * s.z,
            focusDistance = distance,
        };
    }
}