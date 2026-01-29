using BepInEx;
using BepInEx.Unity.Mono;

using HarmonyLib;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.Rendering;

namespace DevUtils;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public void Awake() {
        Application.runInBackground = true;

        Harmony.CreateAndPatchAll(this.GetType());

        if (this.Config.Bind("Intro", "Skip", true).Value) {
            Harmony.CreateAndPatchAll(typeof(SkipIntro));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MyGame.InputController), nameof(MyGame.InputController.IsSelectDown))]
    public static void NoClick(ref bool isMouse) {
        isMouse = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SimulationNativeMethods), nameof(SimulationNativeMethods.CallbackPlaySoundFX))]
    public static void Dof() {
        var cam = GlobalWork.Instance?.camGame?[0];
        var dof = cam?.GetComponent<PostProcessingBehaviour>()?.m_DepthOfField?.model;
        if (dof == null) return;

        /*
        if (cam!.GetComponent<CameraHooks>() == null) {
            cam.gameObject.AddComponent<CameraHooks>();
        }
        */

        DofOn(dof);
    }

    private static void DofOn(DepthOfFieldModel dof) {
        dof.enabled = true;
        dof.settings = dof.settings with { aperture = 2, focalLength = 100, focusDistance = 10 };
    }

    [HarmonyPrefix]
    [HarmonyDebug]
    [HarmonyPatch(typeof(DepthOfFieldComponent), nameof(DepthOfFieldComponent.Prepare))]
    public static void OnDofHook(DepthOfFieldComponent __instance) {
        try {
            OnDof(__instance);
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    private static GameObject depthCamera = null!;
    private static RenderTexture depthCamColor = null!, depthCamDepth = null!;

    public static void OnDof(DepthOfFieldComponent __instance) {
        var ctx = __instance.context;

        if (depthCamera == null) {
            depthCamera = new GameObject();
            depthCamera.AddComponent<Camera>();
            depthCamColor = new RenderTexture(1600, 900, 0, RenderTextureFormat.ARGB32) { name = "Buffer C" };
            depthCamDepth = new RenderTexture(1600, 900, 24, RenderTextureFormat.Depth) { name = "Buffer D" };
            depthCamColor.Create();
            depthCamDepth.Create();
        }

        var depthCam = depthCamera.GetComponent<Camera>();
        depthCam.enabled = false;

        var copyDepth = ctx.materialFactory.Get("Hidden/BlitCopyDepth");
        var blitToDepth = ctx.materialFactory.Get("Hidden/BlitToDepth");
        var dof = ctx.materialFactory.Get("Hidden/Post FX/Depth Of Field");
        var debug = ctx.materialFactory.Get("Hidden/Post FX/Builtin Debug Views");

        //var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

        //var buffer = new CommandBuffer();
        //buffer.SetRenderTarget(dest, src);
        //Graphics.ExecuteCommandBuffer(buffer);
        //var dest = RenderTexture.active;
        //Console.WriteLine($"dest: {dest.name}");
        //Graphics.SetRenderTarget(src.colorBuffer, src.depthBuffer);

        //copyDepth.SetTexture("_MainTex", src);
        //copyDepth.SetPass(0);

        var src = ctx.camera.GetComponent<GameManager>().renderTexture;
        //Console.WriteLine(src.name);

        //Shader.SetGlobalTexture("_CameraDepthTexture", ctx.camera.targetTexture);
        //Graphics.Blit(null, dst, debug, 0);
        //Graphics.SetRenderTarget(dst_c.colorBuffer, dst_d.depthBuffer);
        depthCam.CopyFrom(ctx.camera);
        depthCam.SetTargetBuffers(depthCamColor.colorBuffer, depthCamDepth.depthBuffer);
        depthCam.Render();

        var foo = (RenderTexture)Shader.GetGlobalTexture("_CameraDepthTexture");
        RenderTexture.active = foo;
        blitToDepth.SetPass(0);
        blitToDepth.SetTexture("_MainTex", depthCamDepth);
        GlQuad();

        //var dstColor = ctx.renderTextureFactory.Get(__instance.context.width, __instance.context.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, FilterMode.Bilinear, TextureWrapMode.Clamp, "Foo");
        //var dstDepth = ctx.renderTextureFactory.Get(__instance.context.width, __instance.context.height, 0, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, FilterMode.Bilinear, TextureWrapMode.Clamp, "Bar");
        //Console.WriteLine($"{src.name} ({src.format}) -> {dst.name} ({dst.format})");

        //var cmd = new CommandBuffer();
        //cmd.SetRenderTarget(dst.colorBuffer, src.depthBuffer);
        //cmd.DrawRenderer(quad.GetComponent<MeshRenderer>(), copyDepth);
        //Graphics.ExecuteCommandBuffer(cmd);

        //Console.WriteLine(src.depthBuffer.ToString());

        /*
        Graphics.SetRenderTarget(dst.colorBuffer, dst.depthBuffer);
        copyDepth.SetPass(0);
        copyDepth.SetTexture("_MainTex", src);
        Shader.SetGlobalTexture("_MainTex", src);
        GlQuad();
        */

        //Graphics.Blit(null, dst, dof, 0);

        //buffer.SetViewport();
        //buffer.Blit(src, dest);
        //Console.WriteLine("DOF");
        //Console.WriteLine($"  source: {source}");
        //Console.WriteLine($"  depth: {Shader.GetGlobalTexture("_CameraDepthTexture").name}");
    }

    private static void GlQuad() {
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

    /*
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DepthOfFieldComponent), nameof(DepthOfFieldComponent.Prepare))]
    public static bool Fooo(DepthOfFieldComponent __instance, RenderTexture source, Material uberMaterial, bool antialiasCoC, Vector2 taaJitter, float taaBlending) {
        var self = __instance;

        DepthOfFieldModel.Settings settings = self.model.settings;
        RenderTextureFormat format = RenderTextureFormat.DefaultHDR;
        RenderTextureFormat cocFormat = self.SelectFormat(RenderTextureFormat.R8, RenderTextureFormat.RHalf);

        float focalLength = self.CalculateFocalLength(),
              distance = Mathf.Max(settings.focusDistance, focalLength),
              aspect = source.width / source.height,
              lensCoeff = focalLength * focalLength / (settings.aperture * (distance - focalLength) * 0.024f * 2f),
              maxCoc = self.CalculateMaxCoCRadius(source.height);

        var material = self.context.materialFactory.Get("Hidden/Post FX/Depth Of Field");

        material.SetFloat(Uniforms._Distance, distance);
        material.SetFloat(Uniforms._LensCoeff, lensCoeff);
        material.SetFloat(Uniforms._MaxCoC, maxCoc);
        material.SetFloat(Uniforms._RcpMaxCoC, 1f / maxCoc);
        material.SetFloat(Uniforms._RcpAspect, 1f / aspect);

        int w = self.context.width,
            h = self.context.height;
        const FilterMode bilinear = FilterMode.Bilinear;
        const TextureWrapMode clamp = TextureWrapMode.Clamp;
        const RenderTextureReadWrite linear = RenderTextureReadWrite.Linear;

        var depthAndNormals = Shader.GetGlobalTexture("_CameraDepthNormalsTexture");

        var blit = self.context.materialFactory.Get("Hidden/BlitCopyDepth");
        //var bruh = self.context.renderTextureFactory.Get(w, h, 0, RenderTextureFormat.R8, linear, bilinear, clamp, "Bruh");
        //Graphics.Blit(depthAndNormals, bruh, blit);

        var coc_tex = self.context.renderTextureFactory.Get(w, h, 32, cocFormat, linear, bilinear, clamp, "Temp A");
        Graphics.Blit(depthAndNormals, coc_tex, blit, 0);

        if (antialiasCoC) {
            material.SetTexture(Uniforms._CoCTex, coc_tex);

            var z = (!self.CheckHistory(self.context.width, self.context.height)) ? 0f : taaBlending;
            var jitter = taaJitter;
            material.SetVector(Uniforms._TaaParams, new Vector3(jitter.x, jitter.y, z));

            var coc_history = RenderTexture.GetTemporary(self.context.width, self.context.height, 0, cocFormat);
            Graphics.Blit(self.m_CoCHistory, coc_history, material, 1);

            self.context.renderTextureFactory.Release(coc_tex);
            if (self.m_CoCHistory != null) {
                RenderTexture.ReleaseTemporary(self.m_CoCHistory);
            }
            coc_tex = (self.m_CoCHistory = coc_history);
        }

        RenderTexture dof_tex = self.context.renderTextureFactory.Get(w / 2, h / 2, 0, format, default, bilinear, clamp, "Temp B");
        material.SetTexture(Uniforms._CoCTex, coc_tex);
        //material.SetTexture(Uniforms._CoCTex, self.context.camera.targetTexture.depthBuffer);
        Graphics.Blit(source, dof_tex, material, 2);

        RenderTexture temp_c = self.context.renderTextureFactory.Get(w / 2, h / 2, 0, format, default, bilinear, clamp, "Temp C");
        Graphics.Blit(dof_tex, temp_c, material, (int)(3 + settings.kernelSize));

        Graphics.Blit(temp_c, dof_tex, material, 7);

        uberMaterial.SetVector(Uniforms._DepthOfFieldParams, new Vector3(distance, lensCoeff, maxCoc));
        if (self.context.profile.debugViews.IsModeActive(BuiltinDebugViewsModel.Mode.FocusPlane)) {
            uberMaterial.EnableKeyword("DEPTH_OF_FIELD_COC_VIEW");
            self.context.Interrupt();
        } else {
            uberMaterial.SetTexture(Uniforms._DepthOfFieldTex, dof_tex);
            uberMaterial.SetTexture(Uniforms._DepthOfFieldCoCTex, coc_tex);
            uberMaterial.EnableKeyword("DEPTH_OF_FIELD");
        }

        self.context.renderTextureFactory.Release(temp_c);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Graphics), nameof(Graphics.Blit), [typeof(Texture), typeof(RenderTexture), typeof(Material)])]
    [HarmonyPatch(typeof(Graphics), nameof(Graphics.Blit), [typeof(Texture), typeof(RenderTexture), typeof(Material), typeof(int)])]
    [HarmonyPatch(typeof(Graphics), nameof(Graphics.Blit), [typeof(Texture), typeof(Material)])]
    [HarmonyPatch(typeof(Graphics), nameof(Graphics.Blit), [typeof(Texture), typeof(Material), typeof(int)])]
    public static void OnBlit(Material mat) {
        Console.WriteLine($"Blit({mat.name})");
    }
    */

    /*
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DepthOfFieldComponent), nameof(DepthOfFieldComponent.GetCameraFlags))]
    public static void GetCameraFlags(ref DepthTextureMode __result) {
        __result = DepthTextureMode.DepthNormals;
    }
    */

    private static readonly HashSet<string> seen = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalManager), nameof(GlobalManager.SetActiveStageObject))]
    public static void Ourgh(GlobalManager __instance) {
        foreach (var x in __instance.objCombineMeshes) {
            x.GetComponent<MeshRenderer>().material.SetInt("_Zwrite", 1);
        }
    }
}

public sealed class CameraHooks : MonoBehaviour {
    private Camera cam = null!;
    //private Camera otherCam = null!;
    //private Material matDepth = null!;

    private bool needsInit = false;

    public void Start() {
        //this.matDepth = new Material(Resources.Load<Material>("RenderDepth"));
        this.cam = this.GetComponent<Camera>();
        //this.copyDepth = new Material(Resources.Load<Material>("Hidden/BlitCopyDepth"));
        this.needsInit = true;
    }

    public void Update() {
        if (this.needsInit) {
            if (this.TryInit()) {
                this.needsInit = false;
            }
        }
    }

    //private int n = 0;
    //private Color[] colors = [Color.red, new(255, 127, 0), Color.yellow, Color.green, Color.blue, Color.magenta];

    public void OnWillRenderObject() {
        //Console.WriteLine($"    ORWO active texture: {this.cam.activeTexture.name}");
    }

    private bool TryInit() {
        var cb = new CommandBuffer();

        //var src = cam.activeTexture.depthBuffer;
        //var dst = Shader.GetGlobalTexture("_CameraDepthTexture");

        //if (dst == null) return false;

        //Console.WriteLine($"preparing to blit {src.m_RenderTextureInstanceID} -> {dst.name} ({dst.GetInstanceID()})");

        //cb.ClearRenderTarget(false, true, Color.green);
        //cb.Blit(src, dst);

        cb.ClearRenderTarget(false, true, Color.blue);
        var src = this.cam.activeTexture;
        var dst = (RenderTexture)Shader.GetGlobalTexture("_CameraDepthTexture");
        //cb.CopyTexture(src.depthBuffer, dst.depthBuffer);
        //cb.ConvertTexture(src.depthBuffer, dst.depthBuffer);
        cb.SetRenderTarget(src);
        cb.ClearRenderTarget(true, true, Color.green);
        //cb.Blit(src, dst);
        //cb.SetRenderTarget(src.colorBuffer, dst.depthBuffer);
        //Console.WriteLine($"    ORO active texture: {this.cam.activeTexture.name}");


        this.cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, cb);

        var cb2 = new CommandBuffer();
        cb2.ClearRenderTarget(false, true, Color.red);
        this.cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, cb2);

        Console.WriteLine("init done");

        return true;
    }


    /*
    public void OnPostRender() {
        Console.WriteLine($"OnPostRender {this.name}");
        this.otherCam.CopyFrom(this.cam);
        this.otherCam.RenderWithShader(this.matDepth.shader, "");
        this.ReportTextures();
    }
    public void OnPreCull() {
        Console.WriteLine($"OnPreCull {this.name}");
        this.ReportTextures();
    }
    public void OnPreRender() {
        Console.WriteLine($"OnPreRender {this.name}");
        this.ReportTextures();
    }
    public void OnRenderImage() {
        Console.WriteLine($"OnRenderImage {this.name}");
        this.ReportTextures();
    }
    public void OnRenderObject() {
        Console.WriteLine($"OnRenderObject {this.name}");
        this.ReportTextures();
    }
    public void OnWillRenderObject() {
        Console.WriteLine($"OnWillRenderObject {this.name}");
        this.ReportTextures();
    }

    private void ReportTextures() {
        Console.WriteLine($"     dt mode: {this.cam.depthTextureMode}");
        Console.WriteLine($"     active texture: {this.cam.activeTexture.name}");
        Console.WriteLine($"     active color: {this.cam.activeTexture.colorBuffer.m_RenderTextureInstanceID}");
        Console.WriteLine($"     active depth: {this.cam.activeTexture.depthBuffer.m_RenderTextureInstanceID}");
        Console.WriteLine($"     CameraDepthTexture: {Shader.GetGlobalTexture("_CameraDepthTexture").name}");
        Console.WriteLine($"     LastCameraDepthTexture: {Shader.GetGlobalTexture("_LastCameraDepthTexture").name}");
    }
    */
}
