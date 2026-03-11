using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace GraphicalEnhancements;

public static class AnisotropicGround {
    public static void Init() {
        SceneManager.activeSceneChanged += static (prev, cur) => {
            if (cur.name == "GameMain") {
                if (prev.name is "st2" or "st3") {
                    SharpenGrass(prev);
                }
            }
        };
    }

    public static void SharpenGrass(Scene scene) {
        var materials = new Dictionary<string, Material>();
        var materialUsers = new Dictionary<string, List<MeshRenderer>>();

        foreach (var root in scene.GetRootGameObjects()) {
            if (!EligibleRoot(root.name)) continue;

            foreach (var obj in root.transform.Children()) {
                if (!EligibleObject(obj.name)) continue;

                var renderer = obj.GetComponent<MeshRenderer>();
                var mat = renderer.sharedMaterial;
                if (!materials.ContainsKey(mat.name)) {
                    materials.Add(mat.name, mat);
                    materialUsers.Add(mat.name, []);
                }
                materialUsers[mat.name].Add(renderer);
            }
        }

        foreach (var pair in materials) {
            GlobalWork.Instance.StartCoroutine(Ensharpen(pair.Value, materialUsers[pair.Key]));
        }
    }

    private static bool EligibleRoot(string name) {
        if (name is "s_2_a" or "s_3_a") return true;
        if (name == "s_2_b") return true;
        if (name == "s_3_c") return true;
        if (name == "s_3_d") return true;
        if (name.StartsWith("s_2_e")) return true;
        if (name.StartsWith("s_3_e")) return true;
        return false;
    }

    private static bool EligibleObject(string name) {
        if (name.Contains("shibafu")) return true;
        if (name.Contains("patrn")) return true;
        if (name.Contains("obj_st3_ground")) return true;
        if (name.Contains("obj_st3_concr")) return true;
        if (name.Contains("obj_earthColor")) return true;
        if (name.Contains("obj_ground")) return true;
        if (name.Contains("obj_gravel")) return true;
        if (name.Contains("obj_grass")) return true;
        if (name.Contains("obj_asphalt")) return true;
        if (name.Contains("ztexatlas")) return true;
        return false;
    }

    public static IEnumerator Ensharpen(Material material, List<MeshRenderer> users) {
        var srcTex = (Texture2D)material.mainTexture;
        int w = srcTex.width, h = srcTex.height;

        // This is very silly, but we need to point-upscale the pixel art terrain texture a bit
        // in order to have it still look sharp in the extreme foreground,
        // while also being subject to anisotropic/trilinear filtering.
        // However, let's avoid doing that for the atlas textures, which have size 2048x2048.
        if (w <= 128 && h <= 128) {
            w *= 8;
            h *= 8;
        } else {
            w *= 2;
            h *= 2;
        }

        var blitTex = new RenderTexture(w, h, 0);
        blitTex.Create();
        Graphics.Blit(srcTex, blitTex);

        var request = AsyncGPUReadback.Request(blitTex, mipIndex: 0);
        while (!request.done) yield return new WaitForEndOfFrame();
        var pixels = request.GetData<Color32>();
        blitTex.Release();

        var newTex = new Texture2D(w, h, TextureFormat.RGB24, mipmap: true) {
            filterMode = FilterMode.Trilinear,
            anisoLevel = 16,
            name = srcTex.name + " (Anisotropic)",
        };
        newTex.SetAllPixels32(pixels.ToArray(), miplevel: 0);
        newTex.Apply(updateMipmaps: true, makeNoLongerReadable: true);

        material.mainTexture = newTex;
        foreach (var user in users) {
            user.sharedMaterial = material;
        }
    }
}