using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

namespace SingleplayerCousins;

public static class Customs {
    private static readonly Assimp.AssimpContext ctx = new();

    public static GameObject LoadOuji() {
        //var name = "OUJI16";
        var name = "OUJI15";
        var ouji = AssetBundleSimulator.Instance.LoadAsset<GameObject>(name, name);

        var bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
            .ToDictionary(x => x.gameObject.name);

        var scene = ctx.ImportFile("./katamari_Data/StreamingAssets/StandaloneWindows/OUJI40.fbx");

        var uTextures = new List<Texture2D>();
        foreach (var aMat in scene.Materials) {
            var path = aMat.TextureDiffuse.FilePath;
            var uTex = new Texture2D(0, 0);

            if (path.StartsWith("*")) {
                var i = int.Parse(path.Substring(1));
                var aTex = scene.Textures[i];
                //Console.WriteLine($"Embedded texture: {aTex.IsCompressed}, {aTex.CompressedFormatHint}");
                ImageConversion.LoadImage(uTex, aTex.CompressedData);
            } else {
                var data = File.ReadAllBytes(path);
                ImageConversion.LoadImage(uTex, data);
            }

            uTextures.Add(uTex);
        }

        foreach (var aMesh in scene.Meshes) {
            if (!bodyParts.TryGetValue(aMesh.Name, out var bodyPart)) continue;
            //var uMesh = new Mesh() { name = aMesh.Name };
            var uMesh = bodyPart.sharedMesh;
            var uBones = bodyPart.bones;

            var vertices = aMesh.Vertices.Select(v => v.ToUnity()).ToList();
            uMesh.SetVertices(vertices);

            var topo = bodyPart.sharedMesh.GetTopology(0);
            var indices = new List<int>(3 * aMesh.FaceCount);
            foreach (var face in aMesh.Faces) indices.AddRange(face.Indices);
            uMesh.SetIndices(indices.ToArray(), topo, 0);

            var uvs = aMesh.TextureCoordinateChannels[0].Select(v => v.ToUnityVec2()).ToList();
            uMesh.SetUVs(0, uvs);

            var uBoneWeights = new BoneWeight[vertices.Count];
            var uBindposes = new Matrix4x4[uBones.Length];

            foreach (var aBone in aMesh.Bones) {
                var boneIdx = uBones.IndexOfBone(aBone.Name);
                uBindposes[boneIdx] = aBone.OffsetMatrix.ToUnity();

                foreach (var aWeight in aBone.VertexWeights) {
                    if (aWeight.Weight == 0) continue;
                    uBoneWeights[aWeight.VertexID] = uBoneWeights[aWeight.VertexID].AddWeight(boneIdx, aWeight.Weight);
                }
            }
            uMesh.boneWeights = uBoneWeights;
            uMesh.bindposes = uBindposes;


            // Something about the bindposes and/or scaling is fucking up the arm animations.
            // In the default pose, the arms look positioned and scaled fine, but when animated, it's like the bones are moved a lot more than they should.

            bodyPart.material.mainTexture = uTextures[aMesh.MaterialIndex];
        }

        return ouji;
    }

    private static BoneWeight AddWeight(this BoneWeight self, int idx, float weight) {
        if (self.weight0 == 0) {
            self.boneIndex0 = idx;
            self.weight0 = weight;
        } else if (self.weight1 == 0) {
            self.boneIndex1 = idx;
            self.weight1 = weight;
        } else if (self.weight2 == 0) {
            self.boneIndex2 = idx;
            self.weight2 = weight;
        } else if (self.weight3 == 0) {
            self.boneIndex3 = idx;
            self.weight3 = weight;
        } else {
            throw new InvalidOperationException("Too many weights on one bone!");
        }
        return self;
    }

    private static int IndexOfBone(this Transform[] bones, string name) {
        for (var i = 0; i < bones.Length; i++) {
            if (bones[i].name == name) {
                return i;
            }
        }
        throw new InvalidOperationException($"No bone found with name {name}");
    }

    static Customs() {
        var platformHelper = Type.GetType("Assimp.Unmanaged.PlatformHelper, AssimpNet, Version=4.1.0.0, Culture=neutral, PublicKeyToken=0d51b391f59f42a6");
        var targetMethod = AccessTools.Method(platformHelper, "GetAppBaseDirectory");
        var patchMethod = AccessTools.Method(typeof(Customs), nameof(StupidHack));
        new Harmony("Stupid hack").Patch(targetMethod, postfix: new(patchMethod));
    }
    private static void StupidHack(ref string __result) => __result = "./katamari_Data/Plugins";
}