using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;

namespace SingleplayerCousins;

internal static class Extensions {
    public static GameObject[] GetChildren(this GameObject parent) {
        var ret = new GameObject[parent.transform.childCount];
        for (var i = 0; i < ret.Length; i++) {
            ret[i] = parent.transform.GetChild(i).gameObject;
        }
        return ret;
    }

    public static T? FirstWithName<T>(this IEnumerable<T> objects, string name) where T : UnityObject {
        foreach (var obj in objects) if (obj?.name == name) return obj;
        return null;
    }

    public static List<T> AllWithNames<T>(this IEnumerable<T> objects, params string[] names) where T : UnityObject {
        var list = new List<T>();
        foreach (var obj in objects) {
            if (obj != null && names.Contains(obj.name)) {
                list.Add(obj);
            }
        }
        return list;
    }

    public static int IndexOf<T>(this T[] objects, string name) where T : UnityObject {
        for (var i = 0; i < objects.Length; i++) {
            if (objects[i].name == name) {
                return i;
            }
        }
        throw new System.InvalidOperationException($"No object with name {name} in list");
    }

    public static Vector3 ToUnity(this Assimp.Vector3D v) => new(v.X, v.Y, v.Z);
    public static Vector2 ToUnityVec2(this Assimp.Vector3D v) => new(v.X, v.Y);
    public static Matrix4x4 ToUnity(this Assimp.Matrix4x4 v) => new(new(v.A1, v.B1, v.C1, v.D1), new(v.A2, v.B2, v.C2, v.D2), new(v.A3, v.B3, v.C3, v.D3), new(v.A4, v.B4, v.C4, v.D4));
    public static Quaternion ToUnity(this Assimp.Quaternion v) => new(v.X, v.Y, v.Z, v.W);

    public static TextureWrapMode ToUnity(this Gltf.Sampler.WrapMode m) => m switch {
        Gltf.Sampler.WrapMode.Mirror => TextureWrapMode.Mirror,
        Gltf.Sampler.WrapMode.Clamp => TextureWrapMode.Clamp,
        Gltf.Sampler.WrapMode.Repeat => TextureWrapMode.Repeat,
        _ => throw new InvalidOperationException(),
    };

    public static BoneWeight AddWeight(this BoneWeight self, int idx, float weight) {
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
            throw new System.InvalidOperationException("Too many weights on one bone!");
        }
        return self;
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

    public static unsafe byte[] CopyOut(this Gltf.BufferView view, byte[] binary) {
        var dstBuf = new byte[view.byteLength];
        fixed (byte* src = &binary[view.byteOffset], dst = dstBuf) {
            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(dst, src, view.byteLength);
        }
        return dstBuf;
    }

    public static unsafe T[] CopyOut<T>(
        this Gltf.BufferView view,
        byte[] binary,
        long srcOffset,
        long count
    ) where T : struct {
        srcOffset += view.byteOffset;
        long elemSize = Marshal.SizeOf(typeof(T));
        long copySize = elemSize * count;
        if (srcOffset + copySize > binary.LongLength) throw new IndexOutOfRangeException();

        var dstBuf = new T[count];
        fixed (void* src = &binary[srcOffset], dst = dstBuf) {
            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(dst, src, copySize);
        }
        return dstBuf;
    }

    public static T[] CopyOut<T>(
        this Gltf.Accessor accessor,
        Gltf.AssetFile file,
        byte[] srcBuf
    ) where T : struct {
        var dstBuf = new T[accessor.count];
        accessor.CopyOut(file, srcBuf, dstBuf, 0);
        return dstBuf;
    }

    public static unsafe void CopyOut<T>(
        this Gltf.Accessor accessor,
        Gltf.AssetFile file,
        byte[] binary,
        T[] dstBuf,
        ulong dstIdx = 0
    ) where T : struct {
        if (dstIdx + accessor.count > (ulong)dstBuf.LongLength) throw new IndexOutOfRangeException();

        var valSize = Marshal.SizeOf(typeof(T));
        if (accessor.Size() != valSize) throw new Exception();
        var byteLength = valSize * accessor.count;

        if (accessor.bufferView == null) {
            fixed (void* dst = &dstBuf[dstIdx]) {
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemClear(dst, byteLength);
            }
        } else {
            var view = file.bufferViews[accessor.bufferView.Value];
            if (view.buffer != 0) throw new Exception();
            if (view.byteStride != null) throw new Exception();

            var srcOffset = view.byteOffset + accessor.byteOffset;
            if (srcOffset + byteLength > binary.LongLength) throw new IndexOutOfRangeException();

            fixed (void* src = &binary[srcOffset], dst = &dstBuf[dstIdx]) {
                Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(dst, src, byteLength);
            }
        }

        if (accessor.sparse is Gltf.Accessor.Sparse s) {
            var valsView = file.bufferViews[s.values.bufferView];
            var valsOffset = valsView.byteOffset + s.values.byteOffset;
            var vals = valsView.CopyOut<T>(binary, valsOffset, s.count);

            var idxView = file.bufferViews[s.indices.bufferView];
            var idxOffset = idxView.byteOffset + s.indices.byteOffset;

            switch (s.indices.componentType) {
                case Gltf.Accessor.ComponentType.U8:
                    ApplySparse(vals, dstBuf, dstIdx, idxView.CopyOut<byte>(binary, idxOffset, s.count));
                    break;
                case Gltf.Accessor.ComponentType.U16:
                    ApplySparse(vals, dstBuf, dstIdx, idxView.CopyOut<ushort>(binary, idxOffset, s.count));
                    break;
                case Gltf.Accessor.ComponentType.U32:
                    ApplySparse(vals, dstBuf, dstIdx, idxView.CopyOut<uint>(binary, idxOffset, s.count));
                    break;
            }
        }
    }

    private static void ApplySparse<T>(T[] src, T[] dst, ulong baseIndex, byte[] indices) {
        for (var i = 0; i < src.Length; i++) dst[baseIndex + indices[i]] = src[i];
    }

    private static void ApplySparse<T>(T[] src, T[] dst, ulong baseIndex, ushort[] indices) {
        for (var i = 0; i < src.Length; i++) dst[baseIndex + indices[i]] = src[i];
    }

    private static void ApplySparse<T>(T[] src, T[] dst, ulong baseIndex, uint[] indices) {
        for (var i = 0; i < src.Length; i++) dst[baseIndex + indices[i]] = src[i];
    }

    public static long Size(this Gltf.Accessor.ComponentType ty) => ty switch {
        Gltf.Accessor.ComponentType.U8 or Gltf.Accessor.ComponentType.I8 => 1,
        Gltf.Accessor.ComponentType.U16 or Gltf.Accessor.ComponentType.I16 => 2,
        Gltf.Accessor.ComponentType.U32 or Gltf.Accessor.ComponentType.F32 => 4,
        _ => 0,
    };

    public static long Size(this Gltf.Accessor.Shape sh) => sh switch {
        Gltf.Accessor.Shape.SCALAR => 1,
        Gltf.Accessor.Shape.VEC2 => 2,
        Gltf.Accessor.Shape.VEC3 => 3,
        Gltf.Accessor.Shape.VEC4 or Gltf.Accessor.Shape.MAT2 => 4,
        Gltf.Accessor.Shape.MAT3 => 9,
        Gltf.Accessor.Shape.MAT4 => 16,
        _ => 0,
    };

    public static long Size(this Gltf.Accessor a) => a.componentType.Size() * a.type.Size();
}
