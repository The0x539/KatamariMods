using DefineEnum;

using HarmonyLib;

using System;
using System.Reflection.Emit;

using UnityEngine;

using UnityEngine.PostProcessing;
using UnityEngine.Rendering;

namespace GraphicalEnhancements;

static class PostProcessing {
    // TODO: This probably isn't the place to put the updater component on the camera.
    // I need to learn more about the "life cycles" of some of these objects and components.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.SetActiveStageObject))]
    public static void EnableDof() {
        var gw = GlobalWork.Instance;
        if (gw.u8GameMode == GI_GMODE.GI_GMODE_ENDING) return;

        if (gw.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_VS) {
            // Getting these effects (or more specifically what I've done with the depth buffer)
            // to work properly in versus mode would be a significant chunk of extra work to figure out.
            foreach (var vsCam in gw.camGame) {
                var vsPpb = vsCam.GetComponent<PostProcessingBehaviour>();
                vsPpb.profile.depthOfField.enabled = false;
                vsPpb.profile.ambientOcclusion.enabled = false;
            }
            return;
        }

        var cam = gw.camGame[0];

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

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PostProcessingBehaviour), nameof(PostProcessingBehaviour.OnPreRender))]
    public static IL TakeControlOfSSAO(IL il) {
        // Normally, the ambient occlusion effect schedules its stuff to run during CameraEvent.BeforeImageEffectsOpaque.
        // However, it's VERY difficult to get non–command buffer–flavored code to run immediately before this,
        // and I haven't gotten the depth buffer fix to work with command buffers.
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
    [HarmonyPatch(typeof(SetupRenderTexture), nameof(SetupRenderTexture.SetTexture))]
    public static void UseSeparateDepthBuffer(SetupRenderTexture __instance, out bool __runOriginal) {
        if (GlobalWork.Instance.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_VS) {
            __runOriginal = true;
            return;
        }

        __runOriginal = false;
        var self = __instance;
        var setIndex = self.setIndex;
        self.setIndex = 0;
        if (setIndex == self.setIndex) return;

        var colorTarget = self.renderTexture[self.setIndex];
        var depthComponent = self.GetComponent<SeparateDepthTarget>() ?? self.gameObject.AddComponent<SeparateDepthTarget>();
        var depthTarget = depthComponent.Init(colorTarget);

        self.mainCamera.SetTargetBuffers(colorTarget.colorBuffer, depthTarget.depthBuffer);
        self.gameManager.GameRenderTexture = colorTarget;
        self.outputImage.texture = colorTarget;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PostProcessingBehaviour), nameof(PostProcessingBehaviour.OnRenderImage))]
    public static void ProvideDepth(PostProcessingBehaviour __instance, ref RenderTexture? source) {
        var ctx = __instance.m_Context;

        if (source == null) {
            source = ctx.camera.GetComponent<SetupRenderTexture>().renderTexture[0];
        }

        if (!__instance.m_AmbientOcclusion.active && !__instance.m_DepthOfField.active) return;

        if (ctx.camera.GetComponent<SeparateDepthTarget>()?.texture is not RenderTexture srcDepth) return;
        var dstDepth = (RenderTexture)Shader.GetGlobalTexture("_CameraDepthTexture");

        RenderTexture.active = dstDepth;
        var blitToDepth = ctx.materialFactory.Get("Hidden/BlitToDepth_MSAA");

        blitToDepth.SetTexture("_MainTex", srcDepth);
        GraphicsUtils.Blit(blitToDepth, 0);

        if (__instance.m_AmbientOcclusion.active) {
            // For some reason, the AO non-customizably renders to "the current camera target".
            // For some reason, setting separate color and depth buffers on a camera causes that to not be defined.
            ctx.camera.targetTexture = source;

            var cb = new CommandBuffer();
            __instance.m_AmbientOcclusion.PopulateCommandBuffer(cb);
            Graphics.ExecuteCommandBuffer(cb);

            ctx.camera.SetTargetBuffers(source.colorBuffer, srcDepth.depthBuffer);
        }

        GL.SetViewMatrix(ctx.camera.worldToCameraMatrix);
        GL.LoadProjectionMatrix(ctx.camera.projectionMatrix);

        // Now that SSAO is done, it's safe to draw the clouds to the depth buffer so that DoF blurs them correctly.
        // However, unlike an earlier version of this code, we're going to need to draw to our "main" depth buffer, because it's antialiased.
        // Fortunately, just copying again after this is done should be fine and pretty cheap.
        Graphics.SetRenderTarget(source.colorBuffer, srcDepth.depthBuffer);
        RedrawJungle();
        RedrawClouds();
        RedrawSmoke(ctx, source);

        blitToDepth.SetTexture("_MainTex", srcDepth);
        GraphicsUtils.Blit(blitToDepth, 0);
    }

    private static void RedrawJungle() {
        var player = GlobalWork.Instance.player[0];
        if (player.oujiNo != 23 || !player.objOuji.activeInHierarchy) return;

        var mesh = new Mesh();
        foreach (var name in new[] { "head_tawara_m", "body01_m", "hand_m" }) {
            var bodyPart = player.objOuji.transform.Find("body_root/" + name).GetComponent<SkinnedMeshRenderer>();
            bodyPart.sharedMaterial.SetPass(0);
            bodyPart.BakeMesh(mesh);
            Graphics.DrawMeshNow(mesh, bodyPart.transform.position, bodyPart.transform.rotation);
        }

        var billboard = player.objBillboard.transform.GetChild(0);
        billboard.GetComponent<MeshRenderer>().sharedMaterial.SetPass(0);
        Graphics.DrawMeshNow(billboard.GetComponent<MeshFilter>().sharedMesh, billboard.localToWorldMatrix);
    }

    private static void RedrawClouds() {
        foreach (var obj in SpecialDraw.instances) {
            if (!obj.SpecialThisFrame) continue;

            var renderer = obj.Prop.mRenderers[0];
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial.SetPass(0);
            Graphics.DrawMeshNow(mesh, obj.transform.localToWorldMatrix);
        }
    }

    private static readonly int smokeLayerMask = LayerMask.GetMask("TransparentFX");

    private static Camera smokeCamera = null!;
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
}

// A little object/component with two purposes:
//   - Keep the focal plane "lined up" with the katamari at all times
//   - Make it easier to tweak the DoF parameters, since UnityExplorer only has sliders in its UI for transforms.
public sealed class UpdateDof : MonoBehaviour {
    public DepthOfFieldModel dofModel = null!;
    public Camera camera = null!;
    public Transform katamari = null!;
    public Transform adjuster = null!;

    private float Distance() => Vector3.Distance(this.transform.position, this.katamari.position);

    private float? frozenDistance = null;
    public void FreezeDistance() => this.frozenDistance = this.Distance();
    public void ThawDistance() => this.frozenDistance = null;

    public void Start() {
        this.camera = this.GetComponent<Camera>();
        this.dofModel = this.GetComponent<PostProcessingBehaviour>().profile.depthOfField;

        var gw = GlobalWork.instance;
        var i = Array.IndexOf(gw.camGame, this.camera);
        this.katamari = gw.player[i].KatamariTransform;

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
        this.adjuster.localScale = new(1f, 0.5f, 15f);
        // Originally, I had the DoF effect configured to put the focus distance at exactly the katamari's location.
        // This would put the katamari at the center of the focused area, and worked okay.
        // However, from experimentation, I've gotten better results from putting the focal point further away,
        // such that the katamari ends up at the near edge of the focused area.
        //
        // For whatever reason, this allows for more favorable behavior of the "focus falloff".
        // This X component controls the ratio between camera–katamari distance and the focus distance.
        //
        // Different screen resolutions call for different lens coefficients because Unity's DoF effect is mediocre.
        // I chose the base settings with the game set to 1440p and quickly found another "for point" of aperture = 0.6 at 720p.
        // At 720p, the aperture is multiplied by the Y component.
        // At 1440p, it's multiplied by the Z component.
        // In between the two, or beyond 1440p, the value is linearly interpolated based on those two control points.
        this.adjuster.localPosition = new(1.75f, 0.55f, 1.0f);
    }

    public void Update() {
        var distance = this.frozenDistance ?? this.Distance();
        var s = this.adjuster.localScale;
        var p = this.adjuster.localPosition;

        var apertureT = (this.camera.pixelHeight - 720) / 720f;
        var apertureFactor = Mathf.LerpUnclamped(p.y, p.z, apertureT);

        this.dofModel.settings = this.dofModel.settings with {
            aperture = s.x * apertureFactor,
            focalLength = Mathf.Pow(distance, s.y) * s.z,
            focusDistance = distance * p.x,
        };
    }
}

public sealed class SeparateDepthTarget : MonoBehaviour {
    public RenderTexture? colorTexture = null;
    public RenderTexture? texture = null;

    public RenderTexture Init(RenderTexture tex) {
        this.Release();

        this.colorTexture = tex;
        this.texture = new RenderTexture(tex.width, tex.height, depth: 32, RenderTextureFormat.Depth) {
            antiAliasing = tex.antiAliasing,
            bindTextureMS = true,
            name = tex.name.Replace("Main", "Depth"),
        };
        this.texture.Create();
        return this.texture;
    }

    public void OnDestroy() => this.Release();

    public void Release() {
        if (this.texture != null) {
            this.texture.Release();
            this.texture = null;
        }
    }
}