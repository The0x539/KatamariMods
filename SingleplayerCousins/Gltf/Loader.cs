using System;
using System.Linq;

using UnityEngine;

using UMesh = UnityEngine.Mesh;

namespace SingleplayerCousins.Gltf;

struct Vector4Byte { public byte x, y, z, w; }

public static class Loader {
    public static UMesh LoadMesh(AssetFile file, Mesh gMesh, byte[] binary, bool skinned, bool morph) {
        var uMesh = new UMesh() { name = gMesh.name };

        ulong numVerts = 0;

        foreach (var primitive in gMesh.primitives) {
            void expect(string attribute, Accessor.ComponentType e_ct, Accessor.Shape e_ty) {
                var accessor = file.accessors[primitive.attributes[attribute]];
                var ct = accessor.componentType;
                var ty = accessor.type;
                if (ct != e_ct || ty != e_ty) {
                    throw new Exception($"Vertex {attribute} accessor is {ty}/{ct} (expected {e_ty}/{e_ct})");
                }
            }

            expect("POSITION", Accessor.ComponentType.F32, Accessor.Shape.VEC3);
            expect("NORMAL", Accessor.ComponentType.F32, Accessor.Shape.VEC3);
            expect("TEXCOORD_0", Accessor.ComponentType.F32, Accessor.Shape.VEC2);

            if (skinned) {
                expect("JOINTS_0", Accessor.ComponentType.U8, Accessor.Shape.VEC4);
                expect("WEIGHTS_0", Accessor.ComponentType.F32, Accessor.Shape.VEC4);
            }

            var pm = primitive.mode;
            if (pm != Mesh.Primitive.Mode.Triangles) {
                throw new Exception($"Mesh {gMesh.name} is using primitive mode {pm} (expected TRIANGLES)");
            }

            numVerts += file.accessors[primitive.attributes["POSITION"]].count;
        }

        var verts = new Vector3[numVerts];
        var normals = new Vector3[numVerts];
        var uvs = new Vector2[numVerts];
        ulong vertIdx = 0;
        foreach (var primitive in gMesh.primitives) {
            file.accessors[primitive.attributes["POSITION"]].CopyOut(file, binary, verts, vertIdx);
            file.accessors[primitive.attributes["NORMAL"]].CopyOut(file, binary, normals, vertIdx);
            file.accessors[primitive.attributes["TEXCOORD_0"]].CopyOut(file, binary, uvs, vertIdx);
            vertIdx += file.accessors[primitive.attributes["POSITION"]].count;
        }

        for (ulong i = 0; i < numVerts; i++) {
            var uv = uvs[i];
            uv.y = 1 - uv.y;
            uvs[i] = uv;
        }

        uMesh.vertices = verts;
        uMesh.normals = normals;
        uMesh.uv = uvs;

        if (skinned) {
            uMesh.boneWeights = LoadBoneWeights(file, gMesh, binary, numVerts);
        }

        if (morph) {
            LoadMorphTargets(file, gMesh, binary, uMesh);
        }

        int baseIndex = 0;
        var submesh = 0;
        uMesh.subMeshCount = gMesh.primitives.Length;
        foreach (var primitive in gMesh.primitives) {
            if (primitive.indices is not uint iIndices) throw new Exception($"Mesh {gMesh.name} is using non-indexed geometry");
            var accessor = file.accessors[iIndices];

            var indices = accessor.componentType switch {
                Accessor.ComponentType.U8 => accessor.CopyOut<byte>(file, binary).Select(x => baseIndex + x),
                Accessor.ComponentType.U16 => accessor.CopyOut<ushort>(file, binary).Select(x => baseIndex + x),
                Accessor.ComponentType.U32 => accessor.CopyOut<uint>(file, binary).Select(x => baseIndex + (int)x),
                _ => throw new Exception(),
            };
            uMesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, submesh++);
            baseIndex += (int)file.accessors[primitive.attributes["POSITION"]].count;
        }

        uMesh.UploadMeshData(markNoLongerReadable: false); // TODO: mark true

        return uMesh;
    }

    public static BoneWeight[] LoadBoneWeights(AssetFile file, Mesh gMesh, byte[] binary, ulong numVerts) {
        ulong vertIdx = 0;
        var joints = new Vector4Byte[numVerts];
        var weights = new Vector4[numVerts];
        foreach (var primitive in gMesh.primitives) {
            file.accessors[primitive.attributes["JOINTS_0"]].CopyOut(file, binary, joints, vertIdx);
            file.accessors[primitive.attributes["WEIGHTS_0"]].CopyOut(file, binary, weights, vertIdx);
            vertIdx += file.accessors[primitive.attributes["POSITION"]].count;
        }

        var boneWeights = new BoneWeight[numVerts];
        for (ulong i = 0; i < numVerts; i++) {
            Vector4Byte b = joints[i];
            Vector4 w = weights[i];
            boneWeights[i] = new BoneWeight {
                boneIndex0 = b.x, boneIndex1 = b.y, boneIndex2 = b.z, boneIndex3 = b.w,
                weight0 = w.x, weight1 = w.y, weight2 = w.z, weight3 = w.w,
            };
        }
        return boneWeights;
    }

    public static void LoadMorphTargets(AssetFile file, Mesh gMesh, byte[] binary, UMesh uMesh) {
        var numVerts = uMesh.vertexCount;

        if (gMesh.primitives[0].targets?.Length is not int numTargets) return;
        foreach (var p in gMesh.primitives) {
            if (p.targets == null || p.targets.Length != numTargets) {
                throw new Exception("Mesh primitives don't have matching morph target arrays.");
            }
        }

        // Reuse the same arrays for a slight bit of efficiency
        var deltaVertices = new Vector3[numVerts];
        var deltaNormals = new Vector3[numVerts];

        gMesh.TryGetExtra("targetNames", out string[]? targetNames);

        for (var i = 0; i < numTargets; i++) {
            ulong vertIdx = 0;
            foreach (var primitive in gMesh.primitives) {
                file.accessors[primitive.targets![i]["POSITION"]].CopyOut(file, binary, deltaVertices, vertIdx);
                file.accessors[primitive.targets![i]["NORMAL"]].CopyOut(file, binary, deltaNormals, vertIdx);
                vertIdx += file.accessors[primitive.attributes["POSITION"]].count;
            }

            var name = targetNames?[i] ?? $"morph_{i}";
            uMesh.AddBlendShapeFrame(name, frameWeight: 1, deltaVertices, deltaNormals, null);
        }
        // TODO: Set "initial" blend shape weights based on the gMesh.weights array if present
    }

    public static Texture2D? LoadTexture(AssetFile file, Material mat, byte[] binary, bool mipmap) {
        if (mat.pbrMetallicRoughness?.baseColorTexture?.index is not uint iTex) {
            Plugin.logger.LogWarning($"Material {mat.name} has no texture index.");
            return null;
        }

        var tex = file.textures[iTex];
        if (tex.source is not uint iImg) {
            Plugin.logger.LogWarning($"Texture {tex.name} has no image source index.");
            return null;
        }

        var img = file.images[iImg];
        if (img.bufferView is not uint iView) {
            Plugin.logger.LogWarning($"Image {img.name} does not point to a buffer view.");
            return null;
        }

        var view = file.bufferViews[iView];
        if (view.buffer != 0) {
            Plugin.logger.LogWarning($"Buffer view {view.name} points to buffer #{view.buffer}.");
            return null;
        }

        var data = view.CopyOut(binary);
        var uTex = new Texture2D(0, 0, TextureFormat.RGBA32, mipmap);
        ImageConversion.LoadImage(uTex, data, markNonReadable: true);

        if (mipmap) {
            uTex.anisoLevel = 16;
            uTex.filterMode = FilterMode.Trilinear;
        } else {
            uTex.filterMode = FilterMode.Point;
        }

        if (tex.sampler is uint iSampler) {
            var sampler = file.samplers[iSampler];
            uTex.wrapModeU = sampler.wrapS.ToUnity();
            uTex.wrapModeV = sampler.wrapT.ToUnity();
        }

        return uTex;
    }
}
