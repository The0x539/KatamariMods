using HarmonyLib;

using System;

using UnityEngine;

using UnityEngine.PostProcessing;

namespace GraphicalEnhancements;

static class DepthOfField {
    // TODO: This probably isn't the place to put the updater component on the camera.
    // I need to learn more about the "life cycles" of some of these objects and components.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.SetActiveStageObject))]
    public static void EnableDof() {
        var gw = GlobalWork.Instance;
        if (gw.u8GameMode != DefineEnum.GI_GMODE.GI_GMODE_ENDING) {
            for (var i = 0; i <= 1; i++) {
                var cam = gw.camGame[i];
                if (cam == null) continue;

                var ppb = cam.GetComponent<PostProcessingBehaviour>();
                ppb.profile.depthOfField.enabled = QualitySetting.Instance.IsDOF;
                // This should probably be a graphics option
                ppb.profile.depthOfField.settings = ppb.profile.depthOfField.settings with { kernelSize = DepthOfFieldModel.KernelSize.VeryLarge };

                if (cam.GetComponent<UpdateDof>() == null) {
                    cam.gameObject.AddComponent<UpdateDof>();
                }
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
    private static RenderTexture depthCamColor = null!, depthCamDepth = null!;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DepthOfFieldComponent), nameof(DepthOfFieldComponent.Prepare))]
    public static void DepthPassToFixDof(DepthOfFieldComponent __instance) {
        var ctx = __instance.context;

        if (depthCamera == null) {
            depthCamera = new GameObject().AddComponent<Camera>();
            depthCamera.enabled = false;

            // This msaaSamples thing was very difficult to hunt down and made the whole thing not work.
            var desc = ctx.camera.targetTexture.descriptor with { msaaSamples = 1 };

            desc = desc with { colorFormat = RenderTextureFormat.R8, depthBufferBits = 0 };
            depthCamColor = new RenderTexture(desc) { name = "Depth Pass - Color" };

            desc = desc with { colorFormat = RenderTextureFormat.Depth, depthBufferBits = 24 };
            depthCamDepth = new RenderTexture(desc) { name = "Depth Pass - Depth" };
        }

        var depthCam = depthCamera;

        depthCam.CopyFrom(ctx.camera);
        depthCam.SetTargetBuffers(depthCamColor.colorBuffer, depthCamDepth.depthBuffer);
        depthCam.Render();

        RenderTexture.active = Shader.GetGlobalTexture("_CameraDepthTexture") as RenderTexture;
        var blitToDepth = ctx.materialFactory.Get("Hidden/BlitToDepth");
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

    // First person mode doesn't play very well with this - perhaps use the "desired follow distance" in such cases?
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