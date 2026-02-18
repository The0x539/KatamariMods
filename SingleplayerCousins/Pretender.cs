using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace SingleplayerCousins;

public sealed class Pretender {
    public static readonly Dictionary<int, Pretender> pretenders;
    public static readonly int maxID = 24;

    static Pretender() {
        pretenders = [];

        var items = Directory.GetFiles("./Pretenders/");
        Array.Sort(items);
        var dynamicId = (int)PretenderId.DYNAMIC;
        foreach (var path in items) {
            if (Path.GetExtension(path) != ".fbx") continue;

            int id;

            var name = Path.GetFileNameWithoutExtension(path);
            try {
                var known = (PretenderId)Enum.Parse(typeof(PretenderId), name);
                id = (int)known;
            } catch {
                id = dynamicId++;
            }

            maxID = Math.Max(maxID, id);

            var p = new Pretender { Id = id, Name = name, FilePath = path };
            pretenders.Add(id, p);
        }
    }

    public int Id { get; init; } = 0;
    public string Name { get; init; } = "";
    public string FilePath { get; init; } = "";

    public GameObject Reify() {
        var name = "OUJI16"; // June is a pretty Prince-shaped character who's also unlocked from the start, so a good candidate
        var ouji = AssetBundleSimulator.Instance.LoadAsset<GameObject>(name, name);
        PretenderLoader.ApplyModel(ouji, this.FilePath);
        ouji.name = $"OUJI{this.Id:00}-{this.Name}";
        return ouji;
    }
}

public static class PretenderPatches {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalWork), nameof(GlobalWork.InitItoko))]
    public static void InitItoko(GlobalWork __instance) {
        if (__instance.objItoko.Length > 24) return;

        var newArr = new GameObject[Pretender.maxID];

        for (var i = 0; i < 24; i++) {
            newArr[i] = __instance.objItoko[i];
        }
        foreach (var j in Pretender.pretenders.Keys) {
            newArr[j - 1] = Pretender.pretenders[j].Reify();
        }

        __instance.objItoko = newArr;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Load))]
    public static void AddToOujiArray(ref SaveManager2.SaveData saveData) => AddToOujiArray(saveData.siGame);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Save))]
    public static void AddToOujiArray(GlobalWork gWork) => AddToOujiArray(gWork.siGame);

    public static void AddToOujiArray(SI_GAME siGame) {
        if (siGame.oujiArray.Length > 24) return;

        var newArr = new int[Pretender.maxID];

        for (var i = 0; i < 24; i++) {
            newArr[i] = siGame.oujiArray[i];
        }
        foreach (var j in Pretender.pretenders.Keys) {
            newArr[j - 1] = 1;
        }

        siGame.oujiArray = newArr;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Load))]
    public static void ObscureFromSaveFile(ref SaveManager2.SaveData saveData) => ObscureFromSaveFile(saveData.siGame);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Save))]
    public static void ObscureFromSaveFile(GlobalWork gWork) => ObscureFromSaveFile(gWork.siGame);

    public static void ObscureFromSaveFile(SI_GAME siGame) {
        if (siGame.oujiArray.Length <= 24) return;

        siGame.oujiArray = [.. siGame.oujiArray.Take(24)];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIKinoko), nameof(UIKinoko.SetOujiName))]
    public static void SetName(UIKinoko __instance, int id, bool isOnePlayer) {
        if (!Pretender.pretenders.TryGetValue(id, out var pretender)) return;

        var text = isOnePlayer ? __instance._text_playerLName : __instance._text_playerRName;
        text.text = $"・{pretender.Name}・";
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static IEnumerable<CodeInstruction> LoadIngame(IEnumerable<CodeInstruction> instructions) {
        // TODO: The array used to choose the katamari model still needs to be fixed,
        // so Eternal mode and multiplayer currently don't work with pretenders

        return new CodeMatcher(instructions)
            .MatchForward(false,
                          new(OpCodes.Call, AccessTools.PropertyGetter(typeof(AssetBundleSimulator), nameof(AssetBundleSimulator.Instance))),
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld),
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld),
                          new(OpCodes.Callvirt))
            .RemoveInstructions(6)
            .InsertAndAdvance(new(OpCodes.Ldarg_0),
                              new(OpCodes.Call, AccessTools.Method(typeof(PretenderPatches), nameof(LoadIngameImpl))))
            .Instructions();
    }

    private static GameObject LoadIngameImpl(Player player) {
        if (Pretender.pretenders.TryGetValue(player.oujiNo, out var pretender)) {
            // Fix the animator controller situation
            var newArr = new RuntimeAnimatorController[player.oujiNo + 1];
            int i;
            for (i = 0; i < player.animController.Length; i++) {
                newArr[i] = player.animController[i];
            }
            for (; i < newArr.Length; i++) {
                newArr[i] = player.animController[0];
            }
            player.animController = newArr;

            return pretender.Reify();
        } else {
            var name = player.oujiName;
            return AssetBundleSimulator.Instance.LoadAsset<GameObject>(name, name);
        }
    }
}

internal static class PretenderLoader {
    internal static readonly Assimp.AssimpContext ctx;

    static PretenderLoader() {
        var platformHelper = Type.GetType("Assimp.Unmanaged.PlatformHelper, AssimpNet, Version=4.1.0.0, Culture=neutral, PublicKeyToken=0d51b391f59f42a6");
        var targetMethod = AccessTools.Method(platformHelper, "GetAppBaseDirectory");
        var patchMethod = AccessTools.Method(typeof(PretenderLoader), nameof(StupidHack));
        new Harmony("Stupid hack").Patch(targetMethod, postfix: new(patchMethod));
        ctx = new();
    }

    private static void StupidHack(ref string __result) => __result = "./katamari_Data/Plugins";

    private static readonly Dictionary<string, Assimp.Scene> sceneCache = new();

    public static Assimp.Scene LoadFile(string path) {
        if (sceneCache.TryGetValue(path, out var existing)) return existing;
        var scene = ctx.ImportFile(path);
        sceneCache[path] = scene;
        return scene;
    }

    public static void ApplyModel(GameObject ouji, string path) => ApplyModel(ouji, LoadFile(path));


    public static void ApplyModel(GameObject ouji, Assimp.Scene scene) {
        var bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
            .ToDictionary(x => x.gameObject.name);

        var bones = new Dictionary<string, Transform>();
        foreach (var bone in ouji.GetComponentsInChildren<Transform>(true)) {
            bones[bone.name] = bone;
        }

        var body_m = bodyParts["body_m"];
        var body_root = body_m.transform.parent;

        var uMaterials = new List<Material?>();
        foreach (var aMat in scene.Materials) {
            var path = aMat.TextureDiffuse.FilePath;
            if (path == null) {
                Console.WriteLine($"Texture {aMat.Name} has no filepath");
                uMaterials.Add(null);
                continue;
            }

            var uTex = new Texture2D(0, 0);

            if (path.StartsWith("*")) {
                var i = int.Parse(path.Substring(1));
                var aTex = scene.Textures[i];
                ImageConversion.LoadImage(uTex, aTex.CompressedData);
            } else {
                var data = File.ReadAllBytes(path);
                ImageConversion.LoadImage(uTex, data);
            }

            uTex.filterMode = FilterMode.Point;

            var uMat = UnityObject.Instantiate(body_m.material);
            uMat.name = uTex.name = aMat.Name; // TODO: This name is absolutely not guaranteed to be unique across different characters.
            uMat.mainTexture = uTex;
            uMaterials.Add(uMat);
        }

        foreach (var aMesh in scene.Meshes) {
            var bodyPart = new GameObject(aMesh.Name);
            bodyPart.transform.SetParent(body_root);
            bodyPart.hideFlags = HideFlags.HideAndDontSave;

            var renderer = bodyPart.AddComponent<SkinnedMeshRenderer>();
            var uMesh = new Mesh() { name = aMesh.Name };

            var vertices = aMesh.Vertices.Select(v => v.ToUnity()).ToList();
            uMesh.SetVertices(vertices);

            var topo = aMesh.Faces[0].IndexCount switch {
                3 => MeshTopology.Triangles,
                4 => MeshTopology.Quads,
                _ => throw new InvalidOperationException(),
            };
            var indices = new List<int>(3 * aMesh.FaceCount);
            foreach (var face in aMesh.Faces) indices.AddRange(face.Indices);
            uMesh.SetIndices(indices.ToArray(), topo, 0);

            var uvs = aMesh.TextureCoordinateChannels[0].Select(v => v.ToUnityVec2()).ToList();
            uMesh.SetUVs(0, uvs);

            var uBoneWeights = new BoneWeight[vertices.Count];
            var uBindposes = new List<Matrix4x4> { Matrix4x4.identity };
            var uBones = new List<Transform> { bones["JNT_root"] };

            foreach (var aBone in aMesh.Bones) {
                int boneIdx;
                if (bones.TryGetValue(aBone.Name, out var bone)) {
                    boneIdx = uBones.Count;
                    uBones.Add(bone);
                    uBindposes.Add(aBone.OffsetMatrix.ToUnity());
                } else {
                    boneIdx = 0;
                }

                foreach (var aWeight in aBone.VertexWeights) {
                    if (aWeight.Weight == 0) continue;
                    uBoneWeights[aWeight.VertexID] = uBoneWeights[aWeight.VertexID].AddWeight(boneIdx, aWeight.Weight);
                }
            }

            renderer.bones = uBones.ToArray();
            uMesh.bindposes = uBindposes.ToArray();
            uMesh.boneWeights = uBoneWeights;

            if (aMesh.HasNormals) {
                var normals = aMesh.Normals.Select(n => n.ToUnity()).ToList();
                uMesh.SetNormals(normals);
            } else {
                uMesh.RecalculateNormals();
            }

            renderer.rootBone = bones["JNT_root"];
            renderer.sharedMesh = uMesh;
            if (uMaterials[aMesh.MaterialIndex] is Material mat) {
                renderer.material = mat;
            }
        }

        foreach (var part in bodyParts.Values) {
            part.enabled = false;
        }
    }
}